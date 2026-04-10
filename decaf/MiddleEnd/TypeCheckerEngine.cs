using System;
using System.Linq;

using Decaf.IR.TypedTree;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.TypeCheckingErrors;

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
}
