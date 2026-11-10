# Fouroam Server Architecture — v0.2.0-dev

> This is a standalone TLS networking prototype. **No GTA V integration exists.**

## Components

~~~mermaid
flowchart LR
    A[Test client] -->|TLS 1.2/1.3, pinned cert| H[Server Host]
    H --> P[Protocol v2]
    P --> S[Four-player session]
    P --> F[Authentication limiter]
    P --> B[Persistent identity bans]
    C[Local operator terminal] -->|Host-only commands| H
    L[Private .fouroam directory] --> H
~~~

## Local configuration and TLS identity

The host runs \`init\` once to configure a session name and password (4–128 characters; short passwords are insecure and strongly discouraged). The password is converted to a 210,000-iteration PBKDF2-SHA256 verifier with a random 16-byte salt and saved to \`.fouroam/server.local.json\`.

A self-signed TLS certificate and its private key are stored in \`.fouroam/server-cert.pfx\`. The client **requires** the certificate's SHA-256 fingerprint as an argument and rejects any other certificate. Operators must distribute and verify the fingerprint out of band.

Clients generate a random 256-bit installation identity token, saved under LocalAppData/Fouroam or an operator-provided \`--identity-file\`. This bearer token is submitted only over the pinned TLS channel; the host hashes it for identity bans, without storing the token plaintext.

**Ban limitation:** rotating the client identity file creates a new identity and bypasses an existing ban. This is not authenticated account identification.

## Protocol v2

Wire format is newline-delimited UTF-8 JSON, up to 4096 bytes per message.

After **successful TLS negotiation**, the client sends:

~~~json
{
  "type": "hello",
  "protocolVersion": 2,
  "name": "Athilio",
  "password": "<entered privately on client>",
  "identityToken": "<64 hex characters, local client secret>"
}
~~~

The host validates the version, printable name, installation token, shared password, ban list, capacity and duplicate active identities.

On success the server responds with \`welcome\` including the temporary player ID, session name and roster. Other commands include \`players_request\`, \`players\`, \`player_joined\`, \`player_left\`, \`chat\`, \`ping\`, \`pong\`, \`announcement\`, \`kicked\`, \`error\`, and \`server_full\`.

**Never log, quote or publish the client's password or identity token.**

## Administration and authorization

The host reads local terminal commands. No remote wire command can request kick, ban, unban, announcement or stop. The server chooses player IDs; clients cannot impersonate a server-assigned ID.

Bans are saved atomically in \`.fouroam/bans.json\` as SHA-256 token fingerprints. Administrative unban uses a uniquely matching prefix of at least eight hexadecimal digits.

## Transport hardening currently implemented

- TLS 1.2/1.3 with explicit certificate pin.
- Protocol handshake timeout of 10 seconds and a maximum of eight incomplete handshakes.
- Five failed credentials per remote address within a rolling 60-second window.
- Maximum of four active players.
- 4096-byte frame limit and 20 commands per five seconds per player.
- Printable 2–24 character player names and 1–280 character chat messages.

## Open security tasks

- Stronger account/identity model and safer key storage.
- Tests under hostile or lossy network traffic.
- Audited operational guidance for internet-exposed hosts (currently **not supported**).
- Independent review before production use.

The next major feature, real-time state synchronization, remains research only. GTA V integration and official story progress are not supported.
