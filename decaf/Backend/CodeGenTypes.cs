using System;
using System.Linq;
using System.Collections.Generic;

using Signature = Decaf.IR.Signature;
using Decaf.WasmBuilder;
using Decaf.IR.AnfTree;
using System.Reflection.Emit;

namespace Decaf.Backend {
  public static partial class Codegen {
    private static WasmType GetWasmFunctionTypeFromSignature(CodegenContext ctx, Signature.Signature.FunctionSig signature, bool returnRef) {
      // Handle parameters
      var paramTypes = new List<WasmType>();
      foreach (var paramType in signature.ParameterTypes) {
        paramTypes.Add(GetWasmTypeFromSignature(ctx, paramType));
      }
      // Handle return type
      var returnTypes = new List<WasmType>();
      if (signature.ReturnType is not Signature.Signature.PrimitiveSig { Type: Signature.PrimitiveType.Void }) {
        returnTypes.Add(GetWasmTypeFromSignature(ctx, signature.ReturnType));
      }
      // Build the type label
      var label = new WasmLabel.Label(signature.Position, $"type{ctx.WasmTypes.Count}");
      // Build the function type
      var wasmType = new WasmType.Func(signature.Position, paramTypes, returnTypes);
      if (!ctx.WasmTypes.ContainsKey(signature)) {
        ctx.WasmModule.AddWasmType(label, wasmType);
        // Add the type to the context
        ctx.WasmTypes.Add(signature, label);
      }
      if (returnRef) return new WasmType.FuncRef(signature.Position, ctx.WasmTypes[signature]);
      else return wasmType;
    }
    private static WasmType GetWasmTypeFromSignature(CodegenContext ctx, Signature.Signature signature, bool returnRef = true) {
      return signature switch {
        // This is an i32 because it is a pointer
        Signature.Signature.ArraySig => new WasmType.I32(signature.Position),
        Signature.Signature.FunctionSig funcSig => GetWasmFunctionTypeFromSignature(ctx, funcSig, returnRef),
        Signature.Signature.PrimitiveSig primitiveSig => primitiveSig.Type switch {
          Signature.PrimitiveType.Int => new WasmType.I32(signature.Position),
          Signature.PrimitiveType.Boolean => new WasmType.I32(signature.Position),
          Signature.PrimitiveType.Character => new WasmType.I32(signature.Position),
          Signature.PrimitiveType.String => new WasmType.I32(signature.Position),
          // Void has no wasm representation
          Signature.PrimitiveType.Void =>
          throw new Exception("Void type cannot be used as a value and therefore does not have a wasm type"),
          // NOTE: This should never happen, if it does we forgot to update this method when we added a new primitive type
          _ => throw new Exception("Unknown primitive type")
        },
        // NOTE: This should never happen as modules are not first class
        Signature.Signature.ModuleSig => throw new Exception("Modules cannot be used as values and therefore do not have a wasm type"),
        // Unknown signature
        _ => throw new Exception($"Unknown signature node kind: {signature.GetType()}"),
      };
    }
    private static WasmExpression GetDefaultValueFromSignature(CodegenContext ctx, Signature.Signature signature) {
      return signature switch {
        // This is an i32 because it is a pointer, the default value for a pointer is 0 (null)
        Signature.Signature.ArraySig => new WasmExpression.I32.Const(signature.Position, 0),
        Signature.Signature.FunctionSig funcSig =>
          new WasmExpression.Ref.Null(signature.Position, GetWasmFunctionTypeFromSignature(ctx, funcSig, returnRef: true)),
        // For primitive types we can return the default value for that type
        Signature.Signature.PrimitiveSig primitiveSig => primitiveSig.Type switch {
          Signature.PrimitiveType.Int => new WasmExpression.I32.Const(signature.Position, 0),
          Signature.PrimitiveType.Boolean => new WasmExpression.I32.Const(signature.Position, 0),
          Signature.PrimitiveType.Character => new WasmExpression.I32.Const(signature.Position, 0),
          Signature.PrimitiveType.String => new WasmExpression.I32.Const(signature.Position, 0),
          // Void has no wasm representation
          Signature.PrimitiveType.Void =>
          throw new Exception("Void type cannot be used as a value and therefore does not have a default value"),
          // NOTE: This should never happen, if it does we forgot to update this method when we added a new primitive type
          _ => throw new Exception("Unknown primitive type")
        },
        // NOTE: This should never happen as modules are not first class
        Signature.Signature.ModuleSig =>
          throw new Exception("Modules cannot be used as values and therefore do not have a default value"),
        // Unknown signature
        _ => throw new Exception($"Unknown signature node kind: {signature.GetType()}"),
      };
    }
  }
}
