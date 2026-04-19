using System.Linq;

using Decaf.Utils;

namespace Decaf.WasmBuilder {
  // An import in the module.
  public record WasmImport(
    Position Position,
    WasmLabel Label,
    string Module,
    string Name,
    WasmType Type
  ) {
    internal string ToWat(WasmBuildCtx ctx) {
      if (Type is WasmType.I32 || Type is WasmType.I64 || Type is WasmType.F32 || Type is WasmType.F64) {
        // Emit a global import
        return $"(import \"{Module}\" \"{Name}\" (global {Type.ToWat(ctx)}))";
      }
      else if (Type is WasmType.Func funcType) {
        // Emit a function import
        var paramStr = string.Join(" ", funcType.ParamTypes.Select(t => $"(param {t.ToWat(ctx)})"));
        var returnStr = string.Join(" ", funcType.ReturnTypes.Select(t => $"(result {t.ToWat(ctx)})"));
        return $"(import \"{Module}\" \"{Name}\" (func {Label.ToWat(ctx)} {paramStr} {returnStr}))";
      }
      else {
        throw new System.Exception($"Unsupported import type: {Type}");
      }
    }
  }
}
