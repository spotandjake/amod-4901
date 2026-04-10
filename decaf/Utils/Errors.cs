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
    }
    /// <summary>
    /// An exception to be thrown when a declaration is not found in the current scope or any parent scope during scope lookup.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public class DeclarationNotDefinedException(
      Position position,
      string message
    ) : Exception(ErrorConstructor.CreateError(position, $"Declaration of `{message}` not found during scope lookup.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a declaration is not mutable but is being assigned to during scope lookup.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public class DeclarationNotMutableException(
      Position position,
      string message
    ) : Exception(ErrorConstructor.CreateError(position, $"`{message}` cannot be assigned as it is not mutable.")) {
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
      public Position Position { get; } = position;
    }
  }
  // Type Checking Errors
  namespace TypeCheckingErrors {
    /// <summary>
    /// An exception to be thrown when the left hand side and right hand side of an expression do not match during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="expected">The expected type category.</param>
    /// <param name="received">The received type category.</param>
    public class LhsNotRhs(
      Position position,
      string expected,
      string received
    ) : Exception(ErrorConstructor.CreateError(position, $"Expected {expected}, but received {received}.")) {
    }
    /// <summary>
    /// An exception to be thrown when a call happens on a non-method type during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class CallOnNonMethod(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "A call can only be performed on a method type.")) {
    }
    /// <summary>
    /// An exception to be thrown when a member access is performed on a non-module type during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class MemberAccessOnNonModule(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "A member access can only be performed on a module.")) {
    }
    /// <summary>
    /// An exception to be thrown when a member access is performed and the module does not have the member being accessed during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="memberName">The name of the member being accessed.</param>
    public class MemberAccessUnknown(
      Position position,
      string memberName
    ) : Exception(ErrorConstructor.CreateError(position, $"The member `{memberName}` does not exist on the module being accessed.")) {
    }
    /// <summary>
    /// An exception to be thrown when an array access is performed on a non-array type during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class ArrayAccessOnNonArray(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "An array access can only be performed on an array type.")) {
    }
    /// <summary>
    /// An exception to be thrown when a `return` statement is encountered outside of a method during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class ReturnUseOutsideOfMethod(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "`return` statement must be within a method.")) {
    }
    /// <summary>
    /// An exception to be thrown when a primitive callout is encountered with an unknown name during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="name">The name of the unknown primitive callout.</param>
    public class UnknownPrimitiveCall(
      Position position,
      string name
    ) : Exception(ErrorConstructor.CreateError(position, $"Unknown primitive callout: `{name}`.")) {
    }
    // Code Generation Errors
  }


  public static class ErrorHandler {
    public static bool HandleError(bool debug, Exception exn) {
      switch (exn) {
        // Lexer
        case System.Data.SyntaxErrorException e:
          Console.WriteLine(e.Message);
          break;
        // Parser
        case Antlr4.Runtime.Misc.ParseCanceledException e:
          Console.WriteLine($"Parsing failed: {e.InnerException?.Message ?? e.Message}");
          break;
        // Scoping
        case ScopeErrors.DuplicateDeclarationException e:
          Console.WriteLine(e.Message);
          break;
        case ScopeErrors.DeclarationNotDefinedException e:
          Console.WriteLine(e.Message);
          break;
        case ScopeErrors.DeclarationNotMutableException e:
          Console.WriteLine(e.Message);
          break;
        // Semantic Analysis
        case SemanticErrors.SemanticException e:
          Console.WriteLine(e.Message);
          break;
        // Type Checking
        case TypeCheckingErrors.LhsNotRhs e:
          Console.WriteLine(e.Message);
          break;
        case TypeCheckingErrors.CallOnNonMethod e:
          Console.WriteLine(e.Message);
          break;
        case TypeCheckingErrors.MemberAccessOnNonModule e:
          Console.WriteLine(e.Message);
          break;
        case TypeCheckingErrors.MemberAccessUnknown e:
          Console.WriteLine(e.Message);
          break;
        case TypeCheckingErrors.ArrayAccessOnNonArray e:
          Console.WriteLine(e.Message);
          break;
        case TypeCheckingErrors.ReturnUseOutsideOfMethod e:
          Console.WriteLine(e.Message);
          break;
        case TypeCheckingErrors.UnknownPrimitiveCall e:
          Console.WriteLine(e.Message);
          break;
        // Unknown
        default:
          if (debug) return true;
          else Console.WriteLine($"Compilation failed: {exn.Message}");
          break;
      }
      // By default we don't ever rethrow
      return false;
    }
  }
}
