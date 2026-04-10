using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmExpression(Position Position) {
    public Position Position { get; } = Position;
    // Wasm Expression subtypes
    public record I32(Position Position) : WasmExpression(Position) {
      public sealed record Const(Position Position, int Value) : I32(Position);
      // Comparison - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#comparison
      public sealed record Eq(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Ne(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record GtS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record LtS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record GeS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record LeS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record GtU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record LtU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record GeU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record LeU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      // Arithmetic - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#arithmetic
      public sealed record Add(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Sub(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Mul(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record DivS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record DivU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record RemU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record RemS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      // Bitwise - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#bitwise
      public sealed record And(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Or(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Xor(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Shl(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record ShrS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record ShrU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Rotl(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Rotr(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position);
      public sealed record Clz(Position Position, WasmExpression LHS) : I32(Position);
      public sealed record Ctz(Position Position, WasmExpression LHS) : I32(Position);
      public sealed record Popcnt(Position Position, WasmExpression LHS) : I32(Position);
    }
  }
}
