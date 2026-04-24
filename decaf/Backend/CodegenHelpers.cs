namespace Decaf.Backend {
  using Decaf.Utils;
  using Decaf.WasmBuilder;

  public static partial class Codegen {
    // --- Array Compilation Helpers ---
    private static WasmExpression.I32.Add CompileArrayByteIndex(
      Position position,
      WasmExpression index
    ) {
      // Each element is 4 bytes (i32)
      return new WasmExpression.I32.Add(
        position,
        new WasmExpression.I32.Mul(
          position,
          index,
          new WasmExpression.I32.Const(position, 4)
        ),
        new WasmExpression.I32.Const(position, 4) // add 4 to skip the length field at the start of the array
      );
    }
    private static WasmExpression.If CompileArrayBoundsCheck(
      Position position,
      WasmExpression pointer,
      WasmExpression index
    ) {
      // Load the length from the start of the array
      var compiledLength = new WasmExpression.I32.Load(
        position,
        pointer,
        0 // length is at offset 0
      );
      // Compare the index to the length
      var lowerCheck = new WasmExpression.I32.LtS(position, index, new WasmExpression.I32.Const(position, 0));
      var upperCheck = new WasmExpression.I32.GeS(position, index, compiledLength);
      var check = new WasmExpression.I32.Or(position, lowerCheck, upperCheck);
      // If in bounds, continue with the original expression, otherwise execute the alternative (which should be a trap)
      return new WasmExpression.If(
        position,
        check,
        // If the index is out of bounds, we currently just throw using `unreachable`
        // TODO: Use a proper wasm exception
        new WasmExpression.Unreachable(position),
        null
      );
    }
  }
}
