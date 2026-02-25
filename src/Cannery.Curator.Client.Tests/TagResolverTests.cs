using Cannery.Conductor.Client.Abstractions;
using Cannery.Conductor.Client.Entities;
using Cannery.Conductor.Client.Resolvers;
using Goodtocode.McpClient.Abstractions;
using Goodtocode.McpClient.Messaging;

namespace Cannery.Curator.Client.Tests
{
    [TestClass]
    public class TagResolverTests
    {
        private static TagInputDto MakeInput(string value, string? locale = null) => new TagInputDto { Value = value, Locale = locale };

        private class FakeTagStore : ITagStore
        {
            public Func<string, CancellationToken, Task<ResolvedTagDto?>>? TryGetByCanonicalAsyncImpl;
            public Func<string, IEnumerable<string>, CancellationToken, Task<ResolvedTagDto>>? InsertProvisionalAsyncImpl;
            public Func<Guid, string, IEnumerable<string>, CancellationToken, Task<ResolvedTagDto>>? InsertAuthoritativeAsyncImpl;
            public Func<Guid, CancellationToken, Task<ResolvedTagDto?>>? GetByIdAsyncImpl;

            public Task<ResolvedTagDto?> TryGetByCanonicalAsync(string canonical, CancellationToken ct)
                => TryGetByCanonicalAsyncImpl?.Invoke(canonical, ct) ?? Task.FromResult<ResolvedTagDto?>(null);

            public Task<ResolvedTagDto> InsertProvisionalAsync(string canonical, IEnumerable<string> aliases, CancellationToken ct)
                => InsertProvisionalAsyncImpl!(canonical, aliases, ct);

            public Task<ResolvedTagDto> InsertAuthoritativeAsync(Guid curatorId, string canonical, IEnumerable<string> aliases, CancellationToken ct)
                => InsertAuthoritativeAsyncImpl!(curatorId, canonical, aliases, ct);

            public Task<ResolvedTagDto?> GetByIdAsync(Guid tagId, CancellationToken ct)
                => GetByIdAsyncImpl?.Invoke(tagId, ct) ?? Task.FromResult<ResolvedTagDto?>(null);
        }

        private class FakeMcpClient : IMcpClient
        {
            public Func<string, string, object, McpSendOptions?, CancellationToken, object>? SendAsyncImpl;

            public Task<Envelope<TResponse>> SendAsync<TRequest, TResponse>(
                string operation,
                string path,
                TRequest request,
                McpSendOptions? options = null,
                CancellationToken ct = default)
            {
                if (SendAsyncImpl != null)
                {
                    var result = SendAsyncImpl(operation, path, request!, options, ct);
                    return Task.FromResult((Envelope<TResponse>)result!);
                }
                return Task.FromResult<Envelope<TResponse>>(default!);
            }
        }

