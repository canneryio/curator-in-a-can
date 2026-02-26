using Cannery.Conductor.Client.Abstractions;
using Cannery.Conductor.Client.Entities;
using Goodtocode.McpClient.Abstractions;
using Goodtocode.McpClient.Messaging;
using System.Diagnostics;

namespace Cannery.Conductor.Client.Resolvers;

public class TagResolver(ITagStore store, IMcpClient mcpClient, string curatorPath, string source = "Local") : ITagResolver
{
    private readonly ITagStore _store = store;
    private readonly IMcpClient _mcpClient = mcpClient;
    private readonly string _curatorPath = curatorPath;
    private readonly string _source = source;

    public async Task<TagResolutionResultDto> ResolveAsync(IEnumerable<TagInputDto> inputs, TagResolutionOptionsDto? options = null, CancellationToken ct = default)
    {
        options ??= new TagResolutionOptionsDto();
        var tags = new List<ResolvedTagDto>();
        var errors = new List<TagErrorDto>();

        var normalized = inputs
            .Where(i => !string.IsNullOrWhiteSpace(i.Value))
            .Select(i => new {
                Input = i,
                Canonical = i.Value.Trim().ToLowerInvariant(),
                Aliases = new[] { i.Value }
            })
            .ToArray();

        // 1) Local fast-path
        foreach (var n in normalized)
        {
            var local = await _store.TryGetByCanonicalAsync(n.Canonical, ct);
            if (local is not null)
                tags.Add(local);
        }

        // 2) Missing set
        var missing = normalized.Where(n => tags.All(t => t.Canonical != n.Canonical)).ToArray();

        // 3) Insert provisional locally if allowed
        if (options.AllowCreate)
        {
            foreach (var m in missing)
            {
                var provisional = await _store.InsertProvisionalAsync(m.Canonical, m.Aliases, ct);
                tags.Add(provisional);
            }
        }
        else if (missing.Length > 0 && options.Mode == TagResolutionMode.Provisional)
        {
            foreach (var m in missing)
                errors.Add(new TagErrorDto { Input = m.Input.Value, Code = "not_found", Message = "No local match and creation disabled." });
        }

        // 4) Direct Curator call (HTTP via MCP client)
        var needCurator = normalized.Where(n => tags.All(t => t.Canonical != n.Canonical) || tags.Any(t => t.Canonical == n.Canonical && t.IsProvisional)).ToArray();
        if (needCurator.Length > 0)
        {
            var envelopeId = $"curator:{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}:{Guid.NewGuid()}";
            var requests = needCurator.Select(m => new CuratorTagRequestDto
            {
                Canonical = m.Canonical,
                Aliases = m.Aliases,
                Source = _source,
                Locale = m.Input.Locale,
                ProvisionalId = Guid.NewGuid(),
                IdempotencyKey = envelopeId
            }).ToList();

            try
            {
                var envelope = new Envelope<IReadOnlyList<CuratorTagRequestDto>>(
                    operation: "CuratorTagResolve",
                    correlationId: envelopeId,
                    sentUtc: DateTimeOffset.UtcNow,
                    result: requests
                );
                var response = await _mcpClient.SendAsync<Envelope<IReadOnlyList<CuratorTagRequestDto>>, Envelope<IReadOnlyList<CuratorTagResponseItemDto>>>(
                    operation: "CuratorTagResolve",
                    path: _curatorPath,
                    request: envelope,
                    options: null,
                    ct: ct);

                if (response.HasResult && response.Result is { Result: not null })
                {
                    foreach (var r in response.Result.Result)
                    {
                        var authoritative = await _store.InsertAuthoritativeAsync(r.CuratorTagId, r.Canonical, r.Aliases, ct);
                        var idx = tags.FindIndex(t => t.Canonical == r.Canonical);
                        if (idx >= 0)
                            tags[idx] = authoritative;
                        else
                            tags.Add(authoritative);
                    }
                }
                else if (response.HasProblem)
                {
                    if (options.Mode == TagResolutionMode.Strict)
                    {
                        foreach (var m in needCurator)
                            errors.Add(new TagErrorDto { Input = m.Input.Value, Code = "strict_required_curator_unavailable", Message = response.Problem?.Message ?? "Curator MCP error" });
                    }
                    else
                    {
                        Debug.WriteLine($"[TagResolver] Curator MCP error: {response.Problem?.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (options.Mode == TagResolutionMode.Strict)
                {
                    foreach (var m in needCurator)
                        errors.Add(new TagErrorDto { Input = m.Input.Value, Code = "strict_required_curator_unavailable", Message = ex.Message });
                }
                else
                {
                    Debug.WriteLine($"[TagResolver] Curator unavailable: {ex.Message}");
                }
            }
        }

        // 5) Deduplicate by canonical (prefer authoritative)
        var final = tags
            .GroupBy(t => t.Canonical)
            .Select(g => g.OrderBy(t => t.IsProvisional).First())
            .ToList();

        return new TagResolutionResultDto { Tags = final, Errors = errors };
    }
}
