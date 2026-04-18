using System.Collections.Generic;

using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmType(Position Position) {
    internal abstract string ToWat(WasmBuildCtx ctx);
    public sealed record I32(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "i32";
    }
    public sealed record I64(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "i64";
    }
    public sealed record F32(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "f32";
    }
    public sealed record F64(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "f64";
    }
    public sealed record FuncRef(Position Position, WasmLabel Label) : WasmType(Position) {
      // TODO: Figure out if this fits into the hierarchy
      internal override string ToWat(WasmBuildCtx ctx) => $"(ref null {Label.ToWat(ctx)})";
    }
    public sealed record Func(Position Position, List<WasmType> ParamTypes, List<WasmType> ReturnTypes) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) {
        // TODO: This could have two different formats depending on if this is being used in a type decl or type use
        var sb = new System.Text.StringBuilder();
        // Handle Parameters
        foreach (var paramType in ParamTypes) {
          sb.Append($"(param {paramType.ToWat(ctx)}) ");
        }
        // Handle Return Types
        foreach (var returnType in ReturnTypes) {
          sb.Append($"(result {returnType.ToWat(ctx)}) ");
        }
        return $"(func {sb.ToString().TrimEnd()})";
      }
    }
  }
}
