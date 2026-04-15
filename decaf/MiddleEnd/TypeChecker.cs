using System;
using System.Collections.Generic;

using ParseTree = Decaf.IR.ParseTree;
using TypedTree = Decaf.IR.TypedTree;
using Signature = Decaf.IR.Signature;
using Decaf.Utils;
using Decaf.Utils.Errors.TypeCheckingErrors;

/// <summary>
/// This the plumbing for the type checker, the core type checking logic itself is implemented in `TypeCheckerCore`,
/// this file is responsible for traversing the tree, mapping the signatures and scopes. 
/// And finally emitting the typed tree with the correct signature on each node.
/// </summary>
namespace Decaf.MiddleEnd.TypeChecker {
  // The type checker itself
  public static class TypeChecker {
    // The context used for type checking
#nullable enable
    private readonly record struct Context(
      string? CurrentModule,
      Signature.Signature.MethodSig? CurrentMethod,
      Scope<Signature.Signature> CurrentScope
    ) {
      public string? CurrentModule { get; } = CurrentModule;
      public Signature.Signature.MethodSig? CurrentMethod { get; } = CurrentMethod;
      public Scope<Signature.Signature> CurrentScope { get; } = CurrentScope;
    }
#nullable disable

    // --- Code Units ---
    #region CodeUnits
    public static TypedTree.ProgramNode TypeProgramNode(ParseTree.ProgramNode node) {
      // Initialize a new context for the program
      var ctx = new Context(null, null, new Scope<Signature.Signature>(null));
      // Map the modules
      var modules = new List<TypedTree.ModuleNode>();
      foreach (var module in node.Modules) {
        // Type check the module
        var typedModule = TypeModuleNode(ctx, module);
        modules.Add(typedModule);
      }
      // Map the program node itself
      return new TypedTree.ProgramNode(node.Position, modules.ToArray(), ctx.CurrentScope);
    }
    private static TypedTree.ModuleNode TypeModuleNode(Context parentCtx, ParseTree.ModuleNode node) {
      // NOTE: Mapping modules is a little interesting as modules are allowed to be self recursive.
      //       This means that we need to know the signature of the module before we can type check the body of the module.
      //       There are a few traditional ways to handle this depending on the type system being used the simplest of which is
      //       to delay all of the checks in the module body until we have the full signature of the module.
      //       However we take a slightly different approach that simplifies the type checker, we take advantage of
      //       the fact that while modules are recursive `Mod.x` can't access `Mod.y` if `y` is declared after `x`.
      //       This means that we can initially register the module with an empty signature, and as we map the statements
      //       we can update the module signature and the global scope as we go.

      var name = node.Name.Name;
      // Create a base signature and register the module in the parent scope
      var moduleSignature = new Signature.Signature.ModuleSig(node.Position, []);
      parentCtx.CurrentScope.AddDeclaration(node.Position, name, moduleSignature);
      // Create a new context for the module
      var ctx = new Context(name, null, new Scope<Signature.Signature>(parentCtx.CurrentScope));
      // Map the statements of the module
      var statements = new List<TypedTree.StatementNode>();
      foreach (var stmt in node.Body.Statements) {
        bool HasReturn = false; // NOTE: You can't return from a module but this is needed for if statements in the module body
        var typedStmt = TypeStatementNode(ctx, stmt, ref HasReturn);
        // NOTE: We could sanity check the HasReturn but we check it elsewhere so it doesn't make sense to check it here as well.
        statements.Add(typedStmt);
        // If the statement is a declaration statement we need to update the module signature
        if (typedStmt is TypedTree.StatementNode.VariableDeclNode variableDecl) {
          // NOTE: It would probably make sense to add a statement in the future to mark binds as public
          foreach (var bind in variableDecl.Binds) {
            // Update the signature
            moduleSignature.Members[bind.Name] = bind.Signature;
            // Update the scope
            parentCtx.CurrentScope.SetDeclaration(variableDecl.Position, name, moduleSignature);
          }
        }
      }
      var body = new TypedTree.StatementNode.BlockNode(node.Body.Position, statements.ToArray(), ctx.CurrentScope);
      // Map the module node itself
      return new TypedTree.ModuleNode(node.Position, name, body, ctx.CurrentScope, moduleSignature);
    }
    #endregion
    // --- Statements ---
    #region Statements
    private static TypedTree.StatementNode TypeStatementNode(
      Context parentCtx,
      ParseTree.StatementNode node,
      ref bool HasReturn
    ) {
      return node switch {
        ParseTree.StatementNode.BlockNode blockNode => TypeBlockStatementNode(parentCtx, blockNode, ref HasReturn),
        ParseTree.StatementNode.VariableDeclNode variableDeclNode => TypeVariableDeclarationStatementNode(parentCtx, variableDeclNode),
        ParseTree.StatementNode.AssignmentNode assignmentNode => TypeAssignmentStatementNode(parentCtx, assignmentNode),
        ParseTree.StatementNode.IfNode ifNode => TypeIfStatementNode(parentCtx, ifNode, ref HasReturn),
        ParseTree.StatementNode.WhileNode whileNode => TypeWhileStatementNode(parentCtx, whileNode),
        ParseTree.StatementNode.ReturnNode returnNode => TypeReturnStatementNode(parentCtx, returnNode, ref HasReturn),
        ParseTree.StatementNode.ContinueNode continueNode => TypeContinueStatementNode(parentCtx, continueNode),
        ParseTree.StatementNode.BreakNode breakNode => TypeBreakStatementNode(parentCtx, breakNode),
        ParseTree.StatementNode.ExprStatementNode exprNode => TypeExpressionStatementNode(parentCtx, exprNode),
        // NOTE: This should be impossible unless we forget to update the checker when adding statements
        _ => throw new Exception($"Unknown statement node type: {node.Kind}"),
      };
    }
    private static TypedTree.StatementNode.BlockNode TypeBlockStatementNode(
      Context parentCtx,
      ParseTree.StatementNode.BlockNode node,
      ref bool HasReturn
    ) {
      // Create a new context for the block
      var ctx = new Context(
        parentCtx.CurrentModule,
        parentCtx.CurrentMethod,
        new Scope<Signature.Signature>(parentCtx.CurrentScope)
      );
      // Map the statements
      var statements = new List<TypedTree.StatementNode>();
      foreach (var stmt in node.Statements) {
        var typedStmt = TypeStatementNode(ctx, stmt, ref HasReturn);
        statements.Add(typedStmt);
      }
      // Map the block node itself, note that the scope of the block is the current scope of the context
      return new TypedTree.StatementNode.BlockNode(node.Position, statements.ToArray(), ctx.CurrentScope);
    }
    private static TypedTree.StatementNode.VariableDeclNode TypeVariableDeclarationStatementNode(
      Context parentCtx, ParseTree.StatementNode.VariableDeclNode node
    ) {
      // Map the binds of the decl
      var binds = new List<TypedTree.StatementNode.VariableDeclNode.BindNode>();
      foreach (var bind in node.Binds) {
        // Extract the signature of the bind
        var signature = bind.Type switch {
          // We are allowed to infer the type of a function literal
          null when bind.InitExpr is ParseTree.ExpressionNode.LiteralExprNode {
            Literal: ParseTree.LiteralNode.FunctionNode functionLiteral
          } => ExtractFunctionSignatureFromFunctionLiteral(parentCtx, functionLiteral),
          // Void isn't a valid bind
          ParseTree.TypeNode.SimpleNode { Type: Signature.PrimitiveType.Void } =>
            throw new InvalidVoidBind(bind.Position),
          // In any other case we don't allow inference
          null => throw new ExpectedBindToHaveAType(bind.Position, bind.Name.Name),
          // Map the type of the bind if it exists
          _ => TypeTypeNode(parentCtx, bind.Type)
        };
        // Add the binding to the scope
        // NOTE: It is important that we do this before mapping the init expr so we can support recursive definitions
        parentCtx.CurrentScope.AddDeclaration(bind.Position, bind.Name.Name, signature);
        // Map the init expression
        var initExpr = TypeExpressionNode(parentCtx, bind.InitExpr);
        // Validate the init expression matches the signature of the bind
        TypeCheckerCore.CheckSignature(expected: signature, received: initExpr.ExpressionType);
        // Map the bind itself
        var typedBind = new TypedTree.StatementNode.VariableDeclNode.BindNode(bind.Position, bind.Name.Name, initExpr, signature);
        binds.Add(typedBind);
      }
      // Map the node itself
      return new TypedTree.StatementNode.VariableDeclNode(node.Position, binds.ToArray());
    }
    private static TypedTree.StatementNode.AssignmentNode TypeAssignmentStatementNode(
      Context parentCtx, ParseTree.StatementNode.AssignmentNode node
    ) {
      // Map the location
      var location = TypeLocationNode(parentCtx, node.Location);
      // Map the expression
      var expression = TypeExpressionNode(parentCtx, node.Expression);
      // Ensure the expression has the type of the location
      TypeCheckerCore.CheckSignature(expected: location.LocationType, received: expression.ExpressionType);
      // Map the node itself
      return new TypedTree.StatementNode.AssignmentNode(node.Position, location, expression);
    }
    private static TypedTree.StatementNode.IfNode TypeIfStatementNode(
      Context parentCtx,
      ParseTree.StatementNode.IfNode node,
      ref bool HasReturn
    ) {
      // Map the condition
      var condition = TypeExpressionNode(parentCtx, node.Condition);
      // Validate the condition is a boolean
      TypeCheckerCore.CheckSignature(
        expected: new Signature.Signature.PrimitiveSig(node.Condition.Position, Signature.PrimitiveType.Boolean),
        received: condition.ExpressionType
      );
      // Map the true branch & false branch
      bool hasReturnTrueBranch = false;
      bool hasReturnFalseBranch = false;
      var trueBranch = TypeStatementNode(parentCtx, node.TrueBranch, ref hasReturnTrueBranch);
      var falseBranch = node.FalseBranch != null ? TypeStatementNode(parentCtx, node.FalseBranch, ref hasReturnFalseBranch) : null;
      // Properly set the HasReturn value for this if statement up the tree.
      if (!HasReturn) HasReturn = hasReturnTrueBranch && hasReturnFalseBranch;
      // Map the node itself
      return new TypedTree.StatementNode.IfNode(node.Position, condition, trueBranch, falseBranch);
    }
    private static TypedTree.StatementNode.WhileNode TypeWhileStatementNode(
      Context parentCtx, ParseTree.StatementNode.WhileNode node
    ) {
      // Map the condition
      var condition = TypeExpressionNode(parentCtx, node.Condition);
      // Validate the condition is a boolean
      TypeCheckerCore.CheckSignature(
        expected: new Signature.Signature.PrimitiveSig(node.Condition.Position, Signature.PrimitiveType.Boolean),
        received: condition.ExpressionType
      );
      // Map the body
      bool hasReturn = false; // NOTE: You can't return from a loop 
      var body = TypeStatementNode(parentCtx, node.Body, ref hasReturn);
      // Map the while node itself
      return new TypedTree.StatementNode.WhileNode(node.Position, condition, body);
    }
    private static TypedTree.StatementNode.ReturnNode TypeReturnStatementNode(
      Context parentCtx,
      ParseTree.StatementNode.ReturnNode node,
      ref bool HasReturn
    ) {
      HasReturn = true;
      // NOTE: Ensure that we are actually in a method, this should never be hit
      //       as we check this semantically however it makes sense to sanity check this here as well.
      if (parentCtx.CurrentMethod == null) throw new ReturnUseOutsideOfMethod(node.Position);
      // Extract the return value and expected return type from the context
      var expectedReturnType = parentCtx.CurrentMethod.ReturnType;
      // Map the return value if it exists
      var returnValue = node.Value switch {
        null => null,
        _ => TypeExpressionNode(parentCtx, node.Value)
      };
      var returnSignature = returnValue switch {
        null => new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Void),
        _ => returnValue.ExpressionType
      };
      // Validate the return type matches the expected return type
      TypeCheckerCore.CheckSignature(expected: expectedReturnType, received: returnSignature);
      // Map the node itself
      return new TypedTree.StatementNode.ReturnNode(node.Position, returnValue);
    }
    private static TypedTree.StatementNode.ContinueNode TypeContinueStatementNode(
      Context _, ParseTree.StatementNode.ContinueNode node
    ) {
      // NOTE: This is a 1 to 1 mapping as there are no rules we need to check
      return new TypedTree.StatementNode.ContinueNode(node.Position);
    }
    private static TypedTree.StatementNode.BreakNode TypeBreakStatementNode(
      Context _, ParseTree.StatementNode.BreakNode node
    ) {
      // NOTE: This is a 1 to 1 mapping as there are no rules we need to check
      return new TypedTree.StatementNode.BreakNode(node.Position);
    }
    private static TypedTree.StatementNode.ExprStatementNode TypeExpressionStatementNode(
      Context parentCtx, ParseTree.StatementNode.ExprStatementNode node
    ) {
      // NOTE: In the future it may make sense to restrict expression statements to only allow expressions that result in void,
      //       but for now we allow any expression as a statement and just ignore the result.

      // Map the expression itself
      var expr = TypeExpressionNode(parentCtx, node.Expression);
      // Map node itself
      return new TypedTree.StatementNode.ExprStatementNode(node.Position, expr);
    }
    #endregion
    // --- Expressions ---
    #region Expressions
    private static TypedTree.ExpressionNode TypeExpressionNode(Context parentCtx, ParseTree.ExpressionNode node) {
      return node switch {
        ParseTree.ExpressionNode.PrefixNode prefixNode => TypePrefixExpressionNode(parentCtx, prefixNode),
        ParseTree.ExpressionNode.BinopNode binopNode => TypeBinopExpressionNode(parentCtx, binopNode),
        ParseTree.ExpressionNode.CallNode callNode => TypeCallExpressionNode(parentCtx, callNode),
        ParseTree.ExpressionNode.ArrayInitNode arrayInitNode => TypeArrayInitExpressionNode(parentCtx, arrayInitNode),
        ParseTree.ExpressionNode.LocationExprNode locationNode => TypeLocationExpressionNode(parentCtx, locationNode),
        ParseTree.ExpressionNode.LiteralExprNode literalNode => TypeLiteralExpressionNode(parentCtx, literalNode),
        // NOTE: This should be impossible unless we forget to update the checker when adding expressions
        _ => throw new Exception($"Unknown expression node type: {node.Kind}"),
      };
    }
    private static TypedTree.ExpressionNode.PrefixNode TypePrefixExpressionNode(
      Context parentCtx, ParseTree.ExpressionNode.PrefixNode node
    ) {
      // Construct the expected signature of the operation based on the operator
      // NOTE: It may make sense to move this logic into it's own place if this grows
      var expectedSignature = node.Operator switch {
        // (bool) => bool
        IR.Operators.PrefixOperator.Not =>
          new Signature.Signature.MethodSig(
            node.Position,
            [new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean)],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean)
          ),
        // (int) => int
        IR.Operators.PrefixOperator.BitwiseNot =>
          new Signature.Signature.MethodSig(
            node.Position,
            [new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
          ),
        // Unknown
        _ => throw new Exception($"Unknown prefix operator {node.Operator}")
      };
      // Map the operand
      var operand = TypeExpressionNode(parentCtx, node.Operand);
      // Construct the actual signature of the operator application
      // NOTE: We treat prefix operators as functions that take a single argument (This allows us to reuse the logic)
      var actualSignature = new Signature.Signature.MethodSig(
        node.Position,
        [operand.ExpressionType],
        expectedSignature.ReturnType
      );
      // Check the signature matches the expected signature
      TypeCheckerCore.CheckSignature(expected: expectedSignature, received: actualSignature);
      // Map the node itself
      return new TypedTree.ExpressionNode.PrefixNode(node.Position, node.Operator, operand, expectedSignature.ReturnType);
    }
    private static TypedTree.ExpressionNode.BinopNode TypeBinopExpressionNode(
      Context parentCtx, ParseTree.ExpressionNode.BinopNode node
    ) {
      // Map the left hand side
      var lhs = TypeExpressionNode(parentCtx, node.Lhs);
      // Map the right hand side
      var rhs = TypeExpressionNode(parentCtx, node.Rhs);
      // Get the expected signature of the operation based on the operator
      var expectedSignature = node.Operator switch {
        // Arithmetic operators: (int, int) => int
        IR.Operators.BinaryOperator.Add or
        IR.Operators.BinaryOperator.Minus or
        IR.Operators.BinaryOperator.Multiply or
        IR.Operators.BinaryOperator.Divide =>
          new Signature.Signature.MethodSig(
            node.Position,
            [
              new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int),
              new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
            ],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
        ),
        // Relational operators: (int, int) => boolean
        IR.Operators.BinaryOperator.LessThan or
        IR.Operators.BinaryOperator.GreaterThan or
        IR.Operators.BinaryOperator.LessThanOrEqual or
        IR.Operators.BinaryOperator.GreaterThanOrEqual =>
          new Signature.Signature.MethodSig(
            node.Position,
            [
              new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int),
              new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
            ],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean)
          ),
        // Equality operators: (a, a) => boolean
        IR.Operators.BinaryOperator.Equal or
        IR.Operators.BinaryOperator.NotEqual =>
          new Signature.Signature.MethodSig(
            node.Position,
            [lhs.ExpressionType, lhs.ExpressionType],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean)
          ),
        // Conditional operators: (boolean, boolean) => boolean
        IR.Operators.BinaryOperator.And or
        IR.Operators.BinaryOperator.Or =>
          new Signature.Signature.MethodSig(
            node.Position,
            [new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean),
             new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean)],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Boolean)
          ),
        // Bitwise operators: (int, int) => int
        IR.Operators.BinaryOperator.BitwiseAnd or
        IR.Operators.BinaryOperator.BitwiseOr or
        IR.Operators.BinaryOperator.BitwiseLeftShift or
        IR.Operators.BinaryOperator.BitwiseRightShift =>
          new Signature.Signature.MethodSig(
            node.Position,
            [
              new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int),
                new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
            ],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
          ),
        _ => throw new Exception($"Unknown binary operator {node.Operator}"),
      };
      // Construct the actual signature of the operator application
      var actualSignature = new Signature.Signature.MethodSig(
        node.Position,
        [lhs.ExpressionType, rhs.ExpressionType],
        expectedSignature.ReturnType
      );
      // Check the signature matches the expected signature
      TypeCheckerCore.CheckSignature(expected: expectedSignature, received: actualSignature);
      // Map the node itself
      return new TypedTree.ExpressionNode.BinopNode(node.Position, lhs, node.Operator, rhs, expectedSignature.ReturnType);
    }
    private static TypedTree.ExpressionNode TypeCallExpressionNode(
      Context parentCtx, ParseTree.ExpressionNode.CallNode node
    ) {
      // Map the arguments
      var args = new List<TypedTree.ExpressionNode>();
      var argTypes = new List<Signature.Signature>();
      foreach (var arg in node.Arguments) {
        var typedArg = TypeExpressionNode(parentCtx, arg);
        args.Add(typedArg);
        argTypes.Add(typedArg.ExpressionType);
      }
      // Primitive are handles slightly differently
      if (node.Callee.IsPrimitive) {
        // Map the callee
        var (primSig, callee) = PrimitiveTypes.ResolvePrimitive(node.Position, node.Callee);
        var expectedSignature = primSig switch {
          Signature.Signature.MethodSig methodSig => methodSig,
          _ => throw new CallOnNonMethod(node.Position)
        };
        // Construct the signature
        var signature = new Signature.Signature.MethodSig(
          node.Position,
          argTypes.ToArray(),
          expectedSignature.ReturnType
        );
        // Check the signature matches the expected signature
        TypeCheckerCore.CheckSignature(expected: expectedSignature, received: signature);
        // Map the node itself
        return new TypedTree.ExpressionNode.PrimCallNode(node.Position, callee, args.ToArray(), expectedSignature.ReturnType);
      }
      else {
        // Map the callee
        var callee = TypeLocationNode(parentCtx, node.Callee);
        var expectedSignature = callee.LocationType switch {
          Signature.Signature.MethodSig methodSig => methodSig,
          _ => throw new CallOnNonMethod(node.Position)
        };
        // Construct the signature
        var signature = new Signature.Signature.MethodSig(
          node.Position,
          argTypes.ToArray(),
          expectedSignature.ReturnType
        );
        // Check the signature matches the expected signature
        TypeCheckerCore.CheckSignature(expected: expectedSignature, received: signature);
        // Map the node itself
        return new TypedTree.ExpressionNode.CallNode(node.Position, callee, args.ToArray(), expectedSignature.ReturnType);
      }
    }
    private static TypedTree.ExpressionNode.ArrayInitNode TypeArrayInitExpressionNode(
      Context parentCtx, ParseTree.ExpressionNode.ArrayInitNode node
    ) {
      // Map the size expression
      var sizeExpr = TypeExpressionNode(parentCtx, node.SizeExpr);
      // Validate the size expression is an integer
      TypeCheckerCore.CheckSignature(
        expected: new Signature.Signature.PrimitiveSig(node.SizeExpr.Position, Signature.PrimitiveType.Int),
        received: sizeExpr.ExpressionType
      );
      // Map the type node
      var typeNode = TypeTypeNode(parentCtx, node.Type);
      // Validate that we support arrays of this type
      if (typeNode is not Signature.Signature.PrimitiveSig {
        Type:
          Signature.PrimitiveType.Int or
          Signature.PrimitiveType.Boolean or
          Signature.PrimitiveType.Character
        // NOTE: We don't support arrays of strings or user defined types yet but we will in the future
        // NOTE: This check is a little fragile if we add more types we should probably work on this
      }) {
        throw new InvalidArrayType(node.Position, typeNode.ToString());
      }
      // Build the signature of the new array node
      var signature = new Signature.Signature.ArraySig(node.Position, typeNode);
      // Map the node itself
      return new TypedTree.ExpressionNode.ArrayInitNode(node.Position, sizeExpr, signature);
    }
    private static TypedTree.ExpressionNode.LocationExprNode TypeLocationExpressionNode(
      Context parentCtx, ParseTree.ExpressionNode.LocationExprNode node
    ) {
      // Map the location
      var location = TypeLocationNode(parentCtx, node.Location);
      // Map the node itself
      return new TypedTree.ExpressionNode.LocationExprNode(node.Position, location, location.LocationType);
    }
    private static TypedTree.ExpressionNode.LiteralExprNode TypeLiteralExpressionNode(
      Context parentCtx, ParseTree.ExpressionNode.LiteralExprNode node
    ) {
      // Map the literal
      var literal = TypeLiteralNode(parentCtx, node.Literal);
      // Map the node itself
      return new TypedTree.ExpressionNode.LiteralExprNode(node.Position, literal, literal.LiteralType);
    }
    #endregion
    // --- Literals ---
    #region Literals
    private static TypedTree.LiteralNode TypeLiteralNode(Context parentCtx, ParseTree.LiteralNode node) {
      return node switch {
        // Most literals are a simple 1 to 1 mapping
        ParseTree.LiteralNode.IntegerNode intNode =>
          new TypedTree.LiteralNode.IntegerNode(
            intNode.Position, intNode.Value, new Signature.Signature.PrimitiveSig(intNode.Position, Signature.PrimitiveType.Int)
          ),
        ParseTree.LiteralNode.BooleanNode boolNode =>
          new TypedTree.LiteralNode.BooleanNode(
            boolNode.Position, boolNode.Value, new Signature.Signature.PrimitiveSig(boolNode.Position, Signature.PrimitiveType.Boolean)
          ),
        ParseTree.LiteralNode.CharacterNode charNode =>
          new TypedTree.LiteralNode.CharacterNode(
            charNode.Position, charNode.Value, new Signature.Signature.PrimitiveSig(charNode.Position, Signature.PrimitiveType.Character)
          ),
        ParseTree.LiteralNode.StringNode strNode =>
          new TypedTree.LiteralNode.StringNode(
            strNode.Position, strNode.Value, new Signature.Signature.PrimitiveSig(strNode.Position, Signature.PrimitiveType.String)
          ),
        // Functions are a little more complex and we've broken them out into their own method
        ParseTree.LiteralNode.FunctionNode functionNode => TypeFunctionLiteralNode(parentCtx, functionNode),
        // NOTE: This should be impossible unless we forget to update the checker when adding literals
        _ => throw new Exception($"Unknown literal node type: {node.Kind}"),
      };
    }
    private static TypedTree.LiteralNode.FunctionNode TypeFunctionLiteralNode(
      Context parentCtx, ParseTree.LiteralNode.FunctionNode node
    ) {
      // Extract a signature from the literal
      var signature = ExtractFunctionSignatureFromFunctionLiteral(parentCtx, node);
      // Create a new context for the method
      var context = new Context(
        parentCtx.CurrentModule,
        signature,
        new Scope<Signature.Signature>(parentCtx.CurrentScope)
      );
      // Map the parameter nodes
      var parameters = new List<TypedTree.LiteralNode.FunctionNode.ParameterNode>();
      foreach (var param in node.Parameters) {
        // Map the parameter type
        var paramSignature = TypeTypeNode(parentCtx, param.Type);
        // Add the parameter to the method scope
        context.CurrentScope.AddDeclaration(param.Position, param.Name.Name, paramSignature);
        // Map the parameter node
        var typedParam = new TypedTree.LiteralNode.FunctionNode.ParameterNode(param.Position, param.Name.Name, paramSignature);
        parameters.Add(typedParam);
      }
      // Map the method body
      bool HasReturn = false;
      var body = TypeBlockStatementNode(context, node.Body, ref HasReturn);
      if (!HasReturn && signature.ReturnType is not Signature.Signature.PrimitiveSig { Type: Signature.PrimitiveType.Void }) {
        throw new NoReturnStatement(node.Position, node.Name.Name, signature.ReturnType.ToString());
      }
      // Map the node itself
      return new TypedTree.LiteralNode.FunctionNode(
        node.Position,
        node.Name.Name,
        parameters.ToArray(),
        body,
        context.CurrentScope,
        signature
      );
    }
    private static Signature.Signature.MethodSig ExtractFunctionSignatureFromFunctionLiteral(
      Context parentCtx, ParseTree.LiteralNode.FunctionNode node
    ) {
      // Map our return type
      var returnSignature = TypeTypeNode(parentCtx, node.ReturnType);
      // Map our parameters
      var parameterSignatures = new List<Signature.Signature>();
      foreach (var param in node.Parameters) {
        var paramSignature = TypeTypeNode(parentCtx, param.Type);
        parameterSignatures.Add(paramSignature);
      }
      // Create a signature for the method
      return new Signature.Signature.MethodSig(node.Position, parameterSignatures.ToArray(), returnSignature);
    }
    #endregion
    // --- Types ---
    #region Types
    private static Signature.Signature TypeTypeNode(Context parentCtx, ParseTree.TypeNode node) {
      return node switch {
        // TODO: We drop `size` here (what was it used for previously)???
        ParseTree.TypeNode.ArrayNode arrNode =>
          new Signature.Signature.ArraySig(node.Position, TypeTypeNode(parentCtx, arrNode.ElementType)),
        ParseTree.TypeNode.SimpleNode simpleNode =>
          new Signature.Signature.PrimitiveSig(node.Position, simpleNode.Type),
        // NOTE: This should be impossible unless we forget to update the checker when adding types
        _ => throw new Exception($"Unknown type node type: {node.Kind}"),
      };
    }
    #endregion
    // --- Locations ---
    #region Locations
    private static TypedTree.LocationNode TypeLocationNode(Context parentCtx, ParseTree.LocationNode node) {
      // Primitives can only be used as callees, if they reach here then they are being used as a location
      if (node.IsPrimitive) throw new InvalidPrimitiveUse(node.Position);
      else {
        switch (node) {
          case ParseTree.LocationNode.ArrayNode arrNode: {
              // Map the root of the array location
              var root = TypeLocationNode(parentCtx, arrNode.Root);
              // Ensure the root expression is an array type
              if (root.LocationType is not Signature.Signature.ArraySig arraySignature) {
                throw new ArrayAccessOnNonArray(arrNode.Root.Position);
              }
              // Map the index expression
              var indexExpr = TypeExpressionNode(parentCtx, arrNode.IndexExpr);
              // Ensure the index expression is an integer
              TypeCheckerCore.CheckSignature(
                expected: new Signature.Signature.PrimitiveSig(indexExpr.Position, Signature.PrimitiveType.Int),
                received: indexExpr.ExpressionType
              );
              // Produce the typed array access node with the inner type of the array as its signature
              return new TypedTree.LocationNode.ArrayNode(node.Position, root, indexExpr, arraySignature.Typ);
            }
          case ParseTree.LocationNode.MemberNode memberNode: {
              // Map the root of the member access
              var root = TypeLocationNode(parentCtx, memberNode.Root);
              // Ensure the root expression is a module type
              if (root.LocationType is not Signature.Signature.ModuleSig moduleSignature) {
                throw new MemberAccessOnNonModule(node.Position);
              }
              // Ensure the module has the member being accessed
              if (!moduleSignature.Members.TryGetValue(memberNode.Member, out Signature.Signature memberSignature)) {
                throw new MemberAccessUnknown(node.Position, memberNode.Member);
              }
              // Map the node itself
              return new TypedTree.LocationNode.MemberNode(node.Position, root, memberNode.Member, memberSignature);
            }
          case ParseTree.LocationNode.IdentifierNode identifierNode: {
              // Find the signature of the identifier in the current scope
              var signature = parentCtx.CurrentScope.GetDeclaration(node.Position, identifierNode.Name);
              // Map the node itself
              return new TypedTree.LocationNode.IdentifierNode(node.Position, identifierNode.Name, signature);
            }
          // NOTE: This should be impossible unless we forget to update the checker when adding locations
          default: throw new Exception($"Unknown location node type: {node.Kind}");
        }
      }
    }
    #endregion
  }
}
