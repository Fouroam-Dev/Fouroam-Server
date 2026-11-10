# Security Policy

## Supported versions

Fouroam Server v0.2.0-dev is a **networking prototype** without a public release. No version is considered production-hardened or suitable for exposure on an unrestricted public network.

## Reporting vulnerabilities

Do not publish exploit instructions, secrets, identity files, IP addresses or private configuration in an issue.

Contact **athiliorecovery@gmail.com** with subject "Fouroam Server - Security Report". Include impact, component, reproduction details and sanitized logs. The maintainers will review privately; no response-time SLA is promised.

## Current security design

- TLS 1.2/1.3, with a self-signed host certificate **explicitly pinned** by SHA-256 fingerprint on the client.
- Local salted PBKDF2-SHA256 verifier for the shared session password.
- Bounded JSON messages, handshake limit, connection limit and login throttling.
- No remote admin commands. \`kick\`, \`ban\`, \`unban\`, \`stop\` and related commands are available only through the host terminal.
- Persistent bans use SHA-256 fingerprints of random client installation tokens, not temporary IDs.

## Known limitations and operator responsibilities

- Client tokens can be regenerated, bypassing bans. No account verification, anti-cheat or identity attestation is provided.
- The locally stored PFX contains the server private key. Protect it and the \`.fouroam/\` directory with OS access controls, and **do not commit, distribute or expose them**.
- All players share one session password. Leaked passwords should be treated as compromised.
- The minimum password length is **4 characters** for private-session convenience, but a 4-character password is easy to guess and is **not recommended**. Prefer a long, unique password and restrict network access to trusted LAN/VPN clients.
- Pinning protects against unrecognized TLS certificates **only when the fingerprint was obtained securely**. Verify an unexpected change with the operator.
- A basic failed-login limiter is not protection against distributed denial-of-service attacks or hostile internet traffic.
- Do not port-forward. Restrict access to trusted LAN/VPN participants using host firewall rules.
- The project still needs independent security review and network hardening before any public deployment.

The policy does not authorize attacking Rockstar, VPN providers, game networks or other players.
