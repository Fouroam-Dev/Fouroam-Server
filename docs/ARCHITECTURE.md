# Fouroam Server — Standalone Network Prototype

## Current implementation

The first milestone is a **real TCP session server**, completely independent from GTA V.

~~~mermaid
flowchart TD
    C1[CLI Client A] --> N[Trusted LAN / VPN]
    C2[CLI Client B] --> N
    C3[CLI Client C] --> N
    C4[CLI Client D] --> N
    N --> S[Fouroam.Server TCP 7777]
    S --> M[Session manager - max 4 players]
    S --> P[Versioned JSON protocol]
~~~

The host can run the server process on the same machine as a client. Other participants connect using the host's private LAN/VPN IPv4 address. No cloud account, matchmaking or VPS is required.

## Components

| Project | Responsibility |
| --- | --- |
| \`Fouroam.Server\` | TCP listener, connection management, unique IDs, capacity, command validation and notifications |
| \`Fouroam.Client.Test\` | CLI client for handshake, roster, chat, ping and disconnect tests |
| \`Fouroam.Shared\` | JSON-lines messages, protocol version and bounded framing |
| \`Fouroam.SmokeTests\` | Automated integration checks with several localhost clients |

## Protocol v1

Each TCP line is a single UTF-8 JSON object, up to **4096 bytes** before the newline delimiter.

Example handshake request:

~~~json
{"type":"hello","protocolVersion":1,"name":"Athilio"}
~~~

Successful response:

~~~json
{"type":"welcome","protocolVersion":1,"playerId":1,"playerCount":1,"maxPlayers":4,"players":[{"id":1,"name":"Athilio"}]}
~~~

Messages and valid directions:

| Type | Direction | Purpose |
| --- | --- | --- |
| \`hello\` | Client → Server | Join with protocol version and name |
| \`welcome\` | Server → Client | Confirm ID and roster |
| \`server_full\` | Server → Client | Reject a fifth player |
| \`player_joined\` / \`player_left\` | Server → Clients | Session events |
| \`players_request\` / \`players\` | Client → Server / Server → Client | Query roster |
| \`ping\` / \`pong\` | Client → Server / Server → Client | Diagnostic echo |
| \`chat\` | Both | Up to 280 printable characters |
| \`error\` | Server → Client | Explain invalid requests |

The server chooses and authenticates the **player ID within the session**; clients cannot select their own IDs. Names are 2–24 characters using letters, digits, spaces, \`_\` and \`-\`. A rate limit of 20 commands in 5 seconds applies per connected player. Up to eight incomplete handshakes can be active, with a 10-second timeout.

## Networking and security boundaries

- Default binding is **127.0.0.1**; use \`--bind 0.0.0.0\` explicitly for other machines.
- Only use a trusted LAN or private VPN; this proof of concept has **no transport encryption, authentication, or server join password**.
- Do not forward the port on your public router or expose it on a public-facing VPS.
- Firewall rules must restrict traffic to authorized private peers.
- Network payloads are validated and bounded. This is **not a security audit** or production-ready protocol.

## Future work

Authentication, encrypted transport, resilience under network loss, real-time state replication and authorized game integrations are not yet implemented. No official story mission or platform achievement support is guaranteed.
