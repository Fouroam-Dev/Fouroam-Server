# Fouroam Server — Production Readiness

**Status: PRIVATE-BETA NETWORK PROTOTYPE. NOT APPROVED FOR PUBLIC-INTERNET PRODUCTION.**

The application has been exercised with four localhost players, chat, ping, and host kick by a real Windows operator. GitHub Actions verifies the integration tests on Linux and Windows. A passing localhost test or CI build does not validate a hostile public deployment.

## Supported use today

- A single Windows PC with several local CLI clients.
- A small private LAN or VPN operated by trusted participants with firewall restrictions.
- Original network test functionality that is entirely independent of GTA V.

## Release engineering

- [x] Source-based CI on Windows and Linux.
- [x] Self-contained Windows x64 server/client EXE packages generated automatically by a separate GitHub Actions workflow.
- [x] Basic SHA-256 manifest for each development EXE.
- [x] No local key files or user secret state included in the build artifacts.
- [ ] Verify successful executable packaging on Windows runner.
- [ ] Perform a fresh download-and-run test on a Windows PC without .NET SDK.
- [ ] Define versioned release channels, support policy and rollback procedure.
- [ ] Add code signing and a trusted provenance/signature process for public binary releases.

## Security blockers to public deployment

- [ ] Independent security review of TLS setup and certificate/key storage.
- [ ] Revisit minimum password policy: **4-character passwords are inherently weak**; private-beta compatibility is not a suitable default for public deployments.
- [ ] Replace shared-password-only access with stronger participant identity and enrollment mechanisms.
- [ ] Implement identity verification beyond client-controlled installation tokens; current bans are bypassable by generating a new token.
- [ ] Evaluate distributed abuse/DoS protections and rate limits at the network edge.
- [ ] Test firewall rules, logs, recovery from network loss and Windows key-file permissions.
- [ ] Add graceful upgrade, configuration migration and backup guidance.

## Product scope blockers

- [ ] Test actual connection between two Windows PCs on a trusted LAN.
- [ ] Test Radmin VPN in a private network.
- [ ] Determine whether any GTA V integration is technically possible and permitted under applicable game/platform rules.
- [ ] Design and validate gameplay synchronization independently before attempting any authorized game integration.

## Explicit non-goals of the current prototype

The Fouroam Server is **not** a GTA V co-op mod, a public multiplayer service, an anti-cheat system, or an official story mission synchronizer. **Do not advertise or deploy it as those products.**

## Operator notes

Download the EXE packages only from the repository's GitHub Actions after a successful build, and verify the included \`SHA256SUMS.txt\`. They are unsigned developer artifacts. Protect the \`.fouroam\` directory containing the TLS private key and session config. Never port-forward the current server; the scope is a trusted LAN/private VPN. See [SECURITY.md](../SECURITY.md) and [Windows EXE guide](WINDOWS-EXE.md).
