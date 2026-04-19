using System;
using System.Collections.Concurrent;
using System.Linq;
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
    private ConcurrentDictionary<WasmLabel, WasmType.Func> WasmTypes { get; } = new ConcurrentDictionary<WasmLabel, WasmType.Func>();
    private ConcurrentQueue<WasmLabel> WasmTypeOrder { get; } = new ConcurrentQueue<WasmLabel>();
    private ConcurrentDictionary<WasmLabel, WasmImport> Imports { get; } = new ConcurrentDictionary<WasmLabel, WasmImport>();
    private ConcurrentQueue<WasmLabel> ImportOrder { get; } = new ConcurrentQueue<WasmLabel>();
    private ConcurrentDictionary<WasmLabel, WasmMemory> Memories { get; } = new ConcurrentDictionary<WasmLabel, WasmMemory>();
    private ConcurrentQueue<WasmLabel> MemoryOrder { get; } = new ConcurrentQueue<WasmLabel>();
    private ConcurrentDictionary<WasmLabel, WasmDataSegment> DataSegments { get; } = new ConcurrentDictionary<WasmLabel, WasmDataSegment>();
    private ConcurrentQueue<WasmLabel> DataSegmentOrder { get; } = new ConcurrentQueue<WasmLabel>();
    private ConcurrentDictionary<int, WasmGlobal> Globals { get; } = new ConcurrentDictionary<int, WasmGlobal>();
    private ConcurrentQueue<int> GlobalOrder { get; } = new ConcurrentQueue<int>();
    private ConcurrentDictionary<int, WasmFunction> Functions { get; } = new ConcurrentDictionary<int, WasmFunction>();
    private ConcurrentQueue<int> FunctionOrder { get; } = new ConcurrentQueue<int>();
#nullable enable
    private WasmLabel? StartFunction { get; set; } = null;
#nullable restore
    // Public API
    // TODO: This should return a reference that can be used to refer to the type later on
    public void AddWasmType(WasmLabel name, WasmType type) {
      if (type is not WasmType.Func funcType) {
        throw new Exception("Only function types are currently supported in the module");
      }
      if (!WasmTypes.TryAdd(name, funcType)) {
        // TODO: I don't like this error handling
        throw new Exception($"Type {name.ToWat(new WasmBuildCtx())} already exists in module");
      }
      else {
        // We also need to keep track of the order of types for the output since the order matters in wasm
        WasmTypeOrder.Enqueue(name);
      }
    }
    public void AddImport(WasmImport import) {
      if (!Imports.TryAdd(import.Label, import)) {
        throw new Exception($"Import {import.Label.ToWat(new WasmBuildCtx())} already exists in module");
      }
      else {
        ImportOrder.Enqueue(import.Label);
      }
    }
    public void AddMemory(WasmMemory memory) {
      if (!Memories.TryAdd(memory.Label, memory)) {
        throw new Exception($"Memory {memory.Label.ToWat(new WasmBuildCtx())} already exists in module");
      }
      else {
        MemoryOrder.Enqueue(memory.Label);
      }
    }
    public void AddDataSegment(WasmDataSegment dataSegment) {
      if (!DataSegments.TryAdd(dataSegment.Label, dataSegment)) {
        throw new Exception($"Data segment {dataSegment.Label.ToWat(new WasmBuildCtx())} already exists in module");
      }
      else {
        DataSegmentOrder.Enqueue(dataSegment.Label);
      }
    }
    public void AddGlobal(WasmGlobal global) {
      // TODO: It would be nice if we had this return some sort of globalref that we could use to refer to the global later on
      // TODO: Is the hash code a reasonable key for this? (Does it capture uniqueness well enough?)
      var id = global.Label.GetHashCode();
      if (!Globals.TryAdd(id, global)) {
        // TODO: I don't like this error handling
        throw new Exception($"Global with label {global.Label.ToWat(new WasmBuildCtx())} already exists in module");
      }
      else {
        // We also need to keep track of the order of globals for the output since the order matters in wasm
        GlobalOrder.Enqueue(id);
      }
    }
    public void AddFunction(WasmFunction func) {
      // TODO: Is the hash code a reasonable key for this? (Does it capture uniqueness well enough?)
      var id = func.Label.GetHashCode();
      if (!Functions.TryAdd(id, func)) {
        // TODO: I don't like this error handling
        throw new Exception($"Function with label {func.Label.ToWat(new WasmBuildCtx())} already exists in module");
      }
      else {
        // We also need to keep track of the order of functions for the output since the order matters in wasm
        FunctionOrder.Enqueue(id);
      }
      // TODO: Should this add the type to the module????
      // TODO: This should possibly return a funcref that we can use to refer to the function later on????
    }
    public void SetStartFunction(WasmLabel label) {
      this.StartFunction = label;
    }
    // Output API
    public string ToWat() {
      var ctx = new WasmBuildCtx();
      // Compile the type section
      var typeSection = new StringBuilder();
      foreach (var typeID in this.WasmTypeOrder) {
        var type = this.WasmTypes[typeID];
        typeSection.AppendLine($"(type {typeID.ToWat(ctx)} {type.ToWat(ctx)})");
      }
      // Compile the import section
      var importSection = new StringBuilder();
      foreach (var importID in this.ImportOrder) {
        var import = this.Imports[importID];
        importSection.AppendLine(import.ToWat(ctx));
      }
      // Compile the memory section
      var memorySection = new StringBuilder();
      foreach (var memoryID in this.MemoryOrder) {
        var memory = this.Memories[memoryID];
        memorySection.AppendLine(memory.ToWat(ctx));
      }
      // Compile an element section for the function references
      // TODO: This should probably be done externally like the other things
      var elementSection = new StringBuilder();
      if (this.FunctionOrder.Count > 0) {
        elementSection.AppendLine("(elem declare funcref");
        // From the import section
        foreach (var importID in this.ImportOrder) {
          var import = this.Imports[importID];
          if (import.Type is WasmType.Func) {
            elementSection.AppendLine($"  (ref.func {import.Label.ToWat(ctx)})");
          }
        }
        // From the function section
        foreach (var funcID in this.FunctionOrder) {
          var func = this.Functions[funcID];
          elementSection.AppendLine($"  (ref.func {func.Label.ToWat(ctx)})");
        }
        elementSection.AppendLine(")");
      }
      // Compile the data segments
      var dataSegmentSection = new StringBuilder();
      foreach (var dataSegmentID in this.DataSegmentOrder) {
        var dataSegment = this.DataSegments[dataSegmentID];
        dataSegmentSection.AppendLine(dataSegment.ToWat(ctx));
      }
      // Compile the global section
      var globalSection = new StringBuilder();
      foreach (var globalID in this.GlobalOrder) {
        var global = this.Globals[globalID];
        globalSection.AppendLine(global.ToWat(ctx));
      }
      // Compile the function section
      var functionSection = new StringBuilder();
      foreach (var funcID in this.FunctionOrder) {
        var func = this.Functions[funcID];
        functionSection.AppendLine(func.ToWat(ctx));
      }
      // TODO: We should handle exports properly but this is just for an experiment
      var memoryExportStr = "(export \"memory\" (memory 0))";
      var startExport = "(export \"_start\" (func $_start))";
      // Compile the start section if it exists
      var startSection = this.StartFunction != null ? $"(start {this.StartFunction.ToWat(ctx)})" : "";
      // Package the entire module
      return $"(module{typeSection}{importSection}{memorySection}{memoryExportStr}{dataSegmentSection}{elementSection}{globalSection}{functionSection}{startExport}{startSection})";
    }
  }
}
