using System.Threading;
using Decaf.Utils;

namespace Decaf.WasmBuilder {
  // A context used when building a module.
  internal record WasmBuildCtx {
    // Unique Label Counter - used to generate unique labels across the module.
    // NOTE: (This is a requirement for getting good wasm-opt performance)
    private int _uniqueIDCounter = 0;
    public int GetUniqueID() => Interlocked.Increment(ref _uniqueIDCounter);
  }
  public record WasmModule(Position Position) {
  }
}