        [TestMethod]
        public async Task ResolveAsyncReturnsLocalTagWhenTagExistsLocally()
        {
            var tag = new ResolvedTagDto { Canonical = "foo" };
            var store = new FakeTagStore
            {
                TryGetByCanonicalAsyncImpl = (canonical, ct) => Task.FromResult<ResolvedTagDto?>(canonical == "foo" ? tag : null)
            };
            var mcp = new FakeMcpClient();
            var resolver = new TagResolver(store, mcp, "curator");

            var result = await resolver.ResolveAsync(new[] { MakeInput("foo") });

            Assert.AreEqual(1, result.Tags.Count);
            Assert.AreEqual("foo", result.Tags[0].Canonical);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task ResolveAsyncCreatesProvisionalWhenMissingAndAllowed()
        {
            var store = new FakeTagStore
            {
                TryGetByCanonicalAsyncImpl = (canonical, ct) => Task.FromResult<ResolvedTagDto?>(null),
                InsertProvisionalAsyncImpl = (canonical, aliases, ct) => Task.FromResult(new ResolvedTagDto { Canonical = canonical, IsProvisional = true })
            };
            var mcp = new FakeMcpClient();
            var resolver = new TagResolver(store, mcp, "curator");

            var result = await resolver.ResolveAsync(new[] { MakeInput("bar") }, new TagResolutionOptionsDto { AllowCreate = true });

            Assert.AreEqual(1, result.Tags.Count);
            Assert.AreEqual("bar", result.Tags[0].Canonical);
            Assert.IsTrue(result.Tags[0].IsProvisional);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task ResolveAsyncReturnsErrorWhenMissingAndProvisionalModeAndNotAllowed()
        {
            var store = new FakeTagStore
            {
                TryGetByCanonicalAsyncImpl = (canonical, ct) => Task.FromResult<ResolvedTagDto?>(null)
            };
            var mcp = new FakeMcpClient();
            var resolver = new TagResolver(store, mcp, "curator");

            var result = await resolver.ResolveAsync(new[] { MakeInput("baz") }, new TagResolutionOptionsDto { AllowCreate = false, Mode = TagResolutionMode.Provisional });

            Assert.AreEqual(0, result.Tags.Count);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("not_found", result.Errors[0].Code);
        }

        [TestMethod]
        public async Task ResolveAsyncCallsCuratorAndInsertsAuthoritativeWhenNeeded()
        {
            var store = new FakeTagStore
            {
                TryGetByCanonicalAsyncImpl = (canonical, ct) => Task.FromResult<ResolvedTagDto?>(null),
                InsertAuthoritativeAsyncImpl = (id, canonical, aliases, ct) => Task.FromResult(new ResolvedTagDto { Id = id, Canonical = canonical, Aliases = aliases.ToList(), IsProvisional = false }),
                InsertProvisionalAsyncImpl = (canonical, aliases, ct) => Task.FromResult(new ResolvedTagDto { Canonical = canonical, Aliases = aliases.ToList(), IsProvisional = true })
            };

            var curatorTagId = Guid.NewGuid();
            var mcp = new FakeMcpClient
            {
                SendAsyncImpl = (operation, path, request, options, ct) =>
                {
                    var responseItems = new List<CuratorTagResponseItemDto>
                    {
                        new CuratorTagResponseItemDto { CuratorTagId = curatorTagId, Canonical = "curated", Aliases = new[] { "curated" } }
                    };
                    // Wrap the result in an inner envelope to match the resolver's expectation
                    var innerEnvelope = new Envelope<IReadOnlyList<CuratorTagResponseItemDto>>(
                        operation: operation,
                        correlationId: "test-correlation",
                        sentUtc: DateTimeOffset.UtcNow,
                        result: responseItems,
                        problem: null,
                        @continue: null,
                        metadata: null
                    );
                    var outerEnvelope = new Envelope<Envelope<IReadOnlyList<CuratorTagResponseItemDto>>>(
                        operation: operation,
                        correlationId: "test-correlation",
                        sentUtc: DateTimeOffset.UtcNow,
                        result: innerEnvelope,
                        problem: null,
                        @continue: null,
                        metadata: null
                    );
                    return outerEnvelope;
                }
            };

            var resolver = new TagResolver(store, mcp, "curator");
            var result = await resolver.ResolveAsync(new[] { MakeInput("curated") }, new TagResolutionOptionsDto { AllowCreate = false, Mode = TagResolutionMode.Strict });

            Assert.AreEqual(1, result.Tags.Count);
            Assert.AreEqual("curated", result.Tags[0].Canonical);
            Assert.IsFalse(result.Tags[0].IsProvisional);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public async Task ResolveAsyncDeduplicatesTagsPrefersAuthoritative()
        {
            var tag1 = new ResolvedTagDto { Canonical = "dup", IsProvisional = true };
            var tag2 = new ResolvedTagDto { Canonical = "dup", IsProvisional = false };
            int callCount = 0;
            var store = new FakeTagStore
            {
                TryGetByCanonicalAsyncImpl = (canonical, ct) =>
                {
                    callCount++;
                    return Task.FromResult<ResolvedTagDto?>(callCount == 1 ? tag1 : tag2);
                }
            };
            var mcp = new FakeMcpClient
            {
                SendAsyncImpl = (operation, path, request, options, ct) =>
                {
                    // Return a valid envelope with an empty result list
                    var responseEnvelope = new Envelope<IReadOnlyList<CuratorTagResponseItemDto>>(
                        operation: operation,
                        correlationId: "test-correlation",
                        sentUtc: DateTimeOffset.UtcNow,
                        result: new List<CuratorTagResponseItemDto>()
                    );
                    return responseEnvelope;
                }
            };
            var resolver = new TagResolver(store, mcp, "curator");

            var result = await resolver.ResolveAsync(new[] { MakeInput("dup"), MakeInput("dup") });

            Assert.AreEqual(1, result.Tags.Count);
            Assert.IsFalse(result.Tags[0].IsProvisional);
        }
    }
}
