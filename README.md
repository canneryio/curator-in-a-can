# Cannery.Conductor.Client

[![NuGet CI/CD](https://github.com/canneryio/conductor-in-a-can/actions/workflows/can-conductor-nuget.yml/badge.svg)](https://github.com/canneryio/conductor-in-a-can/actions/workflows/can-conductor-nuget.yml)

A .NET 10 client library for **Cannery Curator in a Can**—the authoritative source for tag curation, resolution, and taxonomy management. This library enables applications to resolve, deduplicate, and manage tags using both local and authoritative (curator) sources, supporting robust metadata workflows for modern distributed systems.

---

## Features

- **Tag Resolution:** Resolve tags from local or authoritative (curator) sources.
- **Authoritative Tag Management:** Insert and update tags using the Cannery Curator service.
- **Provisional Tag Support:** Seamlessly handle provisional (local-only) tags when authoritative creation is not allowed.
- **Deduplication:** Ensures unique canonical tags, preferring authoritative sources.
- **Error Handling:** Detailed error reporting for missing, ambiguous, or invalid tags.
- **Flexible Resolution Modes:** Strict, provisional, and hybrid strategies.
- **Integration:** Works with CanneryIO Curator and MCP (Model Context Protocol) services.

---

## Installation

Install via NuGet: