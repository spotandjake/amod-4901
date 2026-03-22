using System;
using System.Linq;

using Decaf.IR.TypedTree;
using TypedTree = Decaf.IR.TypedTree;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.TypeCheckingErrors;
using System.Collections.Generic;

namespace Decaf.MiddleEnd.TypeChecker {
  // The private engine that handles type checking, rules
  static class TypeCheckerEngine {
    // Builders
    public static Signature.PrimitiveSignature BuildSimpleSignature(Position position, PrimitiveType type) {
      return new Signature.PrimitiveSignature(position, type);
    }
    public static Signature BuildSimpleCompoundSignature(bool IsArray, ParseTree.TypeNode node) {
      Signature baseType = node.Type switch {
        ParseTree.PrimitiveType.Int => BuildSimpleSignature(node.Position, PrimitiveType.Int),
        ParseTree.PrimitiveType.Boolean => BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
        ParseTree.PrimitiveType.Void => BuildSimpleSignature(node.Position, PrimitiveType.Void),
        ParseTree.PrimitiveType.Custom => new Signature.CustomSignature(node.Position, node.Content),
        // NOTE: This case can never be hit c# exhaustiveness is just being weird
        _ => throw new Exception("Impossible: unknown primitive type"),
      };
      return IsArray switch {
        true => new Signature.ArraySignature(node.Position, baseType),
        false => baseType,
      };
    }
    // Internal Helpers
    private static string GetTypeCategoryName(Signature signature) {
      return signature switch {
        Signature.PrimitiveSignature _ => "primitive",
        Signature.ClassSignature _ => "class",
        Signature.MethodSignature _ => "method",
        Signature.ArraySignature _ => "array",
        Signature.CustomSignature _ => "custom type",
        // NOTE: This is never possible c# is bad at exhaustiveness checking with records
        _ => throw new Exception($"Unknown signature type {signature.GetType()}"),
      };
    }
    private static string GetPrimitiveTypeName(PrimitiveType type) {
      return type switch {
        PrimitiveType.Int => "int",
        PrimitiveType.Boolean => "boolean",
        PrimitiveType.Void => "void",
        PrimitiveType.Null => "null",
        PrimitiveType.Character => "char",
        PrimitiveType.String => "string",
        // NOTE: This is never possible c# is bad at exhaustiveness checking with enums
        _ => throw new Exception($"Unknown primitive type {type}"),
      };
    }
    private static Signature ResolveCustomSignature(Signature.CustomSignature signature, Scope<Signature> scope) {
      // Look up the custom signature in the scope
      var resolvedSignature = scope.GetDeclaration(signature.Position, signature.Name);
      // Ensure that the signature we actually found is a class signature
      if (resolvedSignature is not Signature.ClassSignature) {
        throw new LhsNotRhs(signature.Position, $"custom type named {signature.Name}", "no such class");
      }
      return resolvedSignature;
    }
    // Checkers
    public static void CheckClassSignature(
      Signature.ClassSignature expected,
      Signature.ClassSignature received,
      Scope<Signature> scope
    ) {
      // Check that we have the same number of members on both sides
      if (expected.Members.Count != received.Members.Count) {
        throw new LhsNotRhs(expected.Position, $"{expected.Members.Count} members", $"{received.Members.Count} members");
      }
      // Check that the members on the classes match
      foreach (var expectedMember in expected.Members) {
        if (!received.Members.TryGetValue(expectedMember.Key, out Signature value)) {
          throw new LhsNotRhs(expected.Position, $"member named {expectedMember.Key}", "no such member");
        }
        // Check that the types are the same
        CheckType(expectedMember.Value, value, scope);
      }
    }
    public static void CheckMethodSignature(
      Signature.MethodSignature expected,
      Signature.MethodSignature received,
      Scope<Signature> scope
    ) {
      // Check that the parameter counts are equal on both sides
      if (expected.ParameterTypes.Length != received.ParameterTypes.Length) {
        throw new LhsNotRhs(expected.Position, $"method with {expected.ParameterTypes.Length} parameters", $"method with {received.ParameterTypes.Length} parameters");
      }
      // Check that the parameters are the same types on both sides
      foreach (var (expectedParam, receivedParam) in expected.ParameterTypes.Zip(received.ParameterTypes)) {
        CheckType(expectedParam, receivedParam, scope);
      }
      // Check that the return types are the same on both sides
      CheckType(expected.ReturnType, received.ReturnType, scope);
    }
    public static void CheckArraySignature(
      Signature.ArraySignature expected,
      Signature.ArraySignature received,
      Scope<Signature> scope
    ) {
      // In order for an array signature to match the inner types must match
      CheckType(expected.Typ, received.Typ, scope);
    }
    public static void CheckPrimitiveSignature(
      Signature.PrimitiveSignature expected,
      Signature.PrimitiveSignature received,
      Scope<Signature> scope
    ) {
      // In order for a primitive signature to match the types must match
      if (expected.Type != received.Type) {
        throw new LhsNotRhs(expected.Position, GetPrimitiveTypeName(expected.Type), GetPrimitiveTypeName(received.Type));
      }
    }
    public static void CheckType(Signature expected, Signature received, Scope<Signature> scope) {
      switch ((expected, received)) {
        // Valid Cases (lhs == rhs)
        case (Signature.ClassSignature e, Signature.ClassSignature r):
          CheckClassSignature(e, r, scope);
          break;
        case (Signature.MethodSignature e, Signature.MethodSignature r):
          CheckMethodSignature(e, r, scope);
          break;
        case (Signature.ArraySignature e, Signature.ArraySignature r):
          CheckArraySignature(e, r, scope);
          break;
        case (Signature.PrimitiveSignature e, Signature.PrimitiveSignature r):
          CheckPrimitiveSignature(e, r, scope);
          break;
        // Custom Cases
        case (Signature.CustomSignature e, _):
          CheckType(ResolveCustomSignature(e, scope), received, scope);
          break;
        case (_, Signature.CustomSignature r):
          CheckType(expected, ResolveCustomSignature(r, scope), scope);
          break;
        // NOTE: I think we want to resolve custom signatures first be they on the `e` or `r` side and then recall CheckType
        // Invalid Cases (lhs != rhs)
        default:
          throw new LhsNotRhs(expected.Position, GetTypeCategoryName(expected), GetTypeCategoryName(received));
      }
    }
  }
  // The Actual type checking implement
  public class TypeChecker {
    private TypeChecker() { }
    private readonly record struct TypeCheckContext(
#nullable enable
      Signature.ClassSignature? CurrentClass,
      Signature.MethodSignature? CurrentMethod,
#nullable disable
      Scope<Signature> CurrentScope
    ) {
#nullable enable
      public Signature.ClassSignature? CurrentClass { get; } = CurrentClass;
      public Signature.MethodSignature? CurrentMethod { get; } = CurrentMethod;
#nullable disable
      public Scope<Signature> CurrentScope { get; } = CurrentScope;
    }
    public static ProgramNode TypeProgramNode(ParseTree.ProgramNode node) {
      // Initialize a new context for the program.
      // (NOTE: The new scope is used to track signatures rather than just existence and use of declarations)
      var programContext = new TypeCheckContext(null, null, new Scope<Signature>(null));
      // Map the internal classes
      var classes = node.Classes.Select(decl => TypeDeclClassNode(decl, programContext)).ToArray();
      return new ProgramNode(node.Position, classes, programContext.CurrentScope);
    }
    // General
    private static BlockNode TypeBlockNode(
      ParseTree.BlockNode node,
      TypeCheckContext parentContext,
      ref bool HasReturn
    ) {
      // Create a new context for the block
      var context = new TypeCheckContext(
        parentContext.CurrentClass,
        parentContext.CurrentMethod,
        new Scope<Signature>(parentContext.CurrentScope)
      );
      // Map the declarations
      var decls = node.Declarations.Select(decl => TypeDeclVariableNode(decl, context)).ToArray();
      // Map the statements
      var statements = new List<StatementNode>();
      foreach (var stmt in node.Statements) {
        statements.Add(TypeStatementNode(stmt, context, ref HasReturn));
      }
      // Return a new mapped block
      return new BlockNode(node.Position, decls, statements.ToArray(), context.CurrentScope);
    }
    // DeclarationNodes
    private static DeclarationNode.ClassNode TypeDeclClassNode(
      ParseTree.DeclarationNode.ClassNode node,
      TypeCheckContext parentContext
    ) {
      if (node.SuperClassName != null) {
        // TODO: Handle validation of superClass
        throw new NotImplementedException("Subtyping and inheritance are not implemented");
      }
      // Create a base signature and register the class in the parent scope
      var classSignature = new Signature.ClassSignature(node.Position, []);
      parentContext.CurrentScope.AddDeclaration(node.Position, node.Name, classSignature);
      // We create a new context for the class
      var context = new TypeCheckContext(
        classSignature,
        null,
        new Scope<Signature>(parentContext.CurrentScope)
      );
      // Map the fields of the class
      var fields = node.Fields.Select(field => {
        var typedField = TypeDeclVariableNode(field, context);
        foreach (var bind in typedField.Binds) {
          // After mapping the method we need to update the class signature and the global scope
          context.CurrentClass.Members[bind.Name] = bind.Signature;
        }
        // Update the global scope with the new partial signature
        parentContext.CurrentScope.SetDeclaration(node.Position, node.Name, context.CurrentClass);
        // Return the mapped class
        return typedField;
      }).ToArray();
      // Map the methods
      var methods = node.Methods.Select(method => {
        var typedMethod = TypeDeclMethodNode(method, context);
        // After mapping the method we need to update the class signature and the global scope
        context.CurrentClass.Members[typedMethod.Name] = typedMethod.Signature;
        // Update the global scope with the new partial signature
        parentContext.CurrentScope.SetDeclaration(node.Position, node.Name, context.CurrentClass);
        // Return the mapped class
        return typedMethod;
      }).ToArray();
      // Return the mapped class node
      return new DeclarationNode.ClassNode(
        node.Position,
        node.Name,
        node.SuperClassName,
        fields,
        methods,
        context.CurrentScope,
        context.CurrentClass
      );
    }
    private static DeclarationNode.VariableNode TypeDeclVariableNode(
      ParseTree.DeclarationNode.VariableNode node,
      TypeCheckContext parentContext
    ) {
      // Map the variable declaration node, adding the variables to the current scope as we go
      var binds = node.Binds.Select(bind => {
        // Create the signature for the bind
        var signature = TypeCheckerEngine.BuildSimpleCompoundSignature(bind.IsArray, node.Type);
        // Add the bind to the scope
        parentContext.CurrentScope.AddDeclaration(bind.Position, bind.Name, signature);
        // Map the bind
        return new DeclarationNode.VariableNode.BindNode(bind.Position, bind.Name, signature);
      }).ToArray();
      // Map the declaration node
      return new DeclarationNode.VariableNode(node.Position, binds);
    }
    private static DeclarationNode.MethodNode TypeDeclMethodNode(
      ParseTree.DeclarationNode.MethodNode node,
      TypeCheckContext parentContext
    ) {
      // Create a signature for the return type
      var returnSignature = TypeCheckerEngine.BuildSimpleCompoundSignature(false, node.ReturnType);
      // Create a signature for the parameters
      var parameterSignatures = node.Parameters.Select(
        param => (param, TypeCheckerEngine.BuildSimpleCompoundSignature(param.IsArray, param.ParamType))
      ).ToArray();
      // Create a signature for the method
      var signature = new Signature.MethodSignature(
        node.Position,
        returnSignature,
        parameterSignatures.Select(param => param.Item2).ToArray()
      );
      // Add the method to the parent scope
      parentContext.CurrentScope.AddDeclaration(node.Position, node.Name, signature);
      // Create a new context for the method
      var context = new TypeCheckContext(
        parentContext.CurrentClass,
        signature,
        new Scope<Signature>(parentContext.CurrentScope)
      );
      // Map the parameter nodes
      var parameters = parameterSignatures.Select(paramBundle => {
        var (param, paramSignature) = paramBundle;
        // Add the parameter to the method scope
        context.CurrentScope.AddDeclaration(param.Position, param.Name, paramSignature);
        // Return the mapped parameter node
        return new DeclarationNode.MethodNode.ParameterNode(param.Position, param.Name, paramSignature);
      }).ToArray();
      // Map the method body
      bool HasReturn = false;
      var body = TypeBlockNode(node.Body, context, ref HasReturn);
      if (!HasReturn && signature.ReturnType != TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Void)) {
        throw new LhsNotRhs(node.Position, $"A return value", "no return statement in the method body");
      }
      // Map the method node
      return new DeclarationNode.MethodNode(node.Position, node.Name, parameters, body, context.CurrentScope, signature);
    }
    // StatementNodes
    private static StatementNode TypeStatementNode(
      ParseTree.StatementNode node,
      TypeCheckContext parentContext,
      ref bool HasReturn
    ) {
      return node switch {
        ParseTree.StatementNode.AssignmentNode assignment => TypeStatementAssignmentNode(assignment, parentContext),
        ParseTree.StatementNode.ExprNode exprStmt => TypeStatementExprNode(exprStmt, parentContext),
        ParseTree.StatementNode.IfNode ifNode => TypeStatementIfNode(ifNode, parentContext, ref HasReturn),
        ParseTree.StatementNode.WhileNode whileNode => TypeStatementWhileNode(whileNode, parentContext, ref HasReturn),
        ParseTree.StatementNode.ContinueNode continueNode => TypeStatementContinueNode(continueNode, parentContext),
        ParseTree.StatementNode.BreakNode breakNode => TypeStatementBreakNode(breakNode, parentContext),
        ParseTree.StatementNode.ReturnNode returnNode => TypeStatementReturnNode(returnNode, parentContext, ref HasReturn),
        _ => throw new Exception($"Unknown statement node type: {node.Kind}"),
      };
    }
    private static StatementNode.AssignmentNode TypeStatementAssignmentNode(
      ParseTree.StatementNode.AssignmentNode node,
      TypeCheckContext parentContext
    ) {
      // Map the left and right hand sides
      var location = TypeExpressionLocationNode(node.Location, parentContext);
      var expression = TypeExpressionNode(node.Expression, parentContext);
      // Ensure lhs == rhs
      TypeCheckerEngine.CheckType(location.ExpressionType, expression.ExpressionType, parentContext.CurrentScope);
      // Map the node itself
      return new StatementNode.AssignmentNode(node.Position, location, expression);
    }
    private static StatementNode.ExprNode TypeStatementExprNode(
      ParseTree.StatementNode.ExprNode node,
      TypeCheckContext parentContext
    ) {
      // Map the inner expression
      var content = TypeExpressionNode(node.Content, parentContext);
      // NOTE: It might make sense to check that the expression returns void
      // Map the node itself
      return new StatementNode.ExprNode(node.Position, content);
    }
    private static StatementNode.IfNode TypeStatementIfNode(
      ParseTree.StatementNode.IfNode node,
      TypeCheckContext parentContext,
      ref bool HasReturn
    ) {
      // Map the condition
      var condition = TypeExpressionNode(node.Condition, parentContext);
      // Check the condition is a boolean
      TypeCheckerEngine.CheckType(
        TypeCheckerEngine.BuildSimpleSignature(node.Condition.Position, PrimitiveType.Boolean),
        condition.ExpressionType,
        parentContext.CurrentScope
      );
      // Map the true branch & false branch
      var trueBranch = TypeBlockNode(node.TrueBranch, parentContext, ref HasReturn);
      var falseBranch = node.FalseBranch != null ? TypeBlockNode(node.FalseBranch, parentContext, ref HasReturn) : null;
      // Map the node itself
      return new StatementNode.IfNode(node.Position, condition, trueBranch, falseBranch);
    }
    private static StatementNode.WhileNode TypeStatementWhileNode(
      ParseTree.StatementNode.WhileNode node,
      TypeCheckContext parentContext,
      ref bool HasReturn
      ) {
      // Map the condition
      var condition = TypeExpressionNode(node.Condition, parentContext);
      // Check the condition is a boolean
      TypeCheckerEngine.CheckType(
        TypeCheckerEngine.BuildSimpleSignature(node.Condition.Position, PrimitiveType.Boolean),
        condition.ExpressionType,
        parentContext.CurrentScope
      );
      // Map the body
      var body = TypeBlockNode(node.Body, parentContext, ref HasReturn);
      // Map the node itself
      return new StatementNode.WhileNode(node.Position, condition, body);
    }
    private static StatementNode.ContinueNode TypeStatementContinueNode(
     ParseTree.StatementNode.ContinueNode node,
     TypeCheckContext _
   ) {
      // We just map to the type tree directly as there are no type rules
      return new StatementNode.ContinueNode(node.Position);
    }
    private static StatementNode.BreakNode TypeStatementBreakNode(
     ParseTree.StatementNode.BreakNode node,
     TypeCheckContext _
   ) {
      // We just map to the type tree directly as there are no type rules
      return new StatementNode.BreakNode(node.Position);
    }
    private static StatementNode.ReturnNode TypeStatementReturnNode(
      ParseTree.StatementNode.ReturnNode node,
      TypeCheckContext parentContext,
      ref bool HasReturn
    ) {
      HasReturn = true;
      // Ensure we are actually in a method, note that this will 
      // never be hit due to parsing but it's a future check if we ever allowed top level statements
      if (parentContext.CurrentMethod == null) {
        throw new ReturnUseOutsideOfClass(node.Position);
      }
      // Get the expected return type
      var expectedReturnType = parentContext.CurrentMethod.ReturnType;
      // Map the return value if it exists, otherwise create a void literal
      var returnValue = node.Value switch {
        null => null,
        _ => TypeExpressionNode(node.Value, parentContext)
      };
      var returnSignature = node.Value switch {
        null => TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Void),
        _ => returnValue.ExpressionType
      };
      // Ensure the return type matches the method signature
      TypeCheckerEngine.CheckType(expectedReturnType, returnSignature, parentContext.CurrentScope);
      // Map the node itself
      return new StatementNode.ReturnNode(node.Position, returnValue);
    }
    // Expression Nodes
    private static ExpressionNode TypeExpressionNode(
      ParseTree.ExpressionNode node,
      TypeCheckContext parentContext
    ) {
      return node switch {
        ParseTree.ExpressionNode.CallNode call => TypeExpressionCallNode(call, parentContext),
        ParseTree.ExpressionNode.BinopNode binop => TypeExpressionBinopNode(binop, parentContext),
        ParseTree.ExpressionNode.PrefixNode prefix => TypeExpressionPrefixNode(prefix, parentContext),
        ParseTree.ExpressionNode.NewClassNode newClass => TypeExpressionNewClassNode(newClass, parentContext),
        ParseTree.ExpressionNode.NewArrayNode newArray => TypeExpressionNewArrayNode(newArray, parentContext),
        ParseTree.ExpressionNode.LocationNode location => TypeExpressionLocationNode(location, parentContext),
        ParseTree.ExpressionNode.ThisNode thisNode => TypeExpressionThisNode(thisNode, parentContext),
        ParseTree.ExpressionNode.IdentifierNode identifier => TypeExpressionIdentifierNode(identifier, parentContext),
        ParseTree.ExpressionNode.LiteralNode literal => TypeExpressionLiteralNode(literal, parentContext),
        _ => throw new Exception($"Unknown expression node type: {node.Kind}"),
      };
    }
    private static ExpressionNode.CallNode TypeExpressionCallNode(
      ParseTree.ExpressionNode.CallNode node,
      TypeCheckContext parentContext
    ) {
      // Map the path and find the signature
      ExpressionNode.LocationNode path = node.IsPrimitive switch {
        true => MapPrimitivePath(node.Path, parentContext),
        false => TypeExpressionLocationNode(node.Path, parentContext)
      };
      // Map the arguments
      var args = node.Arguments.Select(arg => TypeExpressionNode(arg, parentContext)).ToArray();
      // Validate the path is actually a method
      if (path.ExpressionType is not Signature.MethodSignature methodSignature) {
        throw new CallOnNonMethod(node.Position);
      }
      // Create a signature for the call node based on the values
      var signature = new Signature.MethodSignature(
        node.Position,
        ((Signature.MethodSignature)path.ExpressionType).ReturnType,
        args.Select(arg => arg.ExpressionType).ToArray()
      );
      // Check the signature matches the expected signature
      TypeCheckerEngine.CheckType(methodSignature, signature, parentContext.CurrentScope);
      // Map the node itself
      return new ExpressionNode.CallNode(node.Position, node.IsPrimitive, path, args, signature.ReturnType);
    }
    private static ExpressionNode.LocationNode MapPrimitivePath(
      ParseTree.ExpressionNode.LocationNode node,
      TypeCheckContext _
    ) {
      // Ensure that the root is just a simple identifier
      if (node.Root is not ParseTree.ExpressionNode.IdentifierNode) {
        // NOTE: This case can never be hit due to parsing but its a sanity check for the future
        throw new Exception("Primitive callouts must be simple identifiers");
      }
      // Get the raw string value
      var primitiveName = ((ParseTree.ExpressionNode.IdentifierNode)node.Root).Name;
      // Get the signature of the primitive callout based on the name, this will throw if the primitive name is invalid
      var signature = PrimitiveTypes.GetPrimitiveCallSignature(primitiveName, node.Position);
      // Map the node itself, we know the signature is correct based on the primitive name so
      return new ExpressionNode.LocationNode(
        node.Position,
        new ExpressionNode.IdentifierNode(node.Position, primitiveName, signature),
        null,
        null,
        signature
      );
    }
    private static ExpressionNode.BinopNode TypeExpressionBinopNode(
      ParseTree.ExpressionNode.BinopNode node,
      TypeCheckContext parentContext
    ) {
      // Map the left hand side
      var lhs = TypeExpressionNode(node.Lhs, parentContext);
      // Map the right hand side
      var rhs = TypeExpressionNode(node.Rhs, parentContext);
      // Get the expected signature of the operation based on the operator
      var expectedSignature = node.Operator switch {
        // Arithmetic operators: (int, int) => int
        "+" or "-" or "*" or "/" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
          [
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int)
          ]
        ),
        // Relational operators: (int, int) => boolean
        "<" or ">" or "<=" or ">=" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
          [
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int)
          ]
        ),
        // Equality operators: (a, a) => boolean
        "==" or "!=" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
          [
            lhs.ExpressionType,
            lhs.ExpressionType
          ]
        ),
        // Conditional operators: (boolean, boolean) => boolean
        "&&" or "||" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
          [
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean)
          ]
        ),
        _ => throw new Exception($"Unknown binary operator {node.Operator}"),
      };
      // Construct the actual signature of the operator application
      var actualSignature = new Signature.MethodSignature(
        node.Position,
        expectedSignature.ReturnType,
        [lhs.ExpressionType, rhs.ExpressionType]
      );
      // Check the signature matches the expected signature
      TypeCheckerEngine.CheckType(expectedSignature, actualSignature, parentContext.CurrentScope);
      // Map the node itself
      return new ExpressionNode.BinopNode(node.Position, lhs, node.Operator, rhs, expectedSignature.ReturnType);
    }
    private static ExpressionNode.PrefixNode TypeExpressionPrefixNode(
      ParseTree.ExpressionNode.PrefixNode node,
      TypeCheckContext parentContext
    ) {
      // Construct the expected signature (boolean) => boolean for the not operator
      var expectedSignature = new Signature.MethodSignature(
        node.Position,
        TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
        [TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean)]
      );
      // Map the operand
      var operand = TypeExpressionNode(node.Operand, parentContext);
      // Construct the actual signature of the operator application
      var actualSignature = new Signature.MethodSignature(
        node.Position,
        expectedSignature.ReturnType,
        [operand.ExpressionType]
      );
      // Check the signature matches the expected signature
      TypeCheckerEngine.CheckType(expectedSignature, actualSignature, parentContext.CurrentScope);
      // Map the node itself
      return new ExpressionNode.PrefixNode(node.Position, node.Operator, operand, expectedSignature.ReturnType);
    }
    private static ExpressionNode.NewClassNode TypeExpressionNewClassNode(
      ParseTree.ExpressionNode.NewClassNode node,
      TypeCheckContext parentContext
    ) {
      // Map the path
      var path = TypeExpressionLocationNode(node.Path, parentContext);
      var signature = path.ExpressionType;
      // Ensure the path is actually a class type
      if (signature is not Signature.ClassSignature) {
        throw new InitializationOfNonClass(node.Position);
      }
      // Map the node itself
      return new ExpressionNode.NewClassNode(node.Position, path, signature);
    }
    private static ExpressionNode.NewArrayNode TypeExpressionNewArrayNode(
      ParseTree.ExpressionNode.NewArrayNode node,
      TypeCheckContext parentContext
    ) {
      // Map the size expression
      var sizeExpr = TypeExpressionNode(node.SizeExpr, parentContext);
      // Validate the size expression is an integer
      TypeCheckerEngine.CheckType(
        TypeCheckerEngine.BuildSimpleSignature(node.SizeExpr.Position, PrimitiveType.Int),
        sizeExpr.ExpressionType,
        parentContext.CurrentScope
      );
      // Validate the type expression is a compatible type
      // NOTE: This could break as we add more types
      if (node.Type.Type == ParseTree.PrimitiveType.Void) {
        throw new LhsNotRhs(node.Position, "array of void", "an array of `int`, `boolean` or `T`");
      }
      // Build the signature of the new array node
      var signature = TypeCheckerEngine.BuildSimpleCompoundSignature(true, node.Type);
      // Map the node itself
      return new ExpressionNode.NewArrayNode(node.Position, sizeExpr, signature);
    }
    private static ExpressionNode.LocationNode TypeExpressionLocationNode(
      ParseTree.ExpressionNode.LocationNode node,
      TypeCheckContext parentContext
    ) {
      // Map the root of the location expression
      var root = TypeExpressionNode(node.Root, parentContext);
      var signature = root.ExpressionType;
      // If we are mapping a path access then we need to validate the path
      if (node.Path != null) {
        // Ensure the root expression is a class type
        if (signature is not Signature.ClassSignature classSignature) {
          throw new MemberAccessOnNonClass(node.Position);
        }
        // Ensure the class has the member being accessed
        if (!classSignature.Members.TryGetValue(node.Path, out Signature value)) {
          throw new MemberAccessUnknown(node.Position, node.Path);
        }
        // Get the signature of the member being accessed
        signature = value;
      }
      // If we are mapping an array access then we need to validate the index and return the inner type of the array
      ExpressionNode indexExpr = null;
      if (node.IndexExpr != null) {
        // Ensure the root expression is an array type
        if (signature is not Signature.ArraySignature arraySignature) {
          throw new ArrayAccessOnNonArray(node.IndexExpr.Position);
        }
        // Map the index expression
        indexExpr = TypeExpressionNode(node.IndexExpr, parentContext);
        // Ensure the index expression is an integer
        TypeCheckerEngine.CheckType(
          TypeCheckerEngine.BuildSimpleSignature(indexExpr.Position, PrimitiveType.Int),
          indexExpr.ExpressionType,
          parentContext.CurrentScope
        );
        // Update the signature to be the inner type of the array
        signature = arraySignature.Typ;
      }
      // Map the node itself
      return new ExpressionNode.LocationNode(node.Position, root, node.Path, indexExpr, signature);
    }
    private static ExpressionNode.ThisNode TypeExpressionThisNode(
      ParseTree.ExpressionNode.ThisNode node,
      TypeCheckContext parentContext
    ) {
      // Get the current class context and return its partial signature
      var parentClassSignature = parentContext.CurrentClass;
      // NOTE: This could never actually happen due to parsing but it's a future check if we ever allowed top level statements
      if (parentClassSignature == null) {
        throw new ThisAccessOutsideOfClass(node.Position);
      }
      // NOTE: This only works because we require declarations to be defined before use, otherwise we would need to delay this
      return new ExpressionNode.ThisNode(node.Position, parentClassSignature);
    }
    private static ExpressionNode.IdentifierNode TypeExpressionIdentifierNode(
      ParseTree.ExpressionNode.IdentifierNode node,
      TypeCheckContext parentContext
    ) {
      // Find the signature of the identifier in the current scope
      var signature = parentContext.CurrentScope.GetDeclaration(node.Position, node.Name);
      // Map the node itself
      return new ExpressionNode.IdentifierNode(node.Position, node.Name, signature);
    }
    private static ExpressionNode.LiteralNode TypeExpressionLiteralNode(
      ParseTree.ExpressionNode.LiteralNode node,
      TypeCheckContext _
    ) {
      var content = TypeLiteralNode(node.Content);
      var signature = content switch {
        TypedTree.LiteralNodes.IntegerNode _ => TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
        TypedTree.LiteralNodes.CharacterNode _ => TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Character),
        TypedTree.LiteralNodes.StringNode _ => TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.String),
        TypedTree.LiteralNodes.BooleanNode _ => TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
        TypedTree.LiteralNodes.NullNode _ => TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Null),
        _ => throw new Exception($"Unknown literal node type {content.Kind}"),
      };
      return new ExpressionNode.LiteralNode(node.Position, content, signature);
    }
    // Literal Nodes
    private static LiteralNode TypeLiteralNode(ParseTree.LiteralNode node) {
      return node switch {
        ParseTree.LiteralNodes.IntegerNode n => TypeLiteralIntegerNode(n),
        ParseTree.LiteralNodes.CharacterNode n => TypeLiteralCharacterNode(n),
        ParseTree.LiteralNodes.StringNode n => TypeLiteralStringNode(n),
        ParseTree.LiteralNodes.BooleanNode n => TypeLiteralBooleanNode(n),
        ParseTree.LiteralNodes.NullNode n => TypeLiteralNullNode(n),
        _ => throw new Exception($"Unknown literal node type {node.Kind}"),
      };
      ;
    }
    private static TypedTree.LiteralNodes.IntegerNode TypeLiteralIntegerNode(ParseTree.LiteralNodes.IntegerNode node) {
      return new TypedTree.LiteralNodes.IntegerNode(node.Position, node.Value);
    }
    private static TypedTree.LiteralNodes.CharacterNode TypeLiteralCharacterNode(ParseTree.LiteralNodes.CharacterNode node) {
      return new TypedTree.LiteralNodes.CharacterNode(node.Position, node.Value);
    }
    private static TypedTree.LiteralNodes.StringNode TypeLiteralStringNode(ParseTree.LiteralNodes.StringNode node) {
      return new TypedTree.LiteralNodes.StringNode(node.Position, node.Value);
    }
    private static TypedTree.LiteralNodes.BooleanNode TypeLiteralBooleanNode(ParseTree.LiteralNodes.BooleanNode node) {
      return new TypedTree.LiteralNodes.BooleanNode(node.Position, node.Value);
    }
    private static TypedTree.LiteralNodes.NullNode TypeLiteralNullNode(ParseTree.LiteralNodes.NullNode node) {
      return new TypedTree.LiteralNodes.NullNode(node.Position);
    }
  }
}
