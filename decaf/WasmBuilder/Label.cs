using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmLabel(Position Position) {
    public sealed record Label(Position Position, string Name) : WasmLabel(Position);
    public sealed record UniqueLabel(Position Position, string Name) : WasmLabel(Position);
  }
}
