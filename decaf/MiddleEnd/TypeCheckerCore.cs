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
        case (Signature.Signature.FunctionSig e, Signature.Signature.FunctionSig r):
          CheckFunctionSignature(e, r);
          break;
        case (Signature.Signature.ArraySig e, Signature.Signature.ArraySig r):
          CheckArraySignature(e, r);
          break;
        case (Signature.Signature.PrimitiveSig e, Signature.Signature.PrimitiveSig r):
          CheckPrimitiveSignature(e, r);
          break;
        // Currently we only have four type categories which have no subtyping relationships, so if we do not match on one of the above
        // cases we know that the types do not match.
        default: throw new LhsNotRhs(expected.Position, expected.ToString(), received.ToString());
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
        throw new LhsNotRhs(received.Position, $"{expected.Members.Count} members", $"{received.Members.Count} members");
      }
      // Check that the members on the modules match
      foreach (var expectedMember in expected.Members) {
        if (!received.Members.TryGetValue(expectedMember.Key, out Signature.Signature value)) {
          throw new LhsNotRhs(received.Position, $"member named {expectedMember.Key}", "no such member");
        }
        // Check that the types are the same
        CheckSignature(expectedMember.Value, value);
      }
    }
    public static void CheckFunctionSignature(
      Signature.Signature.FunctionSig expected,
      Signature.Signature.FunctionSig received
    ) {
      // The rules for function signature compatibility are as follows:
      // 1. The parameter counts must be the same on both sides
      // 2. The parameter types must be the same on both sides
      // 3. The return types must be the same on both sides

      // Check that we have the same number of parameters on both sides
      if (expected.ParameterTypes.Length != received.ParameterTypes.Length) {
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
        var expectedTypeName = Enum.GetName(expected.Type);
        var receivedTypeName = Enum.GetName(received.Type);
        throw new LhsNotRhs(expected.Position, expectedTypeName, receivedTypeName);
      }
    }
  }
}
