// This file contains the type definition for primitive callouts in the language
using Decaf.IR.TypedTree;
using Decaf.Utils;

namespace Decaf.MiddleEnd.TypeChecker {
  public static class PrimitiveTypes {
    public static Signature GetPrimitiveCallSignature(string name, Position position) {
      return name switch {
        _ => throw new System.Exception($"Unknown primitive callout: {name}")
      };
    }
  }
}

