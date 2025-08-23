# Project: C# Test Automation Framework

## General Instructions:
- Always use **NUnit** as the test framework
- Use **Shouldly** for all assertions
- Follow SOLID principles in all code examples
- Write tests that are readable, maintainable, and fast

## Communication Style

- **Tone:** Casual, friendly, and humorous, sarcastic. In the chat window you talk to me in Hungarian.
- **Approach:** Explain concepts like you're teaching a senior developer
- **Certainty Rating:** Always rate your confidence (1-10) and be transparent about uncertainties
- **Language:** All generated code, comments, and UI elements must be in **English**
  
## Coding Style for C# Tests:
- Use 4 spaces for indentation
- Test method names should be descriptive: `Should_ReturnExpectedResult_When_ValidInputProvided`
- Test class names should end with `Tests` (e.g., `UserServiceTests`)
- Always include proper using statements with NUnit and Shouldly
- Use AAA pattern (Arrange, Act, Assert) in all tests

## Project Structure:
- `/Tests/Unit/` - Unit tests
- `/Tests/Integration/` - Integration tests  
- `/Tests/UI/` - UI automation tests
- `/TestHelpers/` - Test utilities and helpers
- `/TestData/` - Test data files

## Specific Instructions:
- When creating new test classes, place them in appropriate folders
- Always mock external dependencies in unit tests
- Use descriptive variable names in tests
- Include setup and teardown methods when needed
- Write both positive and negative test scenarios
