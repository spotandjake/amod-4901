using System.Collections.Generic;
using System.Text;
using Decaf.Utils;

namespace Decaf.WasmBuilder {
  public abstract record WasmExpression(Position Position) {
    public Position Position { get; } = Position;
    public abstract string ToWat();
    // Wasm Expression subtypes
    public abstract record I32(Position Position) : WasmExpression(Position) {
      public sealed record Const(Position Position, int Value) : I32(Position) {
        public override string ToWat() => $"(i32.const {Value})";
      }
      // Comparison - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#comparison
      public sealed record Eqz(Position Position, WasmExpression Operand) : I32(Position) {
        public override string ToWat() => $"(i32.eqz {Operand.ToWat()})";
      }
      public sealed record Eq(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.eq {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Ne(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.ne {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record GtS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.gt_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record LtS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.lt_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record GeS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.ge_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record LeS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.le_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record GtU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.gt_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record LtU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.lt_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record GeU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.ge_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record LeU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.le_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      // Arithmetic - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#arithmetic
      public sealed record Add(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.add {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Sub(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.sub {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Mul(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.mul {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record DivS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.div_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record DivU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.div_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record RemU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.rem_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record RemS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.rem_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      // Bitwise - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#bitwise
      public sealed record And(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.and {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Or(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.or {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Xor(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.xor {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Shl(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.shl {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record ShrS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.shr_s {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record ShrU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.shr_u {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Rotl(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.rotl {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Rotr(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        public override string ToWat() => $"(i32.rotr {LHS.ToWat()} {RHS.ToWat()})";
      }
      public sealed record Clz(Position Position, WasmExpression LHS) : I32(Position) {
        public override string ToWat() => $"(i32.clz {LHS.ToWat()})";
      }
      public sealed record Ctz(Position Position, WasmExpression LHS) : I32(Position) {
        public override string ToWat() => $"(i32.ctz {LHS.ToWat()})";
      }
      public sealed record Popcnt(Position Position, WasmExpression LHS) : I32(Position) {
        public override string ToWat() => $"(i32.popcnt {LHS.ToWat()})";
      }
      // Load - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Memory/load
      public sealed record Load(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.load {Ptr.ToWat()} (offset {Offset.ToWat()}))";
      }
      public sealed record Load8S(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.load8_s {Ptr.ToWat()} (offset {Offset.ToWat()}))";
      }
      public sealed record Load8U(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.load8_u {Ptr.ToWat()} (offset {Offset.ToWat()}))";
      }
      public sealed record Load16S(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.load16_s {Ptr.ToWat()} (offset {Offset.ToWat()}))";
      }
      public sealed record Load16U(Position Position, WasmExpression Ptr, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.load16_u {Ptr.ToWat()} (offset {Offset.ToWat()}))";
      }
      // Store - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Memory/store
      public sealed record Store(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.store {Ptr.ToWat()} {Value.ToWat()} (offset {Offset.ToWat()}))";
      }
      public sealed record Store8(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.store8 {Ptr.ToWat()} {Value.ToWat()} (offset {Offset.ToWat()}))";
      }
      public sealed record Store16(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Offset) : I32(Position) {
        public override string ToWat() => $"(i32.store16 {Ptr.ToWat()} {Value.ToWat()} (offset {Offset.ToWat()}))";
      }
    }
    // Memory
    public abstract record Memory(Position Position) : WasmExpression(Position) {
      public sealed record Size(Position Position) : Memory(Position) {
        public override string ToWat() => $"(memory.size)";
      }
      public sealed record Grow(Position Position, WasmExpression PageCount) : Memory(Position) {
        public override string ToWat() => $"(memory.grow {PageCount.ToWat()})";
      }
      public sealed record Fill(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Length) : Memory(Position) {
        public override string ToWat() => $"(memory.fill {Ptr.ToWat()} {Value.ToWat()} {Length.ToWat()})";
      }
    }
    // Global - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global
    public abstract record Global(Position Position) : WasmExpression(Position) {
      // Get - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global.get
      public sealed record Get(Position Position, WasmLabel Name) : Global(Position) {
        public override string ToWat() => $"(global.get {Name.ToWat()})";
      }
      // Set - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global.set
      public sealed record Set(Position Position, WasmLabel Name, WasmExpression Value) : Global(Position) {
        public override string ToWat() => $"(global.set {Name.ToWat()} {Value.ToWat()})";
      }
    }
    // Local - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local
    public abstract record Local(Position Position) : WasmExpression(Position) {
      // Get - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.get
      public sealed record Get(Position Position, WasmLabel Name) : Local(Position) {
        public override string ToWat() => $"(local.get ${Name.ToWat()})";
      }
      // Set - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.set
      public sealed record Set(Position Position, WasmLabel Name, WasmExpression Value) : Local(Position) {
        public override string ToWat() => $"(local.set {Name.ToWat()} {Value.ToWat()})";
      }
      // Tee - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.tee
      public sealed record Tee(Position Position, WasmLabel Name, WasmExpression Value) : Local(Position) {
        public override string ToWat() => $"(local.tee {Name.ToWat()} {Value.ToWat()})";
      }
    }
    // Block - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/block
    public record Block(Position Position, WasmLabel Label, IEnumerable<WasmExpression> Expressions) : WasmExpression(Position) {
      public override string ToWat() {
        var sb = new StringBuilder();
        sb.Append($"(block {Label}");
        foreach (var expr in Expressions) {
          sb.AppendLine();
          sb.Append($"  {expr.ToWat()}");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Br - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br
    public sealed record Br(Position Position, WasmLabel Label) : WasmExpression(Position) {
      public override string ToWat() => $"(br {Label.ToWat()})";
    }
    // BrIf - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br_if
    public sealed record BrIf(Position Position, WasmLabel Label, WasmExpression Condition) : WasmExpression(Position) {
      public override string ToWat() => $"(br_if {Label.ToWat()} {Condition.ToWat()})";
    }
    // TODO: BrTable
    // Call - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/call
    public sealed record Call(Position Position, WasmLabel FunctionName, IEnumerable<WasmExpression> Arguments) : WasmExpression(Position) {
      public override string ToWat() {
        var sb = new StringBuilder();
        sb.Append($"(call {FunctionName.ToWat()}");
        foreach (var arg in Arguments) {
          sb.Append($" {arg.ToWat()}");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Drop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/drop
    public sealed record Drop(Position Position, WasmExpression Value) : WasmExpression(Position) {
      public override string ToWat() => $"(drop {Value.ToWat()})";
    }
    // If - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/if...else
    public sealed record If(
#nullable enable
      Position Position, WasmExpression Condition, WasmExpression TrueBranch, WasmExpression? FalseBranch
#nullable disable
    ) : WasmExpression(Position) {
      public override string ToWat() {
        var sb = new StringBuilder();
        sb.Append($"(if {Condition.ToWat()}");
        sb.AppendLine();
        sb.Append($"  (then {TrueBranch.ToWat()})");
        if (FalseBranch != null) {
          sb.AppendLine();
          sb.Append($"  (else {FalseBranch.ToWat()})");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Loop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/loop
    public sealed record Loop(Position Position, WasmLabel Label, IEnumerable<WasmExpression> Body) : WasmExpression(Position) {
      public override string ToWat() {
        var sb = new StringBuilder();
        sb.Append($"(loop {Label.ToWat()}");
        foreach (var expr in Body) {
          sb.AppendLine();
          sb.Append($"  {expr.ToWat()}");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Nop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/nop
    public sealed record Nop(Position Position) : WasmExpression(Position) {
      public override string ToWat() => "(nop)";
    }
    // Return - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/return
#nullable enable
    public sealed record Return(Position Position, WasmExpression? Value) : WasmExpression(Position) {
      public override string ToWat() => $"(return {Value?.ToWat() ?? string.Empty})";
    }
#nullable disable
    // TODO: Select
    // Unreachable - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/unreachable
    public sealed record Unreachable(Position Position) : WasmExpression(Position) {
      public override string ToWat() => "(unreachable)";
    }
  }
}
