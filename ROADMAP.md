# Fouroam Server Roadmap

The roadmap distinguishes implemented source code from CI and real-network validation.

## 0. Project foundation

- [x] README, community documentation and MIT license.
- [x] C# / .NET 10 and Windows-first architecture.
- [ ] Assess technical and legal permissions for any game integration.

## 1. Standalone networking v0.1

- [x] Four-player server, player IDs and bounded protocol.
- [x] TCP ping, roster, join/leave and diagnostic chat.
- [x] Windows and Linux CI checks.
- [x] User-validated four concurrent connections and chat on Windows.
- [x] User-validated rejection of a fifth connection.

## 2. Local security and administration v0.2

- [x] Private setup and salted password verifier.
- [x] TLS with mandatory certificate fingerprint validation.
- [x] Host-only console commands and kick.
- [x] Ban/unban using persistent per-installation identity hashes.
- [x] Rate limiting for authentication failures.
- [x] Expanded smoke tests for authorization and bans.
- [x] Review v0.2 Windows/Linux GitHub Actions results (both build and smoke checks passed).
- [ ] Repeat real-device testing with the new TLS client.
- [ ] Independent security review; improve password and identity management.

## 3. LAN and VPN testing

- [ ] Test a real second PC through LAN.
- [ ] Test a trusted Radmin VPN connection.
- [ ] Validate firewall settings and imperfect connections.

## 4. Real-time state protocol

- [ ] Evaluate UDP and state replication.
- [ ] Build a test simulation unrelated to GTA V.
- [ ] Benchmark performance and reliability.

## 5. Game integration feasibility

- [ ] Identify a permitted game-side integration path, if any.
- [ ] Assess game editions and official mission restrictions.

Official GTA V story co-op and Steam/Epic achievement support are **not implemented or promised**.
