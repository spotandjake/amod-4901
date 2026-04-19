using System.Linq;
using Decaf.Utils;

namespace Decaf.WasmBuilder {
  // A wasm data segment, which is used to initialize linear memory with data.
  public record WasmDataSegment(
    Position Position,
    WasmLabel Label,
    byte[] Data
  ) {
    internal string ToWat(WasmBuildCtx ctx) {
      var labelStr = Label.ToWat(ctx);
      var dataStr = string.Join("", Data.Select(b => $"\\{b:X2}"));
      return $"(data {labelStr} \"{dataStr}\")";
    }
  }
}
