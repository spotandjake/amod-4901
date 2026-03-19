using Decaf.IR.ParseTree;
using System;
using System.Linq;
using System.Reflection;

namespace MiddleEnd {
  // Possible exceptions that can be thrown during semantic analysis
  // TODO: Add position information to these exceptions for better error reporting
  public class SemanticException(string message) : Exception(message) { }
  // Performs semantic analysis on the parse tree, ensuring that the program is semantically valid
  public class SemanticChecker {
    private SemanticChecker() { }
    public static void CheckProgramNode(ProgramNode program) {
      // Ensure the program contains a class named "Program"
      if (!program.Classes.Any(m => m.Name == "Program")) {
        throw new SemanticException("A program must contain a class called 'Program'");
      }

      foreach (var _class in program.Classes) {
        CheckClassNode(_class);
      }
    }
    private static void CheckClassNode(DeclarationNode.ClassNode _class) {
      // Ensure the Program class contains a method named Main
      if (_class.Name == "Program") {
        const string mainName = "Main";
        // Find the Main method in the Program class
        var mainMethodCandidates = _class.Methods.Where(m => m.Name == mainName);
        if (mainMethodCandidates.Count() != 1) {
          throw new SemanticException("A main entry point must exist at `Program.Main()`");
        }
        // We get the first, note that the list is guaranteed to have exactly one element due to the previous check
        var mainMethod = mainMethodCandidates.First(m => m.Name == mainName);
        if (mainMethod.Parameters.Length != 0) {
          throw new SemanticException("`Program.Main()` should not accept any parameters");
        }
        if (!(mainMethod.ReturnType.Type is TypeNode.PrimitiveType.Void)) {
          throw new SemanticException("`Program.Main()` must return void");
        }
        foreach (var method in _class.Methods) {
          CheckMethodNode(method);
        }
      }
    }

    private static void CheckMethodNode(DeclarationNode.MethodNode _methodDecl) {
      var parentContext = ParentContext.Default();
      CheckBlockNode(_methodDecl.Body, parentContext);
    }

    private static void CheckBlockNode(BlockNode block, ParentContext context) {
      foreach (var statement in block.Statements) {
        CheckStatementNode(statement, context);
      }
    }

    private static void CheckStatementNode(StatementNode statement, ParentContext context) {
      switch (statement) {
        case StatementNode.AssignmentNode assign:
          CheckExpressionNode(assign.Expression, context);
          break;
        case StatementNode.ExprNode expr:
          CheckExpressionNode(expr.Content, context);
          break;
        case StatementNode.IfNode ifNode:
          CheckExpressionNode(ifNode.Condition, context);
          break;
        case StatementNode.ReturnNode ret:
          if (ret.Value != null)
            CheckExpressionNode(ret.Value, context);
          break;
        case StatementNode.WhileNode whileNode:
          CheckExpressionNode(whileNode.Condition, context);
          CheckBlockNode(whileNode.Body, context);
          break;
      }
    }

    private static void CheckExpressionNode(ExpressionNode expression, ParentContext context) {
      switch (expression) {
        case ExpressionNode.CallNode call:
          var innerCtx = new ParentContext { InPrimCall = call.IsPrimitive };
          foreach (var arg in call.Arguments) {
            CheckExpressionNode(arg, innerCtx);
          }
          break;

        case ExpressionNode.BinopNode binop:
          CheckExpressionNode(binop.Lhs, context);
          CheckExpressionNode(binop.Rhs, context);

          if (binop.Operator == "/" && binop.Rhs is LiteralNode.IntegerNode { Value: 0 }) {
            throw new SemanticException("Division by zero is not allowed.");
          }
          break;

        case ExpressionNode.PrefixNode prefix:
          CheckExpressionNode(prefix.Operand, context);
          break;

        case ExpressionNode.LocationNode location:
          CheckExpressionNode(location.Root, context);
          if (location.IndexExpr != null) {
            if (location.IndexExpr is LiteralNode.IntegerNode { Value: < 0 } negativeIndex)
              throw new SemanticException($"Array index must be non-negative: {negativeIndex.Value}");
            CheckExpressionNode(location.IndexExpr, context);
          }
          break;

        case LiteralNode.StringNode:
          if (!context.InPrimCall)
            throw new SemanticException("String literals may only appear as arguments to a primitive call");
          break;
      }
    }

    private struct ParentContext {
      public bool InPrimCall { get; init; }

      public static ParentContext Default() => new ParentContext {
        InPrimCall = false
      };
    }

  }
}
