using System;
using System.Linq;

using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils.Errors.SemanticErrors;

namespace Decaf.Frontend {
  /// <summary>
  /// Ensures that the program is semantically valid. We could check some of this stuff while parsing however we 
  /// can provide better errors and simplify our parser by checking that certain constructs make logical sense instead of 
  /// just syntactic sense.
  /// 
  /// A few of the checks we are doing here are:
  /// - General
  ///   - Ensure that every program contains a module named `Program`
  ///     - This is required as the entry point of the program is the code in `Program`
  ///   - Ensure that `break` and `continue` statements are only used inside loops
  ///   - Ensure that `Function Literals` only appear as the direct rhs of a call node
  ///     - We allow functions to be treated as expressions but don't support first class functions (this solves that)
  ///   - Ensure that `Function Literals` only appear at the top level of a module (i.e. not inside of functions or blocks)
  ///   - Ensure that `return` statements only appear inside of functions
  /// - Math:
  ///   - Check for cases of `x / 0` where x is any expression and report an error about division by zero
  /// - Arrays:
  ///   - Ensure that array sizes are non-negative (when the size is a constant literal)
  ///   - Ensure that array indices are non-negative (when the index is a constant literal)
  /// </summary>
  public static class SemanticChecker {
    // The context that we pass around as we check the program
    private record struct Context {
      // The parent node of the current node we are checking
      public ParseTree.Node ParentNode;
      // Indicates weather we are currently inside of a function
      public bool InFunction;
      // Indicates weather we are currently inside of a loop
      public bool InLoop;
    }
    // --- Code Units ---
    #region CodeUnits
    public static void CheckProgramNode(ParseTree.ProgramNode node) {
      // Ensure the program contains a module names `Program`
      if (!node.Modules.Any(m => m.Name.Name == "Program")) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, "A program must contain a module called 'Program'");
      }
      // Check each module
      foreach (var mod in node.Modules) {
        // Create a context for each module
        var ctx = new Context { ParentNode = node, InFunction = false, InLoop = false };
        CheckModuleNode(ctx, mod);
      }
    }
    private static void CheckModuleNode(Context parentCtx, ParseTree.ModuleNode node) {
      // NOTE: We no longer do a check for `Program.Main()` as the body of the module is `Program.Main()`
      // Check the module body
      var ctx = parentCtx with { ParentNode = node };
      // NOTE: Instead of checking the module body as a block statement, we check individually so the parents make more sense
      foreach (var stmt in node.Body.Statements) {
        CheckStatementNode(ctx, stmt);
      }
    }
    #endregion
    // --- Statements ---
    #region Statements
    private static void CheckStatementNode(Context parentCtx, ParseTree.StatementNode node) {
      switch (node) {
        case ParseTree.StatementNode.BlockNode blockNode:
          CheckBlockStatementNode(parentCtx, blockNode);
          break;
        case ParseTree.StatementNode.VariableDeclNode variableDeclNode:
          CheckVariableDeclarationStatementNode(parentCtx, variableDeclNode);
          break;
        case ParseTree.StatementNode.AssignmentNode assignmentNode:
          CheckAssignmentStatementNode(parentCtx, assignmentNode);
          break;
        case ParseTree.StatementNode.IfNode ifNode:
          CheckIfStatementNode(parentCtx, ifNode);
          break;
        case ParseTree.StatementNode.WhileNode whileNode:
          CheckWhileStatementNode(parentCtx, whileNode);
          break;
        case ParseTree.StatementNode.ReturnNode returnNode:
          CheckReturnStatementNode(parentCtx, returnNode);
          break;
        case ParseTree.StatementNode.ContinueNode continueNode:
          CheckContinueStatementNode(parentCtx, continueNode);
          break;
        case ParseTree.StatementNode.BreakNode breakNode:
          CheckBreakStatementNode(parentCtx, breakNode);
          break;
        case ParseTree.StatementNode.ExprStatementNode exprNode:
          CheckExpressionStatementNode(parentCtx, exprNode);
          break;
        // NOTE: This should be impossible unless we forget to update the mapper when adding statements
        default: throw new Exception($"Unknown statement node type: {node.Kind}");
      }
    }
    private static void CheckBlockStatementNode(Context parentCtx, ParseTree.StatementNode.BlockNode node) {
      // Check each statement
      var ctx = parentCtx with { ParentNode = node };
      foreach (var stmt in node.Statements) CheckStatementNode(ctx, stmt);
    }
    private static void CheckVariableDeclarationStatementNode(Context parentCtx, ParseTree.StatementNode.VariableDeclNode node) {
      var ctx = parentCtx with { ParentNode = node };
      // Check each variable bind
      foreach (var bind in node.Binds) {
        CheckLocationNode(ctx, bind.Name);
        CheckExpressionNode(ctx, bind.InitExpr);
        // We only allow functions to be defined at the top level of a module
        if (
          bind.InitExpr is ParseTree.ExpressionNode.LiteralExprNode { Literal: ParseTree.LiteralNode.FunctionNode } &&
          parentCtx.ParentNode is not ParseTree.ModuleNode
        ) {
          // TODO: Give this a unique error from `utils/errors`
          throw new SemanticException(bind.InitExpr.Position, "Functions can only be defined at the top level of a module");
        }
      }
    }
    private static void CheckAssignmentStatementNode(Context parentCtx, ParseTree.StatementNode.AssignmentNode node) {
      var ctx = parentCtx with { ParentNode = node };
      CheckLocationNode(ctx, node.Location);
      CheckExpressionNode(ctx, node.Expression);
    }
    private static void CheckIfStatementNode(Context parentCtx, ParseTree.StatementNode.IfNode node) {
      var ctx = parentCtx with { ParentNode = node };
      CheckExpressionNode(ctx, node.Condition);
      CheckStatementNode(ctx, node.TrueBranch);
      if (node.FalseBranch != null) CheckStatementNode(ctx, node.FalseBranch);
    }
    private static void CheckWhileStatementNode(Context parentCtx, ParseTree.StatementNode.WhileNode node) {
      var ctx = parentCtx with { ParentNode = node, InLoop = true };
      CheckExpressionNode(ctx, node.Condition);
      CheckStatementNode(ctx, node.Body);
    }
    private static void CheckReturnStatementNode(Context parentCtx, ParseTree.StatementNode.ReturnNode node) {
      // Ensure that return statements only appear inside of functions
      if (!parentCtx.InFunction) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, "Return statements must be inside a function");
      }
      if (node.Value != null) CheckExpressionNode(parentCtx, node.Value);
    }
    private static void CheckContinueStatementNode(Context parentCtx, ParseTree.StatementNode.ContinueNode node) {
      // Ensure `continue` statements only appear inside of loops
      if (!parentCtx.InLoop) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, "Continue statements must be inside a loop");
      }
    }
    private static void CheckBreakStatementNode(Context parentCtx, ParseTree.StatementNode.BreakNode node) {
      // Ensure `break` statements only appear inside of loops
      if (!parentCtx.InLoop) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, "Break statements must be inside a loop");
      }
    }
    private static void CheckExpressionStatementNode(Context parentCtx, ParseTree.StatementNode.ExprStatementNode node) {
      // NOTE: We don't update the parentNode here as we know it's an expression (weather its a statement or not doesn't matter)
      CheckExpressionNode(parentCtx, node.Expression);
    }
    #endregion
    // --- Expressions ---
    #region Expressions
    private static void CheckExpressionNode(Context parentCtx, ParseTree.ExpressionNode node) {
      switch (node) {
        case ParseTree.ExpressionNode.PrefixNode prefixNode:
          CheckPrefixExpressionNode(parentCtx, prefixNode);
          break;
        case ParseTree.ExpressionNode.BinopNode binopNode:
          CheckBinopExpressionNode(parentCtx, binopNode);
          break;
        case ParseTree.ExpressionNode.CallNode callNode:
          CheckCallExpressionNode(parentCtx, callNode);
          break;
        case ParseTree.ExpressionNode.ArrayInitNode arrayInitNode:
          CheckArrayInitExpressionNode(parentCtx, arrayInitNode);
          break;
        case ParseTree.ExpressionNode.LocationExprNode locationNode:
          CheckLocationExpressionNode(parentCtx, locationNode);
          break;
        case ParseTree.ExpressionNode.LiteralExprNode literalNode:
          CheckLiteralExpressionNode(parentCtx, literalNode);
          break;
        // NOTE: This should be impossible unless we forget to update the checker when adding expressions
        default: throw new Exception($"Unknown expression node type: {node.Kind}");
      }
    }
    private static void CheckPrefixExpressionNode(Context parentCtx, ParseTree.ExpressionNode.PrefixNode node) {
      var ctx = parentCtx with { ParentNode = node };
      CheckExpressionNode(ctx, node.Operand);
    }
    private static void CheckBinopExpressionNode(Context parentCtx, ParseTree.ExpressionNode.BinopNode node) {
      var ctx = parentCtx with { ParentNode = node };
      CheckExpressionNode(ctx, node.Lhs);
      CheckExpressionNode(ctx, node.Rhs);
      // Check for division by constant literal zero
      if (
        node.Operator == IR.Operators.BinaryOperator.Divide &&
        node.Rhs is ParseTree.ExpressionNode.LiteralExprNode { Literal: ParseTree.LiteralNode.IntegerNode { Value: 0 } }
      ) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, "Division by zero is not allowed.");
      }
    }
    private static void CheckCallExpressionNode(Context parentCtx, ParseTree.ExpressionNode.CallNode node) {
      var ctx = parentCtx with { ParentNode = node };
      CheckLocationNode(ctx, node.Callee);
      foreach (var arg in node.Arguments) CheckExpressionNode(ctx, arg);
    }
    private static void CheckArrayInitExpressionNode(Context parentCtx, ParseTree.ExpressionNode.ArrayInitNode node) {
      var ctx = parentCtx with { ParentNode = node };
      CheckExpressionNode(ctx, node.SizeExpr);
      // Check for negative array size when the size is a constant literal
      if (
        node.SizeExpr is ParseTree.ExpressionNode.LiteralExprNode { Literal: ParseTree.LiteralNode.IntegerNode { Value: < 0 } }
      ) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, $"Array size must be non-negative");
      }
    }
    private static void CheckLocationExpressionNode(Context parentCtx, ParseTree.ExpressionNode.LocationExprNode node) {
      // NOTE: We don't update the parent node here as we know it's a location expression (and not a call or something else that also has a location)
      CheckLocationNode(parentCtx, node.Location);
    }
    private static void CheckLiteralExpressionNode(Context parentCtx, ParseTree.ExpressionNode.LiteralExprNode node) {
      // NOTE: We don't need to check literal nodes as they are always valid (and we check for invalid literals during parsing)
      CheckLiteralNode(parentCtx, node.Literal);
    }
    #endregion
    // --- Literals ---
    #region Literals
    private static void CheckLiteralNode(Context parentCtx, ParseTree.LiteralNode node) {
      switch (node) {
        // Nothing to check for most literals
        case ParseTree.LiteralNode.IntegerNode:
        case ParseTree.LiteralNode.BooleanNode:
        case ParseTree.LiteralNode.CharacterNode:
        case ParseTree.LiteralNode.StringNode: break;

        case ParseTree.LiteralNode.FunctionNode functionNode:
          CheckFunctionLiteralNode(parentCtx, functionNode);
          break;
        // NOTE: This should be impossible unless we forget to update the checker when adding literals
        default: throw new Exception($"Unknown literal node type: {node.Kind}");
      }
    }
    private static void CheckFunctionLiteralNode(Context parentCtx, ParseTree.LiteralNode.FunctionNode node) {
      // Ensure functions only appear as the direct rhs of a variable declaration
      if (
        parentCtx.ParentNode is not ParseTree.StatementNode.VariableDeclNode
        // Sanity check that it is top level (This would be caught by the checks on VariableDeclNode)
        || parentCtx.InFunction || parentCtx.InLoop
      ) {
        // TODO: Give this a unique error from `utils/errors`
        throw new SemanticException(node.Position, "Functions may only be used as the initializer of a bind");
      }
      // Create a brand new context (functions don't inherit the context of their parent as they are self contained)
      var ctx = new Context { ParentNode = node, InFunction = true, InLoop = false };
      // Check the method body
      CheckBlockStatementNode(ctx, node.Body);
    }
    #endregion
    // --- Locations ---
    #region Locations
    private static void CheckLocationNode(Context parentCtx, ParseTree.LocationNode node) {
      switch (node) {
        case ParseTree.LocationNode.ArrayNode arrayNode: {
            var ctx = parentCtx with { ParentNode = node };
            CheckLocationNode(ctx, arrayNode.Root);

            if (arrayNode.IndexExpr != null) CheckExpressionNode(ctx, arrayNode.IndexExpr);
            // Ensure that array indices are non-negative when the index is a constant literal
            if (
              arrayNode.IndexExpr is ParseTree.ExpressionNode.LiteralExprNode {
                Literal: ParseTree.LiteralNode.IntegerNode { Value: < 0 }
              }
            ) {
              // TODO: Give this a unique error from `utils/errors`
              throw new SemanticException(arrayNode.Position, $"Array index must be non-negative");
            }
            break;
          }
        case ParseTree.LocationNode.MemberNode memberNode: {
            var ctx = parentCtx with { ParentNode = node };
            CheckLocationNode(ctx, memberNode.Root);
            break;
          }
        // Nothing to check
        case ParseTree.LocationNode.IdentifierNode:
          break;
        // NOTE: This should be impossible unless we forget to update the checker when adding locations
        default: throw new Exception($"Unknown location node type: {node.Kind}");
      }
    }
    #endregion
  }
}
