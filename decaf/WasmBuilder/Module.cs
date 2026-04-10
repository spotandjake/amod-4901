using System;
using System.Collections.Concurrent;
using System.Text;
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
  // The main module
  public record WasmModule(Position Position) {
    // Section Data
    private ConcurrentDictionary<int, WasmGlobal> Globals { get; } = new ConcurrentDictionary<int, WasmGlobal>();
    // Public API
    public void AddGlobal(WasmGlobal global) {
      // TODO: It would be nice if we had this return some sort of globalref that we could use to refer to the global later on
      // TODO: Is the hash code a reasonable key for this? (Does it capture uniqueness well enough?)
      if (!Globals.TryAdd(global.Label.GetHashCode(), global)) {
        // TODO: I don't like this error handling
        throw new Exception($"Global with label {global.Label.ToWat(new WasmBuildCtx())} already exists in module");
      }
    }
    // Output API
    public string ToWat() {
      var ctx = new WasmBuildCtx();
      // Compile the global section
      var globalSection = new StringBuilder();
      foreach (var global in Globals.Values) {
        globalSection.AppendLine(global.ToWat(ctx));
      }
      // Package the entire module
      return $"(module {globalSection})";
    }
  }
}
