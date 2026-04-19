using System;

namespace Decaf.Utils.Errors {
  class ErrorConstructor {
    public static string CreateError(Position position, string message) {
      var header = $"\u001b[1mFile \"{position.fileName}\":{position.line}:{position.column}\u001b[0m:";
      var body = $"\u001b[31mError\u001b[0m: {message}";
      return $"{header}\n{body}";
    }
  }
  // Lexing Errors
  namespace LexingErrors {
    public class UnrecognizedTokenException : Exception {
      public UnrecognizedTokenException(Position Position, string message) : base(
        ErrorConstructor.CreateError(Position, $"Unrecognized token, `{message}`")
      ) { }
    }
  }
  // Parsing Errors
  namespace ParsingErrors {
    public class UnrecognizedTokenException : Exception {
      public UnrecognizedTokenException(Position Position, string message) : base(
        ErrorConstructor.CreateError(Position, $"Unrecognized token, `{message}`")
      ) { }
    }
  }
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
    /// An exception to be thrown when the program module is not found during semantic analysis, as the program module serves as the entry point of the program and is required for execution.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class ProgramModuleNotFound(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "The program module was not found. Ensure that there is a module declared with the name `Program` to serve as the entry point.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a function literal is defined in an invalid context during semantic analysis, 
    /// as function literals can only be defined at the top level of a module.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class FunctionsCanOnlyBeDefinedAtTopLevelOfModule(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Functions can only be defined at the top level of a module.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a function literal is used in an invalid context during semantic analysis,
    /// as function literals can only be used as the initializer of a variable declaration and cannot be used 
    /// in other contexts such as being passed as an argument to a function call or being assigned to a
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class FunctionLiteralMustBeDirectRhsOfVarDecl(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Function literals must be the direct right hand side of a variable declaration.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a `return` statement is encountered outside of a 
    /// function during semantic analysis, as `return` statements can only be used within functions to return values.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class ReturnStatementOutsideOfFunction(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Return statements must be inside a function.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a `continue` statement is encountered outside of a loop 
    /// during semantic analysis, as `continue` statements can only be used within loops to skip to the next iteration.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class ContinueStatementOutsideOfLoop(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Continue statements must be inside a loop.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a `break` statement is encountered outside of a loop 
    /// during semantic analysis, as `break` statements can only be used within loops to exit the loop early.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class BreakStatementOutsideOfLoop(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Break statements must be inside a loop.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when a division by zero is encountered during semantic analysis,
    /// as division by zero is undefined and not allowed in the language.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class DivisionByZero(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Division by zero is not allowed.")) {
      public Position Position { get; } = position;
    }
    /// <summary>
    /// An exception to be thrown when an array is declared with a non-positive size during semantic analysis,
    /// as arrays must have a positive integer size to be valid.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class ArraySizeMustBePositive(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Array size must be a positive integer.")) {
      public Position Position { get; } = position;
    }
    public class ArrayIndexMustBeNonNegative(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Array index must be a non-negative integer.")) {
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
    /// An exception to be thrown when we are expecting the function to return but we can't find a return statement during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class NoReturnStatement(
      Position position,
      string functionName,
      string expectedReturnType
    ) : Exception(ErrorConstructor.CreateError(position, $"This function {functionName} is expected to return a value of type {expectedReturnType} but not all paths end in a return statement.")) {
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
    /// <summary>
    /// An exception to be thrown when a primitive is used incorrectly during type checking, 
    /// as primitives can only be used as function calls.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class InvalidPrimitiveUse(
      Position position
    ) : Exception(ErrorConstructor.CreateError(position, "Primitive used incorrectly, primitives can only be used as function calls.")) {
    }
    /// <summary>
    /// An exception to be thrown when an array is declared with an invalid element type during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="typeName">The type name of the invalid array element type.</param>
    public class InvalidArrayType(
      Position position,
      string typeName
    ) : Exception(
      ErrorConstructor.CreateError(
        position,
        $"Invalid array element type: {typeName}, only `int`, `boolean` and `character` are allowed as array element types."
      )
    ) { }
    /// <summary>
    /// An exception to be thrown when a variable is declared with type void during type checking.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    public class InvalidVoidBind(
      Position position
    ) : Exception(
      ErrorConstructor.CreateError(
        position,
        "Variables cannot be of type void."
      )
    ) { }
    /// <summary>
    /// An exception to be thrown when a bind is declared without an explicit type annotation 
    /// and the initializer is not a function literal during type checking, as type inference is only supported for function literals.
    /// </summary>
    /// <param name="position">The position where the error occurred.</param>
    /// <param name="bindName">The name of the bind.</param>
    public class ExpectedBindToHaveAType(
      Position position,
      string bindName
    ) : Exception(
      ErrorConstructor.CreateError(
        position,
        $"The bind `{bindName}` is expected to have an explicit type annotation, inference is only supported on functions."
      )
    ) { }
    // Code Generation Errors
  }


  public static class ErrorHandler {
    public static bool HandleError(bool debug, Exception exn) {
      switch (exn) {
        // Lexer
        case LexingErrors.UnrecognizedTokenException:
        // Parser
        case ParsingErrors.UnrecognizedTokenException:
        // Scoping
        case ScopeErrors.DuplicateDeclarationException:
        case ScopeErrors.DeclarationNotDefinedException:
        case ScopeErrors.DeclarationNotMutableException:
        // Semantic Analysis
        case SemanticErrors.ProgramModuleNotFound:
        case SemanticErrors.FunctionsCanOnlyBeDefinedAtTopLevelOfModule:
        case SemanticErrors.FunctionLiteralMustBeDirectRhsOfVarDecl:
        case SemanticErrors.ReturnStatementOutsideOfFunction:
        case SemanticErrors.ContinueStatementOutsideOfLoop:
        case SemanticErrors.BreakStatementOutsideOfLoop:
        case SemanticErrors.DivisionByZero:
        case SemanticErrors.ArraySizeMustBePositive:
        case SemanticErrors.ArrayIndexMustBeNonNegative:
        // Type Checking
        case TypeCheckingErrors.LhsNotRhs:
        case TypeCheckingErrors.CallOnNonMethod:
        case TypeCheckingErrors.MemberAccessOnNonModule:
        case TypeCheckingErrors.MemberAccessUnknown:
        case TypeCheckingErrors.ArrayAccessOnNonArray:
        case TypeCheckingErrors.ReturnUseOutsideOfMethod:
        case TypeCheckingErrors.NoReturnStatement:
        case TypeCheckingErrors.UnknownPrimitiveCall:
        case TypeCheckingErrors.InvalidPrimitiveUse:
        case TypeCheckingErrors.InvalidArrayType:
        case TypeCheckingErrors.InvalidVoidBind:
        case TypeCheckingErrors.ExpectedBindToHaveAType:
          Console.WriteLine(exn.Message);
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
