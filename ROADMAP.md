# Fouroam Server Roadmap

Status is based on the checked-in source code. Passing CI and real LAN/VPN testing are separate validation steps.

## Milestone 0 — Project foundation

- [x] Publish README and community documentation.
- [x] Keep an MIT license for original work.
- [x] Select .NET 10 / C# for the standalone network prototype.
- [ ] Review permissions for any GTA V integration and official story changes.

## Milestone 1 — Standalone TCP networking prototype

- [x] Add .NET projects for server, test client and shared protocol.
- [x] Implement configurable IPv4 bind address and TCP port.
- [x] Define a bounded, versioned JSON-lines protocol.
- [x] Assign unique IDs and validate names at connection time.
- [x] Enforce a four-player maximum; reject the fifth.
- [x] Implement join/leave, roster, diagnostic chat and ping messages.
- [x] Handle disconnects and free player slots.
- [x] Add integration smoke tests and a Windows/Linux build workflow.
- [ ] Verify green builds and integration tests in GitHub Actions.

## Milestone 2 — Private LAN/VPN validation

- [ ] Run the standalone server on a real Windows computer.
- [ ] Connect a second physical computer on a trusted LAN.
- [ ] Repeat with Radmin VPN and validate Windows firewall configuration.
- [ ] Test malformed traffic, reconnections and poor network conditions.
- [ ] Add optional authentication and encrypted transport before considering any broader exposure.

## Milestone 3 — Real-time synchronization research

- [ ] Evaluate UDP and reliability mechanisms.
- [ ] Design host authority, state replication and timeouts.
- [ ] Test in an independent simulation before any game integration.

## Milestone 4 — GTA V integration feasibility

- [ ] Determine a legally permitted and compatible method, if any.
- [ ] Document GTA V edition and version limitations.
- [ ] Scope only authorized integration experiments.

## Not promised

- Official GTA V story mission synchronization.
- Steam/Epic official achievement integration.
- GTA Online compatibility, public matchmaking, or paid server hosting.
