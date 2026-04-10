using System.Collections.Generic;
using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmExpression(Position Position) {
    public Position Position { get; } = Position;
    // Wasm Expression subtypes
    public record I32(Position Position) : WasmExpression(Position) {
      public sealed record Const(Position Position, int Value) : I32(Position);
      // Comparison - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#comparison
      public sealed record Eqz(Position Position, WasmExpression Operand) : I32(Position);
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
      // Load - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Memory/load
      public sealed record Load(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position);
      public sealed record Load8S(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position);
      public sealed record Load8U(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position);
      public sealed record Load16S(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position);
      public sealed record Load16U(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position);
      // Store - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Memory/store
      public sealed record Store(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Offset) : I32(Position);
      public sealed record Store8(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Offset) : I32(Position);
      public sealed record Store16(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Offset) : I32(Position);
    }
    // Memory
    public record Memory(Position Position) : WasmExpression(Position) {
      public sealed record Size(Position Position) : Memory(Position);
      public sealed record Grow(Position Position, WasmExpression PageCount) : Memory(Position);
      public sealed record Fill(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Length) : Memory(Position);
    }
    // Block - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/block
    public record Block(Position Position, WasmLabel Label, IEnumerable<WasmExpression> Expressions) : WasmExpression(Position);
    // Br - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br
    public sealed record Br(Position Position, WasmLabel Label) : WasmExpression(Position);
    // BrIf - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br_if
    public sealed record BrIf(Position Position, WasmLabel Label, WasmExpression Condition) : WasmExpression(Position);
    // TODO: BrTable
    // Call - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/call
    public sealed record Call(Position Position, WasmLabel FunctionName, IEnumerable<WasmExpression> Arguments) : WasmExpression(Position);
    // Drop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/drop
    public sealed record Drop(Position Position, WasmExpression Value) : WasmExpression(Position);
    // If - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/if...else
    public sealed record If(
#nullable enable
      Position Position, WasmExpression Condition, WasmExpression TrueBranch, WasmExpression? FalseBranch
#nullable disable
    ) : WasmExpression(Position);
    // Loop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/loop
    public sealed record Loop(Position Position, WasmLabel Label, IEnumerable<WasmExpression> Body) : WasmExpression(Position);
    // Nop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/nop
    public sealed record Nop(Position Position) : WasmExpression(Position);
    // Return - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/return
#nullable enable
    public sealed record Return(Position Position, WasmExpression? Value) : WasmExpression(Position);
#nullable disable
    // TODO: Select
    // Unreachable - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/unreachable
    public sealed record Unreachable(Position Position) : WasmExpression(Position);
  }
}
