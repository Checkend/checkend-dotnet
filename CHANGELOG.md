# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Make endpoint a required configuration option
- Fix website URL references

## [1.0.0] - 2024-12-28

### Added

- Initial release of Checkend .NET SDK
- Async error reporting via `Channel<T>` for non-blocking operation
- ASP.NET Core middleware integration with `UseCheckend()`
- Automatic context capture (request, user, custom context)
- Sensitive data filtering with built-in keys (password, token, api_key, etc.)
- Custom filter key support via `AddFilterKey()`
- Exception ignoring via `AddIgnoredException()`
- Before notify callbacks for modifying or filtering notices
- Testing utilities with `Testing.Setup()` and `Testing.Teardown()`
- Configuration via builder pattern, environment variables, or appsettings.json
- GitHub Actions CI workflow for automated testing
- NuGet publish workflow with manual trigger support
- Pre-commit hook to prevent secrets from being committed
