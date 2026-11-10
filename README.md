<div align="center">

# Fouroam Server

**Your session. Your machine. Your rules.**

An open-source, self-hosted networking server for [Fouroam-V](https://github.com/Fouroam-Dev/Fouroam-V), a research-stage project exploring private cooperative sessions for up to four friends.

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Project status](https://img.shields.io/badge/status-planning-orange.svg)
![Target platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

[English](README.md) · [Português](README.pt-BR.md) · [Roadmap](ROADMAP.md) · [Contributing](CONTRIBUTING.md)

</div>

> [!IMPORTANT]
> **Planning / pre-alpha.** No functional or downloadable server exists yet. All capabilities below are design goals, not implemented features.

## Overview

Fouroam Server aims to let one player host a **small, private session on their own PC**, with up to three friends joining through LAN or a private VPN such as Radmin VPN. A VPS may also be supported as an optional hosting choice.

The project does not aim to replace GTA Online, provide public matchmaking, or require paid hosting.

## Planned capabilities

| Capability | Status |
| --- | --- |
| Local Windows server process | Planned |
| Maximum of four players | Planned |
| LAN and private VPN connectivity | Planned |
| Optional VPS self-hosting | Planned |
| Connection management, player IDs and capacity limits | Planned |
| Safe server logs and reconnect handling | Planned |
| Real-time state synchronization | Under research |
| Authorized GTA V integration | Requires feasibility and permissions review |
| Official story mission synchronization | Not available or guaranteed |
| Official Steam/Epic achievements | Not supported or promised |

## Intended session flow

1. One friend starts the future Fouroam Server process on a Windows PC.
2. Other players join the same local network or a private VPN.
3. Clients connect to the host's private IP address and configured port.
4. The host manages session state with a limit of four participants.

No central service or hosting subscription should be necessary for local sessions. Each participant would still need a legitimate game copy and any game-side integration must be properly authorized.

## Proposed technical direction

The first milestone will be a **standalone network prototype independent of GTA V**. The current proposal is:

- **C# / .NET 10** for the Windows server.
- **TCP** to validate connections, session capacity and disconnect behavior.
- Evaluation of **UDP** for later real-time state replication.
- Server-authoritative state, strict message validation and minimal private logs.

These are not final decisions. **There are currently no installation, build or launch instructions because the executable has not been developed.**

## Proposed repository structure

```text
Fouroam-Server/
├── src/                     # Future server and shared protocol
├── tests/                   # Future networking tests
├── docs/
│   └── ARCHITECTURE.md
├── README.md
├── README.pt-BR.md
├── ROADMAP.md
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── SECURITY.md
└── LICENSE
```

The `src/` and `tests/` folders are proposed, not implemented.

## Development and contribution

We welcome architecture discussions, independent networking prototypes, documentation contributions and security reviews.

- [Development roadmap](ROADMAP.md)
- [Architecture proposal](docs/ARCHITECTURE.md)
- [Contributing guidelines](CONTRIBUTING.md)
- [Code of conduct](CODE_OF_CONDUCT.md)
- [Security policy](SECURITY.md)
- [GitHub Issues](https://github.com/Fouroam-Dev/Fouroam-Server/issues)

## Legal notice

Fouroam Server is an **unofficial independent project**, not affiliated with or endorsed by Rockstar Games, Take-Two Interactive, Valve or Epic Games.

GTA V, its assets and services remain the property of their respective owners. This repository's MIT license applies only to original material and **does not grant rights to proprietary game content, trademarks, code or platform services**. Game-side multiplayer modifications or official story missions may be restricted and require a permissions review before implementation or distribution.

Do not submit copyrighted game assets, leaked code, authentication or anti-cheat bypasses, or pirated content.

## License

Original project code and documentation: [MIT](LICENSE), subject to third-party rights. Copyright (c) 2026 Fouroam-Dev.
