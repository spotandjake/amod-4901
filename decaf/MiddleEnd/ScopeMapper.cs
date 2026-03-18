using System;
using System.Linq;
using Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.ScopeErrors;

namespace Decaf.MiddleEnd {
  // Analyzes the parse tree and attaches scope tables to nodes
  /// <summary>
  /// The ScopeMapper traverses the parse tree starting from a program node and constructs scope tables for each class, method, and block.
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
      var classes = program.Classes.Select(decl => MapClassNode(decl, globalScope)).ToArray();
      return new ProgramNode(program.Position, classes, globalScope);
    }
    private static DeclarationNode.ClassNode MapClassNode(DeclarationNode.ClassNode decl, Scope<bool> parentScope) {
      // Add the class declaration to the global scope
      parentScope.AddDeclaration(decl.Position, decl.Name, false);
      // Ensure superclass exists, if applicable
      if (decl.SuperClassName != null) {
        if (!parentScope.HasDeclaration(decl.SuperClassName)) {
          throw new DeclarationNotDefinedException(decl.Position, decl.SuperClassName);
        }
        // Mark the superClass as used in the scope
        parentScope.SetDeclaration(decl.Position, decl.SuperClassName, true);
      }
      // Create a scope for the class
      var classScope = new Scope<bool>(parentScope);
      // Map the fields 
      var fields = decl.Fields.Select(field => MapVariableDeclarationNode(field, classScope)).ToArray();
      // Map the methods
      var methods = decl.Methods.Select(method => MapMethodDeclarationNode(method, classScope)).ToArray();
      // Construct the new scoped class node
      return new DeclarationNode.ClassNode(
        decl.Position,
        decl.Name,
        decl.SuperClassName,
        fields,
        methods,
        classScope
      );
    }
    private static TypeNode MapTypeNode(TypeNode typeNode, Scope<bool> parentScope) {
      if (typeNode.Type == PrimitiveType.Custom) {
        // For custom types, we need to check if the type exists in the scope
        if (!parentScope.HasDeclaration(typeNode.Content)) {
          throw new DeclarationNotDefinedException(typeNode.Position, typeNode.Content);
        }
        // Mark the type as used in the scope
        parentScope.SetDeclaration(typeNode.Position, typeNode.Content, true);
      }
      return typeNode;
    }
    private static DeclarationNode.VariableNode MapVariableDeclarationNode(DeclarationNode.VariableNode decl, Scope<bool> parentScope) {
      // Map the type of the variable
      var type = MapTypeNode(decl.Type, parentScope);
      // Add variables to scope
      foreach (var bind in decl.Binds) {
        parentScope.AddDeclaration(bind.Position, bind.Name, false);
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
            var lookup = MapLocationNode(assignment.Location, parentScope);
            var expression = MapExpressionNode(assignment.Expression, parentScope);
            return new StatementNode.AssignmentNode(assignment.Position, lookup, expression);
          }
        case StatementNode.ExprNode exprStmt: {
            var content = MapExpressionNode(exprStmt.Content, parentScope);
            return new StatementNode.ExprNode(exprStmt.Position, content);
          }
        case StatementNode.IfNode ifNode: {
            var condition = MapExpressionNode(ifNode.Condition, parentScope);
            var trueBranch = MapBlockNode(ifNode.TrueBranch, parentScope);
            var falseBranch = ifNode.FalseBranch != null ? MapBlockNode(ifNode.FalseBranch, parentScope) : null;
            return new StatementNode.IfNode(ifNode.Position, condition, trueBranch, falseBranch);
          }
        case StatementNode.WhileNode whileNode: {
            var condition = MapExpressionNode(whileNode.Condition, parentScope);
            var body = MapBlockNode(whileNode.Body, parentScope);
            return new StatementNode.WhileNode(whileNode.Position, condition, body);
          }
        case StatementNode.ReturnNode returnNode: {
            var value = returnNode.Value != null ? MapExpressionNode(returnNode.Value, parentScope) : null;
            return new StatementNode.ReturnNode(returnNode.Position, value);
          }
        default:
          throw new Exception($"Unknown statement node type: {node.Kind}");
      }
    }
    private static ExpressionNode MapExpressionNode(ExpressionNode expression, Scope<bool> parentScope) {
      switch (expression) {
        case ExpressionNode.CallNode callNode: {
            // NOTE: Because we don't want to scope primitive calls we just avoid mapping them
            var path = callNode.IsPrimitive ? callNode.Path : MapLocationNode(callNode.Path, parentScope);
            var args = callNode.Arguments.Select(arg => MapExpressionNode(arg, parentScope)).ToArray();
            return new ExpressionNode.CallNode(callNode.Position, callNode.IsPrimitive, path, args);
          }
        case ExpressionNode.BinopNode binopExprNode:
          return new ExpressionNode.BinopNode(
            binopExprNode.Position,
            MapExpressionNode(binopExprNode.Lhs, parentScope),
            binopExprNode.Operator,
            MapExpressionNode(binopExprNode.Rhs, parentScope)
          );
        case ExpressionNode.PrefixNode prefixExprNode:
          return new ExpressionNode.PrefixNode(
            prefixExprNode.Position,
            prefixExprNode.Operator,
            MapExpressionNode(prefixExprNode.Operand, parentScope)
          );
        case ExpressionNode.NewClassNode newClassExprNode:
          return new ExpressionNode.NewClassNode(
            newClassExprNode.Position,
            MapLocationNode(newClassExprNode.Path, parentScope)
          );
        case ExpressionNode.NewArrayNode newArrayExprNode:
          return new ExpressionNode.NewArrayNode(
            newArrayExprNode.Position,
            MapTypeNode(newArrayExprNode.Type, parentScope),
            MapExpressionNode(newArrayExprNode.SizeExpr, parentScope)
          );
        case ExpressionNode.LocationNode locationNode:
          return MapLocationNode(locationNode, parentScope);
        case ExpressionNode.ThisNode thisNode:
          return thisNode; // No mapping is needed for `this` as it contains no children
        case ExpressionNode.IdentifierNode identifierNode:
          // Check if the identifier exists in the scope
          if (!parentScope.HasDeclaration(identifierNode.Name)) {
            throw new DeclarationNotDefinedException(identifierNode.Position, identifierNode.Name);
          }
          parentScope.SetDeclaration(identifierNode.Position, identifierNode.Name, true); // Mark the variable as used
          return identifierNode; // No further mapping is needed for identifiers as they contain no children
        case ExpressionNode.LiteralNode literalNode:
          return literalNode; // No mapping is needed for literals as they contain no scope-relevant children
        default:
          throw new Exception($"Unknown expression node type: {expression.Kind}");
      }
    }
    private static ExpressionNode.LocationNode MapLocationNode(ExpressionNode.LocationNode node, Scope<bool> parentScope) {
      var root = MapExpressionNode(node.Root, parentScope);
      // If the location is an array access, map the index expression
      var indexExpr = node.IndexExpr != null ? MapExpressionNode(node.IndexExpr, parentScope) : null;
      // Return the new mapped location node
      return new ExpressionNode.LocationNode(node.Position, root, node.Path, indexExpr);
    }
  }
}
