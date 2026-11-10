# Security Policy

## Supported versions

Fouroam Server is currently in **planning / pre-alpha**. No executable release is available and no runtime version is supported yet.

Once the project publishes working code, this section will state which versions receive security updates.

## Reporting a vulnerability

**Do not open public issues with exploit instructions, secret values, or private network information.**

Email **athiliorecovery@gmail.com** with the subject `Fouroam Server — Security Report`. If private vulnerability reporting becomes available on GitHub, that channel may also be used.

Include:

- A summary of the vulnerability and its possible impact.
- The affected version, component or commit, if known.
- Minimal steps to reproduce.
- Logs or screenshots with credentials, IP addresses and personal details removed.

The maintainers will assess reports privately. No response-time guarantee is offered while the project is in planning.

## Security priorities for development

- Validate incoming packets and message lengths.
- Prevent unauthorized session joins and enforce the four-player limit.
- Guard against malformed input, denial of service and unsafe deserialization.
- Avoid leaking user data in logs or diagnostics.
- Verify dependency and release integrity.

This policy covers **original Fouroam Server software** only. It does not authorize testing third-party game platforms, services, VPN infrastructure or other players.
