// This file contains the type definition for primitive callouts in the language
namespace Decaf.IR.PrimitiveDefinition {
  public enum PrimDefinition {
    // --- @wasm namespace ---
    // memory sub namespace
    WasmMemorySize,
    WasmMemoryGrow,
    WasmMemoryFill,
    // I32 sub namespace
    WasmI32Store
  }
}
