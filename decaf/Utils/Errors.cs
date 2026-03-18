using System;

namespace Decaf.Utils.Errors {
  class ErrorConstructor {
    public static string CreateError(Position position, string message) {
      return $"\u001b[1mFile \"{position.fileName}\":{position.line}:{position.column}\u001b[0m:\n{message}";
    }
  }
  // Lexing Errors
  // Parsing Errors
  // Scoping Errors
  namespace ScopeErrors {
    /// <summary>
    /// An exception to be thrown when a declaration already exists in the current scope during scope mapping.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public class DuplicateDeclarationException(
      Position position,
      string name
    ) : Exception(ErrorConstructor.CreateError(position, $"Duplicate declaration of '{name}' at {position}.")) {
      public Position Position { get; } = position;
      // TODO: Make sure we provide the correct error message
    }
    /// <summary>
    /// An exception to be thrown when a declaration is not found in the current scope or any parent scope during scope lookup.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public class DeclarationNotDefinedException(
      Position position,
      string message
    ) : Exception(ErrorConstructor.CreateError(position, message)) {
      // TODO: Make sure we provide the correct error message
      public Position Position { get; } = position;
    }
  }
  // Semantic Analysis Errors
  namespace SemanticErrors {
    /// <summary>
    /// An exception to be thrown when a semantic error is encountered during semantic analysis.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public class SemanticException(
      Position position,
      string message
    ) : Exception(ErrorConstructor.CreateError(position, message)) {
      // TODO: Make sure we provide the correct error message
      public Position Position { get; } = position;
    }
  }
  // Type Checking Errors
  // Code Generation Errors
}
