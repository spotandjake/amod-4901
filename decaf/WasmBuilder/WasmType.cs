using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmType(Position Position) {
    internal abstract string ToWat(WasmBuildCtx ctx);
    public record I32(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "i32";
    }
    public record I64(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "i64";
    }
    public record F32(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "f32";
    }
    public record F64(Position Position) : WasmType(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "f64";
    }
  }
}
