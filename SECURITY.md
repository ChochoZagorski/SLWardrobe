# Security Policy

## Supported Versions

Only the latest stable release receives security updates. Testing releases (any version marked `2.x.x` in the console output (until an actual 2.0.0 release happens...)) are not covered.

| Version | Supported          |
| ------- | ------------------ |
| From `1.8.0` onwards | ✅ |
| `1.7.0` and older | ❌ |
| Testing builds (`2.x.x`) | ❌ |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, use GitHub's private vulnerability reporting:

1. Go to the [Security tab](https://github.com/ChochoZagorski/SLWardrobe/security) of this repository
2. Click **"Report a vulnerability"**
3. Fill out the advisory form with as much detail as possible

This keeps the report confidential until a fix is ready.

### What to Include

To help triage the report quickly, please provide:

- A description of the vulnerability and its potential impact
- The affected version(s)
- Full paths of any relevant source files
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or example exploit (if applicable)
- Any special server configuration required to trigger the issue

### What to Expect

- An acknowledgement within **72 hours**
- Updates on triage and fix progress as they happen
- Credit in the release notes if you'd like it (just let me know)

If the vulnerability is accepted, a patch will be issued as soon as possible and a GitHub Security Advisory will be published after the fix is live. If it is declined, you will receive an explanation.

## Scope

This policy covers the SLWardrobe plugin itself. Vulnerabilities in third-party dependencies ([EXILED](https://github.com/ExMod-Team/EXILED), [LabAPI](https://github.com/northwood-studios/LabAPI), [ProjectMER](https://github.com/Michal78900/ProjectMER)) should be reported to their respective maintainers.

## Preferred Language

English.
