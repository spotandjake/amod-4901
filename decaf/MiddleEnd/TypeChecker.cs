using System;
using System.Linq;
using System.Collections.Generic;

using Decaf.IR.TypedTree;
using TypedTree = Decaf.IR.TypedTree;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.TypeCheckingErrors;
using Decaf.IR.PrimitiveDefinition;

namespace Decaf.MiddleEnd.TypeChecker {
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
        ParseTree.StatementNode.WhileNode whileNode => TypeStatementWhileNode(whileNode, parentContext),
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
      var location = TypeLocationNode(node.Location, parentContext);
      var expression = TypeExpressionNode(node.Expression, parentContext);
      // Ensure lhs == rhs
      TypeCheckerEngine.CheckType(location.LocationType, expression.ExpressionType, parentContext.CurrentScope);
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
      bool hasReturnTrueBranch = false;
      bool hasReturnFalseBranch = false;
      var trueBranch = TypeBlockNode(node.TrueBranch, parentContext, ref hasReturnTrueBranch);
      var falseBranch = node.FalseBranch != null ? TypeBlockNode(node.FalseBranch, parentContext, ref hasReturnFalseBranch) : null;
      // Properly set the HasReturn value for this if statement up the tree.
      if (!HasReturn) HasReturn = hasReturnTrueBranch && hasReturnFalseBranch;
      // Map the node itself
      return new StatementNode.IfNode(node.Position, condition, trueBranch, falseBranch);
    }
    private static StatementNode.WhileNode TypeStatementWhileNode(
      ParseTree.StatementNode.WhileNode node,
      TypeCheckContext parentContext
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
      bool hasReturn = false;
      var body = TypeBlockNode(node.Body, parentContext, ref hasReturn);
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
        ParseTree.ExpressionNode.LocationAccessNode location => TypeExpressionLocationAcccessNode(location, parentContext),
        ParseTree.ExpressionNode.LiteralNode literal => TypeExpressionLiteralNode(literal, parentContext),
        _ => throw new Exception($"Unknown expression node type: {node.Kind}"),
      };
    }
    private static ExpressionNode TypeExpressionCallNode(
      ParseTree.ExpressionNode.CallNode node,
      TypeCheckContext parentContext
    ) {
      if (node.IsPrimitive) {
        // Map the path and find the signature
        var (primSig, primDef) = MapPrimitivePath(node.Path, parentContext);
        // Map the arguments
        var args = node.Arguments.Select(arg => TypeExpressionNode(arg, parentContext)).ToArray();
        // Validate the path is actually a method
        if (primSig is not Signature.MethodSignature methodSignature) {
          throw new CallOnNonMethod(node.Position);
        }
        // Create a signature for the call node based on the values
        var signature = new Signature.MethodSignature(
          node.Position,
          ((Signature.MethodSignature)primSig).ReturnType,
          args.Select(arg => arg.ExpressionType).ToArray()
        );
        // Check the signature matches the expected signature
        TypeCheckerEngine.CheckType(methodSignature, signature, parentContext.CurrentScope);
        // Map the node itself
        return new ExpressionNode.PrimitiveNode(node.Position, primDef, args, signature.ReturnType);
      }
      else {
        // Map the path and find the signature
        LocationNode path = TypeLocationNode(node.Path, parentContext);
        // Map the arguments
        var args = node.Arguments.Select(arg => TypeExpressionNode(arg, parentContext)).ToArray();
        // Validate the path is actually a method
        if (path.LocationType is not Signature.MethodSignature methodSignature) {
          throw new CallOnNonMethod(node.Position);
        }
        // Create a signature for the call node based on the values
        var signature = new Signature.MethodSignature(
          node.Position,
          ((Signature.MethodSignature)path.LocationType).ReturnType,
          args.Select(arg => arg.ExpressionType).ToArray()
        );
        // Check the signature matches the expected signature
        TypeCheckerEngine.CheckType(methodSignature, signature, parentContext.CurrentScope);
        // Map the node itself
        return new ExpressionNode.CallNode(node.Position, path, args, signature.ReturnType);
      }
    }
    private static (Signature, PrimDefinition) MapPrimitivePath(
      ParseTree.LocationNode node,
      TypeCheckContext _
    ) {
      switch (node) {
        case ParseTree.LocationNode.IdentifierAccessNode identifierAccess:
          // Resolve the primitive name, if the primitive is invalid this will throw
          return PrimitiveTypes.GetPrimitiveCallSignature(identifierAccess.Name, node.Position);
        // NOTE: This case can never be hit due to parsing but its a sanity check for the future
        default: throw new Exception("Primitive callouts must be simple identifiers");
      }
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
        // Bitwise operators: (int, int) => int
        "&" or "|" or "<<" or ">>" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
          [
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
            TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int)
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
      var expectedSignature = node.Operator switch {
        // Not operator: (boolean) => boolean
        "!" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean),
          [TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Boolean)]
        ),
        // Bitwise not operator: (int) => int
        "~" => new Signature.MethodSignature(
          node.Position,
          TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int),
          [TypeCheckerEngine.BuildSimpleSignature(node.Position, PrimitiveType.Int)]
        ),
        _ => throw new Exception($"Unknown prefix operator {node.Operator}"),
      };
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
      var path = TypeLocationNode(node.Path, parentContext);
      var signature = path.LocationType;
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
    private static ExpressionNode.LocationAccessNode TypeExpressionLocationAcccessNode(
      ParseTree.ExpressionNode.LocationAccessNode node,
      TypeCheckContext parentContext
    ) {
      // Map the location
      var location = TypeLocationNode(node.Content, parentContext);
      // Map the node itself
      return new ExpressionNode.LocationAccessNode(node.Position, location, location.LocationType);
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
    // Locations
    private static LocationNode TypeLocationNode(ParseTree.LocationNode node, TypeCheckContext parentContext) {
      switch (node) {
        case ParseTree.LocationNode.ThisNode: {
            // Get the current class context and return its partial signature
            var parentClassSignature = parentContext.CurrentClass;
            // NOTE: This could never actually happen due to parsing but it's a future check if we ever allowed top level statements
            if (parentClassSignature == null) throw new ThisAccessOutsideOfClass(node.Position);
            // NOTE: This only works because we require declarations to be defined before use, otherwise we would need to delay t
            return new LocationNode.ThisNode(node.Position, parentClassSignature);
          }
        case ParseTree.LocationNode.IdentifierAccessNode identifierAccess: {
            // Find the signature of the identifier in the current scope
            var signature = parentContext.CurrentScope.GetDeclaration(node.Position, identifierAccess.Name);
            // Map the node itself
            return new LocationNode.IdentifierAccessNode(node.Position, identifierAccess.Name, signature);
          }
        case ParseTree.LocationNode.MemberAccessNode pathAccess: {
            // Map the root of the member access
            var root = TypeLocationNode(pathAccess.Root, parentContext);
            // Ensure the root expression is a class type
            if (root.LocationType is not Signature.ClassSignature classSignature) {
              throw new MemberAccessOnNonClass(node.Position);
            }
            // Ensure the class has the member being accessed
            if (!classSignature.Members.TryGetValue(pathAccess.Member, out Signature signature)) {
              throw new MemberAccessUnknown(node.Position, pathAccess.Member);
            }
            // Map the node itself
            return new LocationNode.MemberAccessNode(node.Position, root, pathAccess.Member, signature);
          }
        case ParseTree.LocationNode.ArrayAccessNode arrayAccess: {
            // Map the root of the array access
            var root = TypeLocationNode(arrayAccess.Root, parentContext);
            // Ensure the root expression is an array type
            if (root.LocationType is not Signature.ArraySignature arraySignature) {
              throw new ArrayAccessOnNonArray(arrayAccess.IndexExpr.Position);
            }
            // Map the index expression
            var indexExpr = TypeExpressionNode(arrayAccess.IndexExpr, parentContext);
            // Ensure the index expression is an integer
            TypeCheckerEngine.CheckType(
              TypeCheckerEngine.BuildSimpleSignature(indexExpr.Position, PrimitiveType.Int),
              indexExpr.ExpressionType,
              parentContext.CurrentScope
            );
            // Produce the typed array access node with the inner type of the array as its signature
            return new LocationNode.ArrayAccessNode(node.Position, root, indexExpr, arraySignature.Typ);
          }
        default: throw new Exception($"Unknown location node type: {node.Kind}");
      }
    }
  }
}
