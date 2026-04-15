using System;

using Signature = Decaf.IR.Signature;
using Decaf.Utils.Errors.TypeCheckingErrors;

namespace Decaf.MiddleEnd.TypeChecker {
  /// <summary>
  /// The small core of the type checker.
  /// This is where the actual signature checking logic lives, and all the type rules are encoded.
  /// 
  /// The rest of the type checker is mostly just plumbing to build signatures and compare them using the core.
  /// This module isn't overly decaf specific and could be easily re-used in other contexts.
  /// </summary>
  internal static class TypeCheckerCore {
    // TODO: Figure out if we actually need these?
    // // Builders
    // public static TypedTree.Signature.PrimitiveSignature BuildSimpleSignature(Position position, TypedTree.PrimitiveType type) {
    //   return new TypedTree.Signature.PrimitiveSignature(position, type);
    // }
    // public static TypedTree.Signature BuildSimpleCompoundSignature(bool IsArray, ParseTree.TypeNode node) {
    //   TypedTree.Signature baseType = node.Type switch {
    //     ParseTree.PrimitiveType.Int => BuildSimpleSignature(node.Position, TypedTree.PrimitiveType.Int),
    //     ParseTree.PrimitiveType.Boolean => BuildSimpleSignature(node.Position, TypedTree.PrimitiveType.Boolean),
    //     ParseTree.PrimitiveType.Void => BuildSimpleSignature(node.Position, TypedTree.PrimitiveType.Void),
    //     ParseTree.PrimitiveType.String => BuildSimpleSignature(node.Position, TypedTree.PrimitiveType.String),
    //     // ParseTree.PrimitiveType.Custom => new Signature.CustomSignature(node.Position, node.Content),
    //     // NOTE: This case can never be hit c# exhaustiveness is just being weird
    //     _ => throw new Exception("Impossible: unknown primitive type"),
    //   };
    //   return IsArray switch {
    //     true => new Signature.ArraySignature(node.Position, baseType),
    //     false => baseType,
    //   };
    // }
    // Internal Helpers
    // TODO: Maybe we just convert this to `toString` overrides on the signatures?
    private static string GetTypeCategoryName(Signature.Signature signature) {
      return signature switch {
        Signature.Signature.PrimitiveSig _ => "primitive",
        Signature.Signature.ModuleSig _ => "module",
        Signature.Signature.MethodSig _ => "method",
        Signature.Signature.ArraySig _ => "array",
        // NOTE: This is never possible c# is bad at exhaustiveness checking with records
        _ => throw new Exception($"Unknown signature type {signature.GetType()}"),
      };
    }
    // TODO: This needs to be updated at the bare minimum
    private static string GetPrimitiveTypeName(Signature.PrimitiveType type) {
      return type switch {
        Signature.PrimitiveType.Int => "int",
        Signature.PrimitiveType.Boolean => "boolean",
        Signature.PrimitiveType.Void => "void",
        Signature.PrimitiveType.Character => "char",
        Signature.PrimitiveType.String => "string",
        // NOTE: This is never possible c# is bad at exhaustiveness checking with enums
        _ => throw new Exception($"Unknown primitive type {type}"),
      };
    }
    // Checkers
    public static void CheckSignature(
      Signature.Signature expected,
      Signature.Signature received
    ) {
      switch ((expected, received)) {
        // Valid Cases (lhs == rhs)
        case (Signature.Signature.ModuleSig e, Signature.Signature.ModuleSig r):
          CheckModuleSignature(e, r);
          break;
        case (Signature.Signature.MethodSig e, Signature.Signature.MethodSig r):
          CheckMethodSignature(e, r);
          break;
        case (Signature.Signature.ArraySig e, Signature.Signature.ArraySig r):
          CheckArraySignature(e, r);
          break;
        case (Signature.Signature.PrimitiveSig e, Signature.Signature.PrimitiveSig r):
          CheckPrimitiveSignature(e, r);
          break;
        // Currently we only have four type categories which have no subtyping relationships, so if we do not match on one of the above
        // cases we know that the types do not match.
        default: throw new LhsNotRhs(expected.Position, GetTypeCategoryName(expected), GetTypeCategoryName(received));
      }
    }
    public static void CheckModuleSignature(
      Signature.Signature.ModuleSig expected,
      Signature.Signature.ModuleSig received
    ) {
      // NOTE: We should never actually end up here given modules are not first class citizens,
      //       however it is easy enough to compare them so we implement it anyways.
      //       If we were ever to add first class modules this would be necessary,
      //       though some of the rules around compatibility may need to be re-thought.

      // The current rules are, that we expect the modules to have the same number of members
      // and that the members on the modules match by name and type.

      // Check that we have the same number of members on both sides
      if (expected.Members.Count != received.Members.Count) {
        // TODO: Throw a more specific error from `utils/errors` here
        throw new LhsNotRhs(received.Position, $"{expected.Members.Count} members", $"{received.Members.Count} members");
      }
      // Check that the members on the modules match
      foreach (var expectedMember in expected.Members) {
        if (!received.Members.TryGetValue(expectedMember.Key, out Signature.Signature value)) {
          // TODO: Throw a more specific error from `utils/errors` here
          throw new LhsNotRhs(received.Position, $"member named {expectedMember.Key}", "no such member");
        }
        // Check that the types are the same
        CheckSignature(expectedMember.Value, value);
      }
    }
    public static void CheckMethodSignature(
      Signature.Signature.MethodSig expected,
      Signature.Signature.MethodSig received
    ) {
      // The rules for method signature compatibility are as follows:
      // 1. The parameter counts must be the same on both sides
      // 2. The parameter types must be the same on both sides
      // 3. The return types must be the same on both sides

      // Check that we have the same number of parameters on both sides
      if (expected.ParameterTypes.Length != received.ParameterTypes.Length) {
        // TODO: Throw a more specific error from `utils/errors` here
        throw new LhsNotRhs(
          received.Position,
          $"method with {expected.ParameterTypes.Length} parameters",
          $"method with {received.ParameterTypes.Length} parameters"
        );
      }
      // Check that the parameters are the same types on both sides
      for (int i = 0; i < expected.ParameterTypes.Length; i++) {
        // NOTE: The array indexing is safe because of the parameter count check above
        CheckSignature(expected.ParameterTypes[i], received.ParameterTypes[i]);
      }
      // Check that the return types are the same on both sides
      CheckSignature(expected.ReturnType, received.ReturnType);
    }
    public static void CheckArraySignature(
      Signature.Signature.ArraySig expected,
      Signature.Signature.ArraySig received
    ) {
      // The rules for array signature compatibility are as follows:
      // 1. The inner types must be the same on both sides

      CheckSignature(expected.Typ, received.Typ);
    }
    public static void CheckPrimitiveSignature(
      Signature.Signature.PrimitiveSig expected,
      Signature.Signature.PrimitiveSig received
    ) {
      // The rules for primitive signature compatibility are as follows:
      // 1. The types must be the same on both sides

      if (expected.Type != received.Type) {
        throw new LhsNotRhs(expected.Position, GetPrimitiveTypeName(expected.Type), GetPrimitiveTypeName(received.Type));
      }
    }
  }
}
