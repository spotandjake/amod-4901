using System.Collections.Generic;
using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public record WasmModule(Position Position) {
    // A unique counter used to generate unique symbols across the module. (This is a requirement for getting good wasm-opt performance)
    // private int _counter = 0;
    // public string UniqueSymbol(string name) => name + Interlocked.Increment(ref _counter).ToString();
  }
}
