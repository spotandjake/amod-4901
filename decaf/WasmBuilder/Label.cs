using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmLabel(Position Position) {
    public abstract string ToWat();
    public sealed record Label(Position Position, string Name) : WasmLabel(Position) {
      public override string ToWat() => $"${Name}";
    }
    public sealed record UniqueLabel(Position Position, string Name) : WasmLabel(Position) {
      // TODO: This ToWat is going to need to take a ctx to make it unique
      public override string ToWat() => $"${Name}";
    }
  }
}
