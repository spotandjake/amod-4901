using Decaf.IR.ParseTree;
using System;
using System.Linq;

using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils.Errors.SemanticErrors;

namespace Decaf.MiddleEnd {
  // Performs semantic analysis on the parse tree, ensuring that the program is semantically valid
  public class SemanticChecker {
    private readonly record struct ParentContext(bool InPrimCall, bool InLoop) {
      public bool InPrimCall { get; } = InPrimCall;
      public bool InLoop { get; } = InLoop;
    }
    private SemanticChecker() { }
    public static void CheckProgramNode(ProgramNode program) {
      // Ensure the program contains a class named "Program"
      if (!program.Classes.Any(m => m.Name == "Program")) {
        throw new SemanticException(program.Position, "A program must contain a class called 'Program'");
      }
      // Check in each class
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
          throw new SemanticException(_class.Position, "A main entry point must exist at `Program.Main()`");
        }
        // We get the first, note that the list is guaranteed to have exactly one element due to the previous check
        var mainMethod = mainMethodCandidates.First(m => m.Name == mainName);
        if (mainMethod.Parameters.Length != 0) {
          throw new SemanticException(mainMethod.Position, "`Program.Main()` should not accept any parameters");
        }
        if (mainMethod.ReturnType.Type is not PrimitiveType.Void) {
          throw new SemanticException(mainMethod.Position, "`Program.Main()` must return void");
        }
      }
      // Check each method in the class
      foreach (var method in _class.Methods) {
        CheckMethodNode(method);
      }
    }
    private static void CheckMethodNode(DeclarationNode.MethodNode _methodDecl) {
      // Create a new context
      var parentContext = new ParentContext(false, false);
      // Check the method body
      CheckBlockNode(_methodDecl.Body, parentContext);
    }
    private static void CheckBlockNode(BlockNode block, ParentContext context) {
      // Check every statement in the block node
      foreach (var statement in block.Statements) {
        CheckStatementNode(statement, context);
      }
    }
    private static void CheckStatementNode(StatementNode statement, ParentContext parentContext) {
      switch (statement) {
        case StatementNode.AssignmentNode assign:
          // Check the path
          CheckLocationNode(assign.Location, parentContext);
          // Check The Expression
          CheckExpressionNode(assign.Expression, parentContext);
          break;
        case StatementNode.ExprNode expr:
          // Check the inner expression
          CheckExpressionNode(expr.Content, parentContext);
          break;
        case StatementNode.IfNode ifNode:
          // Check the condition
          CheckExpressionNode(ifNode.Condition, parentContext);
          // Check the true branch
          CheckBlockNode(ifNode.TrueBranch, parentContext);
          // Check the false branch if it exists
          if (ifNode.FalseBranch != null) {
            CheckBlockNode(ifNode.FalseBranch, parentContext);
          }
          break;
        case StatementNode.WhileNode whileNode: {
            var context = new ParentContext(parentContext.InPrimCall, true);
            // Check the condition
            CheckExpressionNode(whileNode.Condition, context);
            // Check the body
            CheckBlockNode(whileNode.Body, context);
            break;
          }
        case StatementNode.ContinueNode:
          // Check that the continue statement is inside a loop
          if (!parentContext.InLoop) {
            throw new SemanticException(statement.Position, "Continue statements must be inside a loop");
          }
          break;
        case StatementNode.BreakNode:
          // Check that the break statement is inside a loop
          if (!parentContext.InLoop) {
            throw new SemanticException(statement.Position, "Break statements must be inside a loop");
          }
          break;
        case StatementNode.ReturnNode ret:
          if (ret.Value != null) {
            CheckExpressionNode(ret.Value, parentContext);
          }
          break;
        default:
          throw new Exception($"Unknown statement node of type {statement.GetType()}");
      }
    }
    private static void CheckExpressionNode(ExpressionNode expression, ParentContext parentContext) {
      switch (expression) {
        case ExpressionNode.CallNode call: {
            // Update the context - Set IsPrimitive
            var context = new ParentContext(call.IsPrimitive, parentContext.InLoop);
            // Check the path
            CheckLocationNode(call.Path, context);
            // Check each argument
            foreach (var arg in call.Arguments) {
              CheckExpressionNode(arg, context);
            }
            break;
          }
        case ExpressionNode.BinopNode binop: {
            // Check the left hand side
            CheckExpressionNode(binop.Lhs, parentContext);
            // Check the right hand side
            CheckExpressionNode(binop.Rhs, parentContext);
            switch (binop.Operator) {
              // Check for cases of <x>/0 where x is any expression
              case "/":
                if (binop.Rhs is ExpressionNode.LiteralNode { Content: ParseTree.LiteralNodes.IntegerNode { Value: 0 } }) {
                  throw new SemanticException(binop.Position, "Division by zero is not allowed.");
                }
                break;
            }
            break;
          }
        case ExpressionNode.PrefixNode prefix:
          // Check Prefix operand
          // NOTE: It might make sense to warn if this were a constant `true` or `false`
          CheckExpressionNode(prefix.Operand, parentContext);
          break;
        case ExpressionNode.NewClassNode _classInit:
          // Check the path
          CheckLocationNode(_classInit.Path, parentContext);
          break;
        case ExpressionNode.NewArrayNode arrayInit:
          // Check size expression
          CheckExpressionNode(arrayInit.SizeExpr, parentContext);
          // An array cannot have a negative size
          if (
            arrayInit.SizeExpr is ExpressionNode.LiteralNode { Content: ParseTree.LiteralNodes.IntegerNode { Value: < 0 } }
          ) throw new SemanticException(arrayInit.Position, $"Array size must be non-negative");
          break;
        case ExpressionNode.LocationAccessNode locationNode:
          CheckLocationNode(locationNode.Content, parentContext);
          break;
        case ExpressionNode.LiteralNode literalNode:
          switch (literalNode.Content) {
            case ParseTree.LiteralNodes.StringNode:
              if (!parentContext.InPrimCall) {
                throw new SemanticException(literalNode.Position, "String literals can only be used as arguments to primitive calls");
              }
              break;
          }
          break;
        default:
          throw new Exception($"Unknown expression node of type {expression.GetType()}");
      }
    }
    private static void CheckLocationNode(LocationNode location, ParentContext parentContext) {
      switch (location) {
        case LocationNode.ThisNode _:
        case LocationNode.IdentifierAccessNode _:
          // Nothing to check here
          break;
        case LocationNode.MemberAccessNode fieldAccessNode:
          CheckLocationNode(fieldAccessNode.Root, parentContext);
          break;
        case LocationNode.ArrayAccessNode arrayAccessNode:
          // Check the expression
          if (arrayAccessNode.IndexExpr != null) {
            CheckExpressionNode(arrayAccessNode.IndexExpr, parentContext);
          }
          // Array indices cannot be negative
          if (
            arrayAccessNode.IndexExpr is ExpressionNode.LiteralNode { Content: ParseTree.LiteralNodes.IntegerNode { Value: < 0 } }
          ) {
            throw new SemanticException(location.Position, $"Array index must be non-negative");
          }
          break;
        default: throw new Exception($"Unknown location node type: {location.Kind}");
      }
    }
  }
}
