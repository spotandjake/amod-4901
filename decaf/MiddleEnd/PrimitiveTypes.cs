// This file contains the type definition for primitive callouts in the language
using Decaf.IR.PrimitiveDefinition;
using Decaf.IR.TypedTree;
using Decaf.Utils;
using Errors = Decaf.Utils.Errors.TypeCheckingErrors;

// NOTE: Any primitive added to the typechecker also must be defined in the code generator.
namespace Decaf.MiddleEnd.TypeChecker {
  // A type alias used for the return of a primitive resolution
  using PrimResolution = (Signature, PrimDefinition);
  public static class PrimitiveTypes {
    // The main resolver
    public static PrimResolution GetPrimitiveCallSignature(string name, Position position) {
      var path = name.Split('.');
      return path switch {
      // @wasm namespace
      ["@wasm", .. var rest] => GetWasmPrimitiveCallSignature(name, rest ?? [], position),
        // Unknown
        _ => throw new Errors.UnknownPrimitiveCall(position, name)
      };
    }
    // A resolver for @wasm primitive calls
    private static PrimResolution GetWasmPrimitiveCallSignature(string name, string[] path, Position position) {
      return path switch {
      // Memory namespace
      ["memory", .. var subPath] => subPath switch {
      // () => int
      ["size"] =>
        (new Signature.MethodSignature(
          position,
          new Signature.PrimitiveSignature(position, PrimitiveType.Int),
          []
        ), PrimDefinition.WasmMemorySize),
        // (pageCount: int) => int
        ["grow"] =>
              (new Signature.MethodSignature(
                position,
                new Signature.PrimitiveSignature(position, PrimitiveType.Int),
                [new Signature.PrimitiveSignature(position, PrimitiveType.Int)]
              ), PrimDefinition.WasmMemoryGrow),
              // (pointer: int, value: int, byteCount: int) => int
              ["fill"] =>
              (new Signature.MethodSignature(
                position,
                new Signature.PrimitiveSignature(position, PrimitiveType.Int),
                [
                  new Signature.PrimitiveSignature(position, PrimitiveType.Int),
                new Signature.PrimitiveSignature(position, PrimitiveType.Int),
                new Signature.PrimitiveSignature(position, PrimitiveType.Int)
                ]
              ), PrimDefinition.WasmMemoryFill),
        // Unknown
        _ => throw new Errors.UnknownPrimitiveCall(position, name)
      },
        // I32 namespace
        // Unknown
        _ => throw new Errors.UnknownPrimitiveCall(position, name)
      };
    }
  }
}

