using ParseTree;
using System.Collections.Generic;
using System;
using System.Linq;

namespace MiddleEnd {
  // Analyzes the parse tree and attaches scope tables to nodes
  public class ScopeMapper {
    private ScopeMapper() { }
    public static ProgramNode MapProgramNode(ProgramNode program, Scope<bool> globalScope) {
      var classes = new List<ClassNode>();
      foreach (var classDeclaration in program.Classes) {
        // Add the class name to the global scope, fail if we find it already exists
        if (globalScope.HasVariable(classDeclaration.Name)) {
          throw new DuplicateDeclarationException($"Duplicate class name: {classDeclaration.Name}");
        }
        globalScope.AddVariable(classDeclaration.Name, false);
        // Map the class declaration adding new scope information
        var mappedClass = MapClassNode(classDeclaration, globalScope);
        classes.Add(mappedClass);
      }
      return new ProgramNode(program.Position, classes.ToArray(), globalScope);
    }
    private static ClassNode MapClassNode(ClassNode _class, Scope<bool> parentScope) {
      // Ensure superclass exists if it is not defined
      if (_class.SuperClassName != null) {
        if (!parentScope.HasVariable(_class.SuperClassName)) {
          throw new DeclarationNotDefinedException($"Superclass not found: {_class.SuperClassName}, on class: {_class.Name}");
        }
        parentScope.SetVariable(_class.SuperClassName, true); // Mark the superclass as used
      }
      // Create a scope for the class
      var classScope = new Scope<bool>(parentScope);
      // Scope the fields 
      var fields = new List<VariableDeclarationNode>();
      foreach (var field in _class.VariableDeclarations) {
        var scopedField = MapVariableDeclarationNode(field, classScope);
        fields.Add(scopedField);
      }
      // Scope the methods
      var methods = new List<MethodDeclarationNode>();
      foreach (var method in _class.MethodDeclarations) {
        var scopedMethod = MapMethodDeclarationNode(method, classScope);
        methods.Add(scopedMethod);
      }
      // Construct the new scoped class node
      return new ClassNode(_class.Position, _class.Name, _class.SuperClassName, fields.ToArray(), methods.ToArray(), classScope);
    }
    private static VariableDeclarationNode MapVariableDeclarationNode(VariableDeclarationNode variable, Scope<bool> parentScope) {
      // Add variables to scope
      foreach (var bind in variable.VarBinds) {
        parentScope.AddVariable(bind.Name, false);
      }
      return variable;
    }
    private static MethodDeclarationNode MapMethodDeclarationNode(MethodDeclarationNode method, Scope<bool> parentScope) {
      // Add the method to the scope
      parentScope.AddVariable(method.Name, false);
      // Create a new scope for the method
      var methodScope = new Scope<bool>(parentScope);
      // Add parameters to scope
      foreach (var param in method.Parameters) {
        methodScope.AddVariable(param.Name, false);
      }
      // Map the method body
      var body = MapBlockNode(method.Body, methodScope);
      // Return the mapped method declaration node
      return new MethodDeclarationNode(
        method.Position,
        method.ReturnType,
        method.Name,
        method.Parameters,
        body,
        methodScope
      );
    }
    private static BlockNode MapBlockNode(BlockNode block, Scope<bool> parentScope) {
      // Create a new scope for the block
      var blockScope = new Scope<bool>(parentScope);
      // Map the variable bindings
      var decls = new List<VariableDeclarationNode>();
      foreach (var decl in block.VariableDeclarations) {
        var scopedDecl = MapVariableDeclarationNode(decl, blockScope);
        decls.Add(scopedDecl);
      }
      // Map the statements
      var statements = new List<StatementNode>();
      foreach (var stmt in block.Statements) {
        var scopeStmt = MapStatementNode(stmt, blockScope);
        statements.Add(scopeStmt);
      }
      // Return a new mapped block
      return new BlockNode(block.Position, decls.ToArray(), statements.ToArray(), blockScope);
    }
    private static StatementNode MapStatementNode(StatementNode statement, Scope<bool> parentScope) {
      switch (statement) {
        case AssignmentNode assignment: {
            var lookup = MapLocationNode(assignment.Location, parentScope);
            var expression = MapExpressionNode(assignment.Expression, parentScope);
            return new AssignmentNode(assignment.Position, lookup, expression);
          }
        case ExpressionStatementNode exprStmt: {
            var content = MapExpressionNode(exprStmt.Content, parentScope);
            return new ExpressionStatementNode(exprStmt.Position, content);
          }
        case IfNode ifNode: {
            var condition = MapExpressionNode(ifNode.Condition, parentScope);
            var trueBranch = MapBlockNode(ifNode.TrueBranch, parentScope);
            var falseBranch = ifNode.FalseBranch != null ? MapBlockNode(ifNode.FalseBranch, parentScope) : null;
            return new IfNode(ifNode.Position, condition, trueBranch, falseBranch);
          }
        case WhileNode whileNode: {
            var condition = MapExpressionNode(whileNode.Condition, parentScope);
            var body = MapBlockNode(whileNode.Body, parentScope);
            return new WhileNode(whileNode.Position, condition, body);
          }
        case ReturnNode returnNode: {
            var value = returnNode.Value != null ? MapExpressionNode(returnNode.Value, parentScope) : null;
            return new ReturnNode(returnNode.Position, value);
          }
        default:
          throw new Exception($"Unknown statement node type: {statement.Kind}");
      }
    }
    private static ExpressionNode MapExpressionNode(ExpressionNode expression, Scope<bool> parentScope) {
      switch (expression) {
        case CallNode callNode: {
            var methodPath = MapLocationNode(callNode.MethodPath, parentScope);
            var args = callNode.Arguments.Select(arg => MapExpressionNode(arg, parentScope)).ToArray();
            return new CallNode(callNode.Position, methodPath, args);
          }
        case PrimitiveCallNode primCallNode: {
            // Map the arguments
            var primArgs = new List<PrimitiveCallNode.Argument>();
            foreach (var arg in primCallNode.Arguments) {
              switch (arg) {
                case PrimitiveCallNode.Argument.Expression exprArg:
                  var mappedExpr = MapExpressionNode(exprArg.Content, parentScope);
                  primArgs.Add(new PrimitiveCallNode.Argument.Expression(mappedExpr));
                  break;
                default:
                  primArgs.Add(arg); // For non-expression arguments, we assume they don't need mapping (e.g., operator names)
                  break;
              }
            }
            return new PrimitiveCallNode(primCallNode.Position, primCallNode.PrimitiveId, primArgs.ToArray());
          }
        case SimpleExpressionNode simpleExprNode: {
            // NOTE: A simple expression node just wraps an expression
            return new SimpleExpressionNode(simpleExprNode.Position, MapExpressionNode(simpleExprNode.Content, parentScope));
          }
        case BinopExpressionNode binopExprNode:
          return new BinopExpressionNode(
            binopExprNode.Position,
            MapExpressionNode(binopExprNode.Lhs, parentScope),
            binopExprNode.Operator,
            MapExpressionNode(binopExprNode.Rhs, parentScope)
          );
        case PrefixExpressionNode prefixExprNode:
          return new PrefixExpressionNode(
            prefixExprNode.Position,
            prefixExprNode.Operator,
            MapExpressionNode(prefixExprNode.Operand, parentScope)
          );
        case LocationNode locationNode:
          return MapLocationNode(locationNode, parentScope);
        case ThisNode thisNode:
          return thisNode; // No mapping is needed for `this` as it contains no children
        case LiteralNode literalNode:
          return literalNode; // No mapping is needed for literals as they contain no scope-relevant children
        default:
          throw new Exception($"Unknown expression node type: {expression.Kind}");
      }
    }
    private static LocationNode MapLocationNode(LocationNode location, Scope<bool> parentScope) {
      // Check if the base variable exists in the scope
      if (!parentScope.HasVariable(location.Root)) {
        throw new DeclarationNotDefinedException($"Declaration not found: {location.Root}");
      }
      parentScope.SetVariable(location.Root, true); // Mark the variable as used
      // NOTE: We don't actually check field accesses here as they don't change scope information, we handle that in type checking.
      // If the location is an array access, map the index expression
      var indexExpr = location.IndexExpr != null ? MapExpressionNode(location.IndexExpr, parentScope) : null;
      // Return the new mapped location node
      return new LocationNode(location.Position, location.Root, location.Path, indexExpr);
    }
  }
}
