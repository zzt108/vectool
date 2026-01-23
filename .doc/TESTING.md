# Testing Guide

## Overview

VecTool uses **NUnit** for unit and integration testing. We prioritize fast, reliable tests to ensure the "Core" logic and "Handlers" work correctly without launching the UI.

## Running Tests

### From Command Line

```bash
dotnet test UnitTests/UnitTests.csproj
```

### From VecTool (Dogfooding)

You can use VecTool to test itself!

1. Open VecTool.
2. Press **Ctrl+T**.
3. Watch the progress bar.

## Test Structure

- **UnitTests Project**: Contains all tests.
- **Fixtures**:
  - `DocTestBase`: Base class for testing Handlers. Creates a temporary file system.
  - `InMemorySettingsStore`: Mocks the configuration system.
  - `FakeClock`: Allows testing time-dependent logic (like retention policies).

## Guidelines

1. **No UI Dependencies**: Tests should not rely on WinForms components (except specialized UI logic tests running in STA).
2. **Mocking**: Use `Moq` (or manual mocks) for FileSystem and GitRunner where possible to speed up tests.
3. **Naming**: Use `Should_ExpectedBehavior_When_Condition`.

## CI/CD Service

Tests are automatically run on Push. See `.github/workflows` for details (if configured).
