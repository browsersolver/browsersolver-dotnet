# Security Policy

## Supported versions

Security fixes are released for the latest published version of the BrowserSolver .NET SDK.

## Reporting a vulnerability

Please do not report security vulnerabilities through public GitHub issues.

Email **contact@browsersolver.com** with a description of the issue, the affected version, and steps to reproduce. We acknowledge reports within three business days and keep you informed until the issue is resolved.

## Handling API keys

- Keep your BrowserSolver API key out of source control. Load it from the `BROWSERSOLVER_API_KEY` environment variable or a secret manager.
- Rotate the key from the [BrowserSolver dashboard](https://app.browsersolver.com) if you believe it was exposed.
