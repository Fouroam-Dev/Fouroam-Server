# Contributing to Fouroam Server

Thank you for helping improve Fouroam Server.

> **Status: planning / pre-alpha.** No working server or supported build exists yet. Documentation, networking proposals, standalone prototypes and security review are the most useful contributions right now.

## Before getting started

1. Read the [README](README.md), [roadmap](ROADMAP.md), and [Code of Conduct](CODE_OF_CONDUCT.md).
2. Check [GitHub Issues](https://github.com/Fouroam-Dev/Fouroam-Server/issues).
3. Open an issue before making large changes or changing the architecture.
4. Clearly label designs as **planned**, experiments as **experimental**, and working features as **implemented**.

## Submit a pull request

1. Fork `Fouroam-Dev/Fouroam-Server`.
2. Create a focused branch, such as `docs/readme-update` or `feat/network-prototype`.
3. Make the change and add tests or verification notes where relevant.
4. Push the branch to your fork.
5. Open a Pull Request targeting `Fouroam-Dev/Fouroam-Server:main`.
6. Explain the purpose, implementation, tests and limitations.

## Standards

- Prefer small, clear and reviewable changes.
- Validate all network input and follow secure protocol design.
- Never commit access tokens, VPN passwords, private data or generated build artifacts.
- Do not add unexpected telemetry or external network calls.
- Documentation and example commands must reflect code that actually exists.
- Respect third-party licenses and keep required notices.

## Legal boundaries

Only submit original code or work you have the right to distribute under MIT. Do **not** submit proprietary game executables or extracted assets, leaked source code, pirated game content, DRM/authentication bypasses or anti-cheat bypasses. Any proposed GTA V integration and official story features require applicable permissions review before implementation or distribution.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/). Examples:

- `docs: explain local hosting model`
- `feat(network): add standalone handshake prototype`
- `fix(network): handle dropped connections`
- `test(network): cover session capacity limits`

## Pull request checklist

- [ ] The change has a clear scope and explanation.
- [ ] Relevant documentation and tests have been updated.
- [ ] Planned features are not presented as finished.
- [ ] No secrets, personal data or unauthorized assets are included.
- [ ] Any reused code is properly licensed and attributed.

## Joining the team

We also welcome developers interested in contributing to the wider Fouroam Dev organization. Contact **athiliorecovery@gmail.com** or Discord **`lu.cao`** with your background and GitHub profile.

Security issues must be reported privately using [SECURITY.md](SECURITY.md).
