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
- [x] Repeat real-device testing with the new TLS client (user validated connection and host kick on Windows).
- [ ] Independent security review; improve password and identity management.

## 2.1. Private-beta packaging

- [x] Add automated Windows x64 self-contained EXE artifacts for server and test client.
- [x] Support optional slash prefixes and unambiguous player names in local console commands.
- [x] Add administration regression tests and documentation for EXE usage.
- [ ] Confirm a successful Windows EXE artifact build on GitHub Actions.
- [x] Have a user test downloading and running the EXE without the SDK (original package reached the missing-private-config check).
- [x] Add automatic interactive first-run setup when a freshly extracted EXE has no private configuration.
- [ ] Validate the new first-run EXE against the Windows runner and retest it on a user's PC.

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
