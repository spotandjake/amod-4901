using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmLabel(Position Position) {
    // TODO: The purpose of wasm labels is to be able to deterministically refer to things like functions, globals, locals, blocks, ..etc.
    // TODO: The idea is that we can generate code in any order and in parrellel, and when we are ready to convert our wasm tree to wat or wasm binary, we can generate labels, unique labels should be unique across the entire module (however it's best if they are also as deterministic as possible so we will likely give each function in the module a default range of 0 to 100, and then if we need more than 100 unique labels we can take from a global pool of unqique labels this allows us to generate labels in parrellel across functions, while keeping them deterministic (preventing shifts) across the entire module), regular labels just need to resolve to the same name / index whenever they are used however it's worth noting that labels themselves are not global, if the label refers to a local it's only accessible within the function scope.
    internal abstract string ToWat(WasmBuildCtx ctx);
    public sealed record Label(Position Position, string Name) : WasmLabel(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => $"${Name}";
    }
    public sealed record UniqueLabel(Position Position, string Name) : WasmLabel(Position) {
      private int? _uniqueID;
      private string GetUniqueName(WasmBuildCtx ctx, string baseName) {
        // Generate a unique ID for this label if we haven't already
        this._uniqueID ??= ctx.GetUniqueID();
        return $"{baseName}@{this._uniqueID}";
      }
      internal override string ToWat(WasmBuildCtx ctx) => $"${GetUniqueName(ctx, Name)}";
    }
  }
}
