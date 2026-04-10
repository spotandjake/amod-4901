using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmLabel(Position Position) {
    internal abstract string ToWat(WasmBuildCtx ctx);
    public sealed record Label(Position Position, string Name) : WasmLabel(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => $"${Name}";
    }
    public sealed record UniqueLabel(Position Position, string Name) : WasmLabel(Position) {
      // TODO: This ToWat is going to need to take a ctx to make it unique
      private int? _uniqueID;
      private string GetUniqueName(WasmBuildCtx ctx, string baseName) {
        // Generate a unique ID for this label if we haven't already
        this._uniqueID ??= ctx.GetUniqueID();
        return $"{baseName}#{this._uniqueID}";
      }
      internal override string ToWat(WasmBuildCtx ctx) => $"${GetUniqueName(ctx, Name)}";
    }
  }
}
