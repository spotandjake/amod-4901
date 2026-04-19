using Decaf.Utils;

namespace Decaf.WasmBuilder {
  // A memory in wasm
  public record WasmMemory(
    Position Position,
    WasmLabel Label,
    uint InitialPages,
    uint? MaxPages = null
  ) {
    internal string ToWat(WasmBuildCtx ctx) {
      var maxStr = MaxPages.HasValue ? $" {MaxPages.Value}" : "";
      return $"(memory {Label.ToWat(ctx)} {maxStr} {InitialPages})";
    }
  }
}
