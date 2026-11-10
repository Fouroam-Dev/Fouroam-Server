# Fouroam Server Roadmap

This is a **proposed roadmap**, not a set of delivered features or deadlines.

## Milestone 0 — Project foundation

- [x] Publish the README and community documentation.
- [x] Keep a MIT license for original work.
- [ ] Confirm licensing and platform permissions for any GTA V integration.
- [ ] Validate the initial technology stack and supported Windows versions.

## Milestone 1 — Standalone networking server

- [ ] Create a .NET solution and a basic Windows server.
- [ ] Listen on a configurable host interface and port.
- [ ] Define a small versioned network protocol.
- [ ] Validate inputs and assign connection/session identifiers.
- [ ] Limit sessions to four participants.
- [ ] Handle clean disconnects and connection failures.

## Milestone 2 — LAN and VPN validation

- [ ] Build an independent console test client.
- [ ] Connect two computers on the same LAN.
- [ ] Repeat the test using a private VPN, such as Radmin VPN.
- [ ] Test four concurrent connections and reconnection behavior.
- [ ] Document configuration and firewall requirements.

## Milestone 3 — Real-time state research

- [ ] Evaluate UDP for time-sensitive messages.
- [ ] Design host authority, ordering and reliability behavior.
- [ ] Prototype synchronization independently from GTA V.
- [ ] Test under latency and packet loss.

## Milestone 4 — Game integration feasibility

- [ ] Identify an authorized and technically compatible integration method.
- [ ] Confirm support and limitations for game editions and versions.
- [ ] Document legal restrictions before distributing integrations.
- [ ] Evaluate original co-op activities within applicable permissions.

## Out of scope for initial releases

- Public server discovery or paid hosted services.
- GTA Online replacement, pirated copies or platform bypasses.
- Guaranteed official story campaign synchronization.
- Guaranteed official Steam or Epic Games achievements.
