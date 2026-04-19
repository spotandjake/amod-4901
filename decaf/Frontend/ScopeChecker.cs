using System;

using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.ScopeErrors;

// TODO: This should assign a unique LocationReference to every location so we are not using strings to track variable usage, this should also resolve primitives

namespace Decaf.Frontend {
  /// <summary>
  /// The ScopeChecker is responsible for traversing the parse tree and validating variable usage.
  /// 
  /// Originally the intention of this class was to be a ScopeMapper that would attach the scope tables
  /// to the nodes on the way up, however as the information we track is only relevant at each stage of the pipeline,
  /// we found that the scope tables weren't really being re-used and instead its simpler to rebuild them. 
  /// 
  /// The downsides of this approach is that we now have to re-traverse the tree in later stages and rebuild the scope tables,
  /// which means that we need to ensure scoping rules are correctly implemented across multiple stages of the pipeline, however
  /// the upside is that our scopes are always up to date with the current state of the tree which means we don't have to worry about
  /// scope tables getting out of sync with the tree as we perform transformations. Additionally our scoping rules are very 
  /// straightforward so keeping them in sync doesn't pose much of a challenge.
  /// </summary>
  public static class ScopeChecker {
    // --- Code Units ---
    #region CodeUnits
    /// <summary>
    /// Ensures that the given program node is well-scoped and analyses mutation information for variables and fields.
    /// </summary>
    /// <param name="program">The program to check</param>
    /// <exception cref="DuplicateDeclarationException">When a declaration is found to be a duplicate.</exception>
    /// <exception cref="DeclarationNotDefinedException">When a declaration is being used but not defined.</exception>
    /// <exception cref="DeclarationNotMutableException">When a declaration is being mutated but is not mutable.</exception>
    public static void CheckProgramNode(ParseTree.ProgramNode node) {
      // Initialize our global program scope
      var globalScope = new Scope<bool>(null);
      // Check each module
      foreach (var module in node.Modules) CheckModuleNode(module, globalScope);
    }
    private static void CheckModuleNode(ParseTree.ModuleNode node, Scope<bool> parentScope) {
      // Add the module declaration to the global scope (We do this before checking the module body to allow for recursive modules)
      parentScope.AddDeclaration(node.Position, node.Name.Name, false);
      // Create a new scope for the module
      var moduleScope = new Scope<bool>(parentScope);
      // Check the module imports
      foreach (var imp in node.Imports) CheckImportNode(imp, moduleScope);
      // Check the module body
      CheckBlockStatementNode(node.Body, moduleScope);
    }
    private static void CheckImportNode(ParseTree.ImportNode node, Scope<bool> parentScope) {
      // Add the import declaration to the module scope
      parentScope.AddDeclaration(node.Position, node.Name.Name, false);
    }
    #endregion
    // --- Statement Nodes ---
    #region StatementNodes
    private static void CheckStatementNode(ParseTree.StatementNode node, Scope<bool> parentScope) {
      switch (node) {
        case ParseTree.StatementNode.BlockNode blockNode:
          CheckBlockStatementNode(blockNode, parentScope);
          break;
        case ParseTree.StatementNode.VariableDeclNode variableDeclNode:
          CheckVariableDeclarationStatementNode(variableDeclNode, parentScope);
          break;
        case ParseTree.StatementNode.AssignmentNode assignmentNode:
          CheckAssignmentStatementNode(assignmentNode, parentScope);
          break;
        case ParseTree.StatementNode.IfNode ifNode:
          CheckIfStatementNode(ifNode, parentScope);
          break;
        case ParseTree.StatementNode.WhileNode whileNode:
          CheckWhileStatementNode(whileNode, parentScope);
          break;
        case ParseTree.StatementNode.ReturnNode returnNode:
          CheckReturnStatementNode(returnNode, parentScope);
          break;
        case ParseTree.StatementNode.ExprStatementNode exprNode:
          CheckExpressionStatementNode(exprNode, parentScope);
          break;
        // NOTE: There is nothing to check for these statements as they don't touch the scope
        case ParseTree.StatementNode.ContinueNode:
        case ParseTree.StatementNode.BreakNode:
          break;
        // NOTE: This should be impossible unless we forget to update the mapper when adding statements
        default: throw new Exception($"Unknown statement node type: {node.Kind}");
      }
    }
    private static void CheckBlockStatementNode(ParseTree.StatementNode.BlockNode node, Scope<bool> parentScope) {
      // Create a new scope for the block
      var scope = new Scope<bool>(parentScope);
      // Check the statements
      foreach (var stmt in node.Statements) CheckStatementNode(stmt, scope);
    }
    private static void CheckVariableDeclarationStatementNode(ParseTree.StatementNode.VariableDeclNode node, Scope<bool> parentScope) {
      // Add the binds to scope
      foreach (var bind in node.Binds) {
        // NOTE: All binds are mutable except for function binds which cannot be reassigned
        var isMutable = !(bind.InitExpr is ParseTree.ExpressionNode.LiteralExprNode { Literal: ParseTree.LiteralNode.FunctionNode });
        // NOTE: We add the bind before checking the initializer expression to allow for recursive definitions
        parentScope.AddDeclaration(bind.Position, bind.Name.Name, isMutable);
        // Check the initializer expression
        CheckExpressionNode(bind.InitExpr, parentScope);
      }
    }
    private static void CheckAssignmentStatementNode(ParseTree.StatementNode.AssignmentNode node, Scope<bool> parentScope) {
      CheckLocationNode(mutating: true, node.Location, parentScope);
      CheckExpressionNode(node.Expression, parentScope);
    }
    private static void CheckIfStatementNode(ParseTree.StatementNode.IfNode node, Scope<bool> parentScope) {
      CheckExpressionNode(node.Condition, parentScope);
      CheckStatementNode(node.TrueBranch, parentScope);
      if (node.FalseBranch != null) CheckStatementNode(node.FalseBranch, parentScope);
    }
    private static void CheckWhileStatementNode(ParseTree.StatementNode.WhileNode node, Scope<bool> parentScope) {
      CheckExpressionNode(node.Condition, parentScope);
      CheckStatementNode(node.Body, parentScope);
    }
    private static void CheckReturnStatementNode(ParseTree.StatementNode.ReturnNode node, Scope<bool> parentScope) {
      if (node.Value != null) CheckExpressionNode(node.Value, parentScope);
    }
    private static void CheckExpressionStatementNode(ParseTree.StatementNode.ExprStatementNode node, Scope<bool> parentScope) {
      CheckExpressionNode(node.Expression, parentScope);
    }
    #endregion
    // --- Expressions ---
    #region Expressions
    private static void CheckExpressionNode(ParseTree.ExpressionNode node, Scope<bool> parentScope) {
      switch (node) {
        case ParseTree.ExpressionNode.PrefixNode prefixNode:
          CheckPrefixExpressionNode(prefixNode, parentScope);
          break;
        case ParseTree.ExpressionNode.BinopNode binopNode:
          CheckBinopExpressionNode(binopNode, parentScope);
          break;
        case ParseTree.ExpressionNode.CallNode callNode:
          CheckCallExpressionNode(callNode, parentScope);
          break;
        case ParseTree.ExpressionNode.ArrayInitNode arrayInitNode:
          CheckArrayInitExpressionNode(arrayInitNode, parentScope);
          break;
        case ParseTree.ExpressionNode.LocationExprNode locationNode:
          CheckLocationExpressionNode(locationNode, parentScope);
          break;
        case ParseTree.ExpressionNode.LiteralExprNode literalNode:
          CheckLiteralExpressionNode(literalNode, parentScope);
          break;
        // NOTE: This should be impossible unless we forget to update the mapper when adding expressions
        default: throw new Exception($"Unknown expression node type: {node.Kind}");
      }
    }
    private static void CheckPrefixExpressionNode(ParseTree.ExpressionNode.PrefixNode node, Scope<bool> parentScope) {
      CheckExpressionNode(node.Operand, parentScope);
    }
    private static void CheckBinopExpressionNode(ParseTree.ExpressionNode.BinopNode node, Scope<bool> parentScope) {
      CheckExpressionNode(node.Lhs, parentScope);
      CheckExpressionNode(node.Rhs, parentScope);
    }
    private static void CheckCallExpressionNode(ParseTree.ExpressionNode.CallNode node, Scope<bool> parentScope) {
      CheckLocationNode(mutating: false, node.Callee, parentScope);
      foreach (var arg in node.Arguments) CheckExpressionNode(arg, parentScope);
    }
    private static void CheckArrayInitExpressionNode(ParseTree.ExpressionNode.ArrayInitNode node, Scope<bool> parentScope) {
      CheckExpressionNode(node.SizeExpr, parentScope);
    }
    private static void CheckLocationExpressionNode(ParseTree.ExpressionNode.LocationExprNode node, Scope<bool> parentScope) {
      CheckLocationNode(mutating: false, node.Location, parentScope);
    }
    private static void CheckLiteralExpressionNode(ParseTree.ExpressionNode.LiteralExprNode node, Scope<bool> parentScope) {
      CheckLiteralNode(node.Literal, parentScope);
    }
    #endregion
    // --- Literals ---
    #region Literals
    private static void CheckLiteralNode(ParseTree.LiteralNode node, Scope<bool> parentScope) {
      switch (node) {
        // Nothing to check for most literals as they don't touch the scope
        case ParseTree.LiteralNode.IntegerNode:
        case ParseTree.LiteralNode.BooleanNode:
        case ParseTree.LiteralNode.CharacterNode:
        case ParseTree.LiteralNode.StringNode:
          break;
        case ParseTree.LiteralNode.FunctionNode functionNode:
          CheckFunctionLiteralNode(functionNode, parentScope);
          break;
        // NOTE: This should be impossible unless we forget to update the mapper when adding literals
        default: throw new Exception($"Unknown literal node type: {node.Kind}");
      }
    }
    private static void CheckFunctionLiteralNode(ParseTree.LiteralNode.FunctionNode node, Scope<bool> parentScope) {
      // Create a new scope for the function
      var functionScope = new Scope<bool>(parentScope);
      // Add parameters to scope (NOTE: not mutable)
      foreach (var param in node.Parameters)
        functionScope.AddDeclaration(param.Position, param.Name.Name, false);
      // Check the function body
      CheckBlockStatementNode(node.Body, functionScope);
    }
    #endregion
    // --- Locations ---
    #region Locations
    private static void CheckLocationNode(bool mutating, ParseTree.LocationNode node, Scope<bool> parentScope) {
      if (node.IsPrimitive) {
        if (mutating) throw new DeclarationNotMutableException(node.Position, "primitive");
        // We can skip checks on primitive locations (they will be resolved during type checking)
        return;
      }
      switch (node) {
        case ParseTree.LocationNode.ArrayNode arrayNode:
          CheckLocationNode(false, arrayNode.Root, parentScope);
          CheckExpressionNode(arrayNode.IndexExpr, parentScope);
          break;
        case ParseTree.LocationNode.MemberNode memberNode:
          // NOTE: We don't check the member here as we need the module signature (so we check that during type checking)
          CheckLocationNode(false, memberNode.Root, parentScope);
          break;
        case ParseTree.LocationNode.IdentifierNode identifierNode:
          // Ensure the identifier exists in the scope
          if (!parentScope.HasDeclaration(identifierNode.Name)) {
            throw new DeclarationNotDefinedException(identifierNode.Position, identifierNode.Name);
          }
          // Ensure the identifier is mutable if we're trying to mutate it
          if (mutating && parentScope.GetDeclaration(identifierNode.Position, identifierNode.Name) == false) {
            throw new DeclarationNotMutableException(identifierNode.Position, identifierNode.Name);
          }
          break;
        // NOTE: This should be impossible unless we forget to update the mapper when adding locations
        default: throw new Exception($"Unknown location node type: {node.Kind}");
      }
    }
    #endregion
  }
}
