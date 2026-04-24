// This file contains the type definition for primitive callouts in the language
namespace Decaf.IR.PrimitiveDefinition {
  public enum PrimDefinition {
    // General purpose primitives
    GetPointer,
    // --- @wasm namespace ---
    Unreachable,
    // memory sub namespace
    WasmMemorySize,
    WasmMemoryGrow,
    WasmMemoryFill,
    WasmMemoryCopy,
    // I32 sub namespace
    WasmI32Store,
    WasmI32Store8,
    WasmI32Store16,
    WasmI32Load,
    WasmI32Load8S,
    WasmI32Load8U,
    WasmI32Load16S,
    WasmI32Load16U,
    WasmI32RemS,
    WasmI32RemU,
    // --- @cast namespace ---
    CastPtrToString,
  }
}
