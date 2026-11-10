<div align="center">

# Fouroam Server

**Your session. Your machine. Your rules.**

Self-hosted networking prototype for [Fouroam-V](https://github.com/Fouroam-Dev/Fouroam-V).

[![MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![Version](https://img.shields.io/badge/version-0.2.1--dev-orange.svg)
![.NET](https://img.shields.io/badge/.NET-10-purple.svg)
[![.NET CI](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml)

[English](README.md) · [Português](README.pt-BR.md) · [Roadmap](ROADMAP.md) · [Contributing](CONTRIBUTING.md)

</div>

> [!IMPORTANT]
> **Standalone network prototype, not a GTA V multiplayer mod.** It does not connect to GTA V, synchronize the game, or support official story missions. There is no published installer or downloadable release. Source builds are validated by GitHub Actions.

## What's new in v0.2.0-dev

- **TLS 1.2/1.3** for every connection. Clients must verify the host's SHA-256 certificate fingerprint, supplied through a trusted channel. There is **no plaintext fallback**.
- **Private session configuration:** a local, ignored file stores a session name and **salted PBKDF2-SHA256 password verifier**, not the password itself.
- **Host console administration:** \`help\`, \`status\`, \`players\`, \`kick\`, \`ban\`, \`bans\`, \`unban\`, \`say\`, \`stop\`.
- **Identity-based bans:** client identities are generated randomly, retained locally and hashed on the server. Ban lists survive server restarts.
- **Authentication throttling:** up to five failed attempts per remote address over a sliding 60-second window; bounded handshakes and messages.
- **Expanded automated tests** for authentication, administrator commands, permission separation, identity bans, four-player capacity and original chat/ping behavior.

**Existing v0.1 functionality remains**, but the wire protocol is now **v2**, so older unencrypted clients cannot connect.

## Downloadable Windows x64 development EXEs

A [Windows EXE workflow](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/windows-exe.yml) builds **self-contained** server and test-client executables. You can download the two development ZIP artifacts from a successful GitHub Actions run and run the executables without installing the .NET SDK. Builds are not code-signed or stable production releases.

Read the [Windows executable guide](docs/WINDOWS-EXE.md) and the [production readiness checklist](docs/PRODUCTION-READINESS.md). Download links appear in the **Artifacts** section of a successful workflow run and are available for seven days.

The host console accepts an optional leading slash (such as `/kick 1`) and supports uniquely matched player names (`/kick Athilio`). Numeric IDs remain the safest way to address players.

## Security limitations

This is still a **development prototype, not a production-hardened public server**.

- Server-side TLS uses a locally generated self-signed certificate. Every client must verify its fingerprint **out-of-band**. A changed fingerprint must be independently verified before connecting.
- The server certificate's private key is stored locally in \`.fouroam/server-cert.pfx\`. Protect that file and the entire \`.fouroam\` folder using your operating system's permissions; never upload, share or back them up publicly.
- The private config contains a salted password verifier, **not a plaintext password**. Clients send their password only through the pinned TLS channel.
- Ban identities are **client-controlled installation tokens**, not accounts, hardware identifiers or verified identities. Users can generate a new token and bypass bans. Do not present these bans as an anti-cheat or identity verification mechanism.
- The password is shared by all players. TLS protects it in transit, but anyone who learns it can connect unless another control rejects them.
- LAN or VPN access is recommended; restrict the host's firewall. **Do not port-forward or expose the development server to the public internet.**
- Do not transmit sensitive information or rely on the current protocol for hostile network environments.

## Requirements

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows 10/11.

## 1. Initialize a private session (first run only)

From the repository root:

~~~powershell
dotnet restore Fouroam-Server.slnx
dotnet run --project src/Fouroam.Server -- init
~~~

Enter a session name and a password of **4–128 characters**. The password is entered without echoing it. Setup creates the private \`.fouroam/\` directory and displays the **TLS certificate fingerprint**. **Security warning:** a 4-character password is weak. Use a long, unique password whenever possible, keep the server on a trusted LAN/VPN and never port-forward it. The password cannot be recovered from the verifier: if it is lost, you must set up a new configuration.

**Never commit or share the local \`.fouroam/\` directory.** Its contents include the server private key and ban database.

## 2. Start the server

For local tests on one PC:

~~~powershell
dotnet run --project src/Fouroam.Server -- --bind 127.0.0.1 --port 7777
~~~

For a **trusted LAN or private Radmin VPN network**, with an appropriate firewall rule:

~~~powershell
dotnet run --project src/Fouroam.Server -- --bind 0.0.0.0 --port 7777
~~~

The server prints its TLS certificate fingerprint on startup. Compare it with the value displayed during setup.

## 3. Connect a test client

Open another terminal and paste the real fingerprint displayed by your host:

~~~powershell
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Athilio --fingerprint YOUR_64_CHARACTER_CERTIFICATE_SHA256
~~~

The client prompts for the session password privately. Do **not** supply a password as a command-line argument.

For a second machine on Radmin VPN, replace \`127.0.0.1\` with the host's **private Radmin IPv4 address**. Both computers must be on a trusted network, with the firewall restricted to the intended participants.

**Four test clients on the same PC:** each client needs a different identity file so that a single installation is not impersonated by four active connections:

~~~powershell
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Michael --fingerprint YOUR_FINGERPRINT --identity-file .fouroam-client/michael.key
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Franklin --fingerprint YOUR_FINGERPRINT --identity-file .fouroam-client/franklin.key
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Trevor --fingerprint YOUR_FINGERPRINT --identity-file .fouroam-client/trevor.key
~~~

Replace \`YOUR_FINGERPRINT\` with the actual 64-character SHA-256 fingerprint. The default identity is saved under the current Windows user's LocalAppData/Fouroam folder. Other users of the same computer should use their own profile or identity file. Do not share identity tokens.

Client commands: \`/players\`, \`/ping\`, \`/quit\`, or type a short message to chat.

## Server console commands

Host-only commands are typed in the **server's terminal**, not in a client:

| Command | Purpose |
| --- | --- |
| \`help\` | List commands |
| \`status\` | Session and connected player count |
| \`players\` | Names, IDs and shortened identity fingerprints |
| \`kick 3 [reason]\` | Disconnect player ID 3 |
| \`ban 3 [reason]\` | Ban that client's persistent identity and disconnect it |
| \`bans\` | Show stored identity fingerprints |
| \`unban ABCDEF12\` | Remove a uniquely matching ban by fingerprint prefix (at least 8 hex characters) |
| \`say Hello\` | Broadcast an administrator announcement |
| \`stop\` | Gracefully shut down |

**Player IDs are temporary connection identifiers.** For persistent bans, the server saves a fingerprint of the client's secret installation token in \`.fouroam/bans.json\`. A ban cannot prevent someone from creating a new client identity; there is no verified account system.

## Build and smoke tests

~~~powershell
dotnet build Fouroam-Server.slnx --configuration Release
dotnet run --project tests/Fouroam.SmokeTests --configuration Release
~~~

The tests run entirely over localhost with a temporary TLS certificate and config. They verify the original chat/roster/ping features, full sessions, password rejection, host-only kick, ban persistence and unban. [GitHub Actions](https://github.com/Fouroam-Dev/Fouroam-Server/actions) runs the tests on Windows and Linux.

## Source layout

~~~text
src/
  Fouroam.Server/        TLS host, administrative console, config, bans, throttling
  Fouroam.Client.Test/   Interactive pinned-TLS test client
  Fouroam.Shared/        Versioned bounded JSON protocol and client identity
tests/
  Fouroam.SmokeTests/    Automated networking/security integration tests
docs/
  ARCHITECTURE.md        Protocol and threat model
~~~

## Legal notice

Fouroam Server is independent and unofficial; not affiliated with or endorsed by Rockstar Games, Take-Two Interactive, Valve or Epic Games. All GTA V assets and trademarks belong to their respective owners. The MIT license applies only to original source and documentation and does not authorize third-party game content or restricted modifications. GTA V integration, cooperative official missions and official platform achievements are **not implemented or promised**.

## License

[MIT](LICENSE), copyright (c) 2026 Fouroam-Dev.
