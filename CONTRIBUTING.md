# Contribution Guidelines

## Table of Contents
- [Code Conventions](#code-conventions)
  - [Action](#action)
  - [Constants](#constants)
  - [Func](#func)
  - [Private Field](#private-field)
  
## Code Conventions

### Action
- PascalCamelCase
  ```csharp
  Action<string> OutputMessage = message =>
      Console.WriteLine(message);
  ```

### Constant
- SCREAMING_SNAKE_CASE
  ```csharp
  const int EXAMPLE_CONSTANT = 1;
  ```

### Func
- PascalCamelCase
  ```csharp
  Func<string, bool> IsEmptyString = text =>
      String.IsNullOrWhiteSpace(text);
  ```

### Private Field
- leading underscore and camelCase 
  ```csharp
  private int _examplePrivateField = 1;
  ```
