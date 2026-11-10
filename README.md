<div align="center">

# Fouroam Server

**Your session. Your machine. Your rules.**

A self-hosted networking prototype for [Fouroam-V](https://github.com/Fouroam-Dev/Fouroam-V), an independent project exploring small private cooperative sessions.

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Development](https://img.shields.io/badge/stage-0.1.0--dev-orange.svg)
![.NET](https://img.shields.io/badge/.NET-10-purple.svg)
![GTA V](https://img.shields.io/badge/GTA%20V-integration%20not%20implemented-lightgrey.svg)
[![.NET CI](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml)

[English](README.md) · [Português](README.pt-BR.md) · [Roadmap](ROADMAP.md) · [Contributing](CONTRIBUTING.md)

</div>

> [!IMPORTANT]
> **Standalone networking proof of concept, not a GTA V multiplayer mod.** The source now contains a TCP server, a CLI test client and a smoke-test harness. It does **not** connect to GTA V, sync its world or enable story co-op. There is no downloadable installer or release yet. Compilation and test results are tracked by GitHub Actions.

## What works in the current source

- A self-hosted TCP server using **C# / .NET 10**.
- Up to **four concurrent players**, enforced server-side.
- Player identification, bounded name validation and a versioned handshake.
- Join/leave notifications, a player list, chat and ping/pong for diagnostics.
- Framed JSON messages with a 4096-byte size limit and a basic command rate limit.
- A standalone CLI client, plus automated smoke tests for capacity, reconnect, ping and chat.
- Bind to loopback by default, or explicitly bind to a LAN/VPN interface.

**Security limitations:** this development prototype has **no authentication, session passwords or encryption**. Never expose it directly to the public internet, forward its port, or connect over untrusted networks. Restrict access with your firewall and a trusted LAN or private VPN only. Do not use it for sensitive data.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- Windows 10/11 (primary target). The standalone test server may also work on other supported .NET platforms.
- No copy of GTA V is needed for these network-only tests.

## Run the server

From the repository root, open PowerShell or a terminal:

~~~powershell
dotnet restore Fouroam-Server.slnx
dotnet run --project src/Fouroam.Server -- --bind 127.0.0.1 --port 7777
~~~

For a **trusted LAN or private VPN** only, you can listen on all IPv4 interfaces:

~~~powershell
dotnet run --project src/Fouroam.Server -- --bind 0.0.0.0 --port 7777
~~~

Port 7777 is a TCP port. Configure the host firewall to allow only your trusted network if you plan to connect from another PC. For Radmin VPN, the other players use the host's Radmin VPN IPv4 address. **Do not port-forward this prototype.**

## Run a test client

In another terminal, on the host computer:

~~~powershell
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Athilio
~~~

From a second PC on the same trusted LAN/VPN, replace 127.0.0.1 with the host's **private LAN/VPN IPv4 address**.

Client commands:

| Input | Action |
| --- | --- |
| /players | Request connected-player list |
| /ping | Measure round-trip delay |
| /quit | Disconnect |
| Any other nonempty text | Send a short chat message |

To stop the server, press **Ctrl+C**.

## Automated tests

~~~powershell
dotnet build Fouroam-Server.slnx --configuration Release
dotnet run --project tests/Fouroam.SmokeTests --configuration Release
~~~

The test harness starts a server on localhost with an ephemeral port, connects four clients, checks that a fifth is rejected, verifies roster/ping/chat, then checks that a disconnected player frees a slot. The GitHub Actions workflow runs builds and smoke tests on Windows and Linux.

## Source layout

~~~text
Fouroam-Server/
├── src/
│   ├── Fouroam.Server/       # TCP host and session manager
│   ├── Fouroam.Client.Test/  # Interactive terminal test client
│   └── Fouroam.Shared/      # Messages and bounded JSON protocol
├── tests/
│   └── Fouroam.SmokeTests/   # Integration smoke tests without third-party packages
├── docs/
│   └── ARCHITECTURE.md
├── Fouroam-Server.slnx
└── .github/workflows/dotnet.yml
~~~

## Next milestones

The immediate next step is to validate this standalone transport and improve networking security before investigating real-time synchronization. **GTA V integration and official story missions are not implemented or promised.** Any game-side integration must be evaluated for technical and legal compatibility first.

Read [ROADMAP.md](ROADMAP.md), [ARCHITECTURE.md](docs/ARCHITECTURE.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

## Legal notice

Fouroam Server is an independent unofficial project, not affiliated with or endorsed by Rockstar Games, Take-Two Interactive, Valve or Epic Games. GTA V and related assets belong to their respective owners. The MIT license applies only to original project code and documentation and does not grant rights to proprietary game content or permit bypassing any platform rules.

## License

[MIT License](LICENSE). Copyright (c) 2026 Fouroam-Dev.
