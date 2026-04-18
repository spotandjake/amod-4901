using Decaf.Utils;

namespace Decaf.WasmBuilder {
  // A global variable in the module.
  public record WasmGlobal(
    Position Position,
    WasmLabel Label,
    WasmType Type,
    // TODO: Is this meant to be on the wasm type???
    bool IsMutable,
#nullable enable
    WasmExpression? Init
#nullable disable
  ) {
    internal string ToWat(WasmBuildCtx ctx) {
      var labelStr = Label.ToWat(ctx);
      var mutStr = IsMutable ? "mut" : "";
      var typeStr = Type.ToWat(ctx);
      var initStr = Init != null ? Init.ToWat(ctx) : "";
      return $"(global {labelStr} ({mutStr} {typeStr}) {initStr})";
    }
  }
}
