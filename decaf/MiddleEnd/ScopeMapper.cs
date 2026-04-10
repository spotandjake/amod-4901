using System;
using System.Linq;

using Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.ScopeErrors;

namespace Decaf.MiddleEnd {
  // Analyzes the parse tree and attaches scope tables to nodes
  /// <summary>
  /// The ScopeMapper traverses the parse tree starting from a program node and constructs scope tables for each module, method, and block.
  /// </summary>
  public class ScopeMapper {
    private ScopeMapper() { }
    /// <summary>
    /// Maps a program node to a new program node with the scoping information attached.
    /// </summary>
    /// <param name="program">The program to map</param>
    /// <param name="globalScope">The global scope of the program.</param>
    /// <returns>A new program node with scoping information attached.</returns>
    /// <exception cref="DuplicateDeclarationException">When a declaration is found to be a duplicate.</exception>
    /// <exception cref="DeclarationNotDefinedException">When a declaration is being used but not defined.</exception>
    public static ProgramNode MapProgramNode(ProgramNode program, Scope<bool> globalScope) {
      var modules = program.Modules.Select(decl => MapModuleNode(decl, globalScope)).ToArray();
      return new ProgramNode(program.Position, modules, globalScope);
    }
    private static DeclarationNode.ModuleNode MapModuleNode(DeclarationNode.ModuleNode decl, Scope<bool> parentScope) {
      // Add the module declaration to the global scope
      parentScope.AddDeclaration(decl.Position, decl.Name, false);
      // Create a scope for the module
      var moduleScope = new Scope<bool>(parentScope);
      // Map the fields 
      var fields = decl.Fields.Select(field => MapVariableDeclarationNode(field, moduleScope)).ToArray();
      // Map the methods
      var methods = decl.Methods.Select(method => MapMethodDeclarationNode(method, moduleScope)).ToArray();
      // Construct the new scoped module node
      return new DeclarationNode.ModuleNode(
        decl.Position,
        decl.Name,
        fields,
        methods,
        moduleScope
      );
    }
    private static TypeNode MapTypeNode(TypeNode typeNode, Scope<bool> parentScope) {
      // if (typeNode.Type == PrimitiveType.Custom) {
      //   // For custom types, we need to check if the type exists in the scope
      //   if (!parentScope.HasDeclaration(typeNode.Content)) {
      //     throw new DeclarationNotDefinedException(typeNode.Position, typeNode.Content);
      //   }
      // }
      return typeNode;
    }
    private static DeclarationNode.VariableNode MapVariableDeclarationNode(DeclarationNode.VariableNode decl, Scope<bool> parentScope) {
      // Map the type of the variable
      var type = MapTypeNode(decl.Type, parentScope);
      // Add variables to scope
      foreach (var bind in decl.Binds) {
        parentScope.AddDeclaration(bind.Position, bind.Name, true);
      }
      return new DeclarationNode.VariableNode(decl.Position, type, decl.Binds);
    }
    private static DeclarationNode.MethodNode MapMethodDeclarationNode(DeclarationNode.MethodNode decl, Scope<bool> parentScope) {
      // Add the method to the parent scope
      parentScope.AddDeclaration(decl.Position, decl.Name, false);
      // Create a new scope for the method
      var scope = new Scope<bool>(parentScope);
      // Add parameters to scope
      var parameters = decl.Parameters.Select(param => {
        // Map the parameter type
        var type = MapTypeNode(param.ParamType, parentScope);
        // Add the parameter to the method scope
        scope.AddDeclaration(param.Position, param.Name, false);
        // Return a new parameter with the mapped type
        return new DeclarationNode.MethodNode.ParameterNode(param.Position, type, param.Name, param.IsArray);
      }).ToArray();
      // Map the method return type
      var returnType = MapTypeNode(decl.ReturnType, parentScope);
      // Map the method body
      var body = MapBlockNode(decl.Body, scope);
      // Return the mapped method declaration node
      return new DeclarationNode.MethodNode(
        decl.Position,
        returnType,
        decl.Name,
        parameters,
        body,
        scope
      );
    }
    private static BlockNode MapBlockNode(BlockNode node, Scope<bool> parentScope) {
      // Create a new scope for the block
      var scope = new Scope<bool>(parentScope);
      // Map the declarations
      var decls = node.Declarations.Select(decl => MapVariableDeclarationNode(decl, scope)).ToArray();
      // Map the statements
      var statements = node.Statements.Select(stmt => MapStatementNode(stmt, scope)).ToArray();
      // Return a new mapped block
      return new BlockNode(node.Position, decls, statements, scope);
    }
    private static StatementNode MapStatementNode(StatementNode node, Scope<bool> parentScope) {
      switch (node) {
        case StatementNode.AssignmentNode assignment: {
            var lookup = MapLocationNode(assignment.Location, true, parentScope);
            var expression = MapExpressionNode(assignment.Expression, false, parentScope);
            return new StatementNode.AssignmentNode(assignment.Position, lookup, expression);
          }
        case StatementNode.ExprNode exprStmt: {
            var content = MapExpressionNode(exprStmt.Content, false, parentScope);
            return new StatementNode.ExprNode(exprStmt.Position, content);
          }
        case StatementNode.IfNode ifNode: {
            var condition = MapExpressionNode(ifNode.Condition, false, parentScope);
            var trueBranch = MapBlockNode(ifNode.TrueBranch, parentScope);
            var falseBranch = ifNode.FalseBranch != null ? MapBlockNode(ifNode.FalseBranch, parentScope) : null;
            return new StatementNode.IfNode(ifNode.Position, condition, trueBranch, falseBranch);
          }
        case StatementNode.WhileNode whileNode: {
            var condition = MapExpressionNode(whileNode.Condition, false, parentScope);
            var body = MapBlockNode(whileNode.Body, parentScope);
            return new StatementNode.WhileNode(whileNode.Position, condition, body);
          }
        case StatementNode.ContinueNode continueNode:
          return continueNode; // Nothing to map
        case StatementNode.BreakNode breakNode:
          return breakNode; // Nothing to map
        case StatementNode.ReturnNode returnNode: {
            var value = returnNode.Value != null ? MapExpressionNode(returnNode.Value, false, parentScope) : null;
            return new StatementNode.ReturnNode(returnNode.Position, value);
          }
        default:
          throw new Exception($"Unknown statement node type: {node.Kind}");
      }
    }
    private static ExpressionNode MapExpressionNode(ExpressionNode expression, bool mutating, Scope<bool> parentScope) {
      switch (expression) {
        case ExpressionNode.CallNode callNode: {
            // NOTE: Because we don't want to scope primitive calls we just avoid mapping them
            var path = callNode.IsPrimitive ? callNode.Path : MapLocationNode(callNode.Path, mutating, parentScope);
            var args = callNode.Arguments.Select(arg => MapExpressionNode(arg, false, parentScope)).ToArray();
            return new ExpressionNode.CallNode(callNode.Position, callNode.IsPrimitive, path, args);
          }
        case ExpressionNode.BinopNode binopExprNode:
          return new ExpressionNode.BinopNode(
            binopExprNode.Position,
            MapExpressionNode(binopExprNode.Lhs, false, parentScope),
            binopExprNode.Operator,
            MapExpressionNode(binopExprNode.Rhs, false, parentScope)
          );
        case ExpressionNode.PrefixNode prefixExprNode:
          return new ExpressionNode.PrefixNode(
            prefixExprNode.Position,
            prefixExprNode.Operator,
            MapExpressionNode(prefixExprNode.Operand, false, parentScope)
          );
        case ExpressionNode.NewArrayNode newArrayExprNode:
          return new ExpressionNode.NewArrayNode(
            newArrayExprNode.Position,
            MapTypeNode(newArrayExprNode.Type, parentScope),
            MapExpressionNode(newArrayExprNode.SizeExpr, false, parentScope)
          );
        case ExpressionNode.LocationAccessNode locationNode:
          return new ExpressionNode.LocationAccessNode(
            locationNode.Position,
            MapLocationNode(locationNode.Content, mutating, parentScope)
          );
        case ExpressionNode.LiteralNode literalNode:
          return literalNode; // No mapping is needed for literals as they contain no scope-relevant children
        default:
          throw new Exception($"Unknown expression node type: {expression.Kind}");
      }
    }
    private static LocationNode MapLocationNode(
      LocationNode node,
      bool mutating,
      Scope<bool> parentScope
    ) {
      switch (node) {
        case LocationNode.ThisNode thisNode:
          return thisNode; // No mapping needed for `this` as it contains no children
        case LocationNode.IdentifierAccessNode identifierNode:
          // Check if the identifier exists in the scope
          if (!parentScope.HasDeclaration(identifierNode.Name)) {
            throw new DeclarationNotDefinedException(identifierNode.Position, identifierNode.Name);
          }
          if (parentScope.GetDeclaration(identifierNode.Position, identifierNode.Name) == false && mutating) {
            throw new DeclarationNotMutableException(identifierNode.Position, identifierNode.Name);
          }
          return identifierNode; // No further mapping is needed for identifiers as they contain no children
        case LocationNode.MemberAccessNode fieldAccessNode:
          return new LocationNode.MemberAccessNode(
            fieldAccessNode.Position,
            MapLocationNode(fieldAccessNode.Root, false, parentScope),
            fieldAccessNode.Member
          );
        case LocationNode.ArrayAccessNode arrayAccessNode:
          return new LocationNode.ArrayAccessNode(
            arrayAccessNode.Position,
            MapLocationNode(arrayAccessNode.Root, false, parentScope),
            MapExpressionNode(arrayAccessNode.IndexExpr, false, parentScope)
          );
        default: throw new Exception($"Unknown location node type: {node.Kind}");
      }
    }
  }
}
