using System;
using System.Collections.Generic;
using Decaf.Utils;

namespace Decaf.WasmBuilder {
  // A function in the wasm module.
  public record WasmFunction(
    Position Position,
    WasmLabel Label,
    Dictionary<WasmLabel, WasmType> Params,
    List<WasmType> Results,
    Dictionary<WasmLabel, WasmType> Locals,
    WasmExpression[] Body
  ) {
    internal string ToWat(WasmBuildCtx ctx) {
      // Compile the function signature
      var paramSB = new System.Text.StringBuilder();
      foreach (var param in this.Params) {
        paramSB.Append($"(param {param.Key.ToWat(ctx)} {param.Value.ToWat(ctx)}) ");
      }
      // Compile the return
      var resultSB = new System.Text.StringBuilder();
      foreach (var result in this.Results) {
        resultSB.Append($"(result {result.ToWat(ctx)}) ");
      }
      // Compile the locals
      var localSB = new System.Text.StringBuilder();
      foreach (var local in this.Locals) {
        localSB.Append($"(local {local.Key.ToWat(ctx)} {local.Value.ToWat(ctx)}) ");
      }
      // Compile the body
      var bodyStr = new System.Text.StringBuilder();
      foreach (var expr in this.Body) {
        bodyStr.Append(expr.ToWat(ctx));
      }
      // Form the function
      return $"(func {this.Label.ToWat(ctx)} {paramSB.ToString()} {resultSB.ToString()} {localSB.ToString()} {bodyStr.ToString()})";
    }
  }
}
