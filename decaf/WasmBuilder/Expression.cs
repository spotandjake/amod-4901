namespace Decaf.WasmBuilder {
  using System.Collections.Generic;
  using System.Text;
  using Decaf.Utils;


  public abstract record WasmExpression(Position Position) {
    public Position Position { get; } = Position;
    public string ToWat() => ToWat(new WasmBuildCtx());
    internal abstract string ToWat(WasmBuildCtx ctx);
    // Wasm Expression subtypes
    public abstract record I32(Position Position) : WasmExpression(Position) {
      public sealed record Const(Position Position, int Value) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.const {Value})";
      }
      // Comparison - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#comparison
      public sealed record Eqz(Position Position, WasmExpression Operand) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.eqz {Operand.ToWat(ctx)})";
      }
      public sealed record Eq(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.eq {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Ne(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.ne {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record GtS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.gt_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record LtS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.lt_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record GeS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.ge_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record LeS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.le_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record GtU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.gt_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record LtU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.lt_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record GeU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.ge_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record LeU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.le_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      // Arithmetic - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#arithmetic
      public sealed record Add(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.add {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Sub(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.sub {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Mul(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.mul {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record DivS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.div_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record DivU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.div_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record RemU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.rem_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record RemS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.rem_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      // Bitwise - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Numeric#bitwise
      public sealed record And(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.and {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Or(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.or {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Xor(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.xor {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Shl(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.shl {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record ShrS(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.shr_s {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record ShrU(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.shr_u {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Rotl(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.rotl {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Rotr(Position Position, WasmExpression LHS, WasmExpression RHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.rotr {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      public sealed record Clz(Position Position, WasmExpression LHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.clz {LHS.ToWat(ctx)})";
      }
      public sealed record Ctz(Position Position, WasmExpression LHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.ctz {LHS.ToWat(ctx)})";
      }
      public sealed record Popcnt(Position Position, WasmExpression LHS) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(i32.popcnt {LHS.ToWat(ctx)})";
      }
      // Load - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Memory/load
      public sealed record Load(Position Position, WasmExpression Ptr, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.load {offset} {Ptr.ToWat(ctx)})";
        }
      }
      public sealed record Load8S(Position Position, WasmExpression Ptr, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.load8_s {offset} {Ptr.ToWat(ctx)})";
        }
      }
      public sealed record Load8U(Position Position, WasmExpression Ptr, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.load8_u {offset} {Ptr.ToWat(ctx)})";
        }
      }
      public sealed record Load16S(Position Position, WasmExpression Ptr, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.load16_s {offset} {Ptr.ToWat(ctx)})";
        }
      }
      public sealed record Load16U(Position Position, WasmExpression Ptr, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.load16_u {offset} {Ptr.ToWat(ctx)})";
        }
      }
      // Store - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Memory/store
      public sealed record Store(Position Position, WasmExpression Ptr, WasmExpression Value, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.store {offset} {Ptr.ToWat(ctx)} {Value.ToWat(ctx)})";
        }
      }
      public sealed record Store8(Position Position, WasmExpression Ptr, WasmExpression Value, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.store8 {offset} {Ptr.ToWat(ctx)} {Value.ToWat(ctx)})";
        }
      }
      public sealed record Store16(Position Position, WasmExpression Ptr, WasmExpression Value, int? Offset) : I32(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          var offset = Offset != null ? $"offset={Offset}" : "";
          return $"(i32.store16 {offset} {Ptr.ToWat(ctx)} {Value.ToWat(ctx)})";
        }
      }
    }
    // Memory
    public abstract record Memory(Position Position) : WasmExpression(Position) {
      public sealed record Size(Position Position) : Memory(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(memory.size)";
      }
      public sealed record Grow(Position Position, WasmExpression PageCount) : Memory(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(memory.grow {PageCount.ToWat(ctx)})";
      }
      public sealed record Fill(Position Position, WasmExpression Ptr, WasmExpression Value, WasmExpression Length) : Memory(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(memory.fill {Ptr.ToWat(ctx)} {Value.ToWat(ctx)} {Length.ToWat(ctx)})";
      }
      public sealed record Copy(Position Position, WasmExpression DestPtr, WasmExpression SrcPtr, WasmExpression Length) : Memory(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(memory.copy {DestPtr.ToWat(ctx)} {SrcPtr.ToWat(ctx)} {Length.ToWat(ctx)})";
      }
      public sealed record Init(
        Position Position, WasmLabel DataSegment, WasmExpression DestPtr, WasmExpression SrcOffset, WasmExpression Length
      ) : Memory(Position) {
        internal override string ToWat(WasmBuildCtx ctx) =>
          $"(memory.init {DataSegment.ToWat(ctx)} {DestPtr.ToWat(ctx)} {SrcOffset.ToWat(ctx)} {Length.ToWat(ctx)})";
      }
    }
    // Global - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global
    public abstract record Global(Position Position) : WasmExpression(Position) {
      // Get - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global.get
      public sealed record Get(Position Position, WasmLabel Name) : Global(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(global.get {Name.ToWat(ctx)})";
      }
      // Set - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/global.set
      public sealed record Set(Position Position, WasmLabel Name, WasmExpression Value) : Global(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(global.set {Name.ToWat(ctx)} {Value.ToWat(ctx)})";
      }
    }
    // Local - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local
    public abstract record Local(Position Position) : WasmExpression(Position) {
      // Get - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.get
      public sealed record Get(Position Position, WasmLabel Name) : Local(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(local.get {Name.ToWat(ctx)})";
      }
      // Set - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.set
      public sealed record Set(Position Position, WasmLabel Name, WasmExpression Value) : Local(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(local.set {Name.ToWat(ctx)} {Value.ToWat(ctx)})";
      }
      // Tee - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Variables/local.tee
      public sealed record Tee(Position Position, WasmLabel Name, WasmExpression Value) : Local(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(local.tee {Name.ToWat(ctx)} {Value.ToWat(ctx)})";
      }
    }
    // Ref - https://webassembly.github.io/spec/core/text/instructions.html#reference-instructions
    public abstract record Ref(Position Position) : WasmExpression(Position) {
      public sealed record Null(Position Position, WasmType WasmType) : Ref(Position) {
        internal override string ToWat(WasmBuildCtx ctx) {
          // TODO: This feels like a code smell, we should probably figure out a good way to pass type references??
          if (WasmType is not WasmType.FuncRef funcRef) {
            throw new System.Exception($"Expected WasmType to be FuncRef, but got {WasmType}");
          }
          return $"(ref.null {funcRef.Label.ToWat(ctx)})";
        }
      }
      public sealed record Func(Position Position, WasmLabel FunctionName) : Ref(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(ref.func {FunctionName.ToWat(ctx)})";
      }
      // TODO: ref.is_null
      // TODO: ref.as_non_null
      public sealed record Eq(Position Position, WasmExpression LHS, WasmExpression RHS) : Ref(Position) {
        internal override string ToWat(WasmBuildCtx ctx) => $"(ref.eq {LHS.ToWat(ctx)} {RHS.ToWat(ctx)})";
      }
      // TODO: ref.test
      // TODO: ref.cast
    }
    // Block - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/block
    public record Block(
      Position Position, WasmLabel Label, IEnumerable<WasmExpression> Expressions, WasmType ResultType = null
    ) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) {
        var sb = new StringBuilder();
        var resultTypeStr = ResultType != null ? $"(result {ResultType.ToWat(ctx)})" : "";
        sb.Append($"(block {Label?.ToWat(ctx) ?? ""} {resultTypeStr}");
        foreach (var expr in Expressions) {
          sb.AppendLine();
          sb.Append($"  {expr.ToWat(ctx)}");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Br - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br
    public sealed record Br(Position Position, WasmLabel Label) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => $"(br {Label.ToWat(ctx)})";
    }
    // BrIf - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/br_if
    public sealed record BrIf(Position Position, WasmLabel Label, WasmExpression Condition) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => $"(br_if {Label.ToWat(ctx)} {Condition.ToWat(ctx)})";
    }
    // TODO: BrTable
    // Call - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/call
    public sealed record Call(Position Position, WasmLabel FunctionName, IEnumerable<WasmExpression> Arguments) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) {
        var sb = new StringBuilder();
        sb.Append($"(call {FunctionName.ToWat(ctx)}");
        foreach (var arg in Arguments) {
          sb.Append($" {arg.ToWat(ctx)}");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Call_ref - https://github.com/WebAssembly/gc/blob/main/proposals/function-references/Overview.md
    public sealed record CallRef(Position Position, WasmType WasmType, WasmExpression FunctionRef, IEnumerable<WasmExpression> Arguments) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) {
        var sb = new StringBuilder();
        if (WasmType is not WasmType.FuncRef) {
          throw new System.Exception($"Expected WasmType to be FuncRef, but got {WasmType}");
        }
        sb.Append($"(call_ref {(WasmType as WasmType.FuncRef).Label.ToWat(ctx)}");
        foreach (var arg in Arguments) {
          sb.Append($" {arg.ToWat(ctx)}");
        }
        sb.Append($" {FunctionRef.ToWat(ctx)}");
        sb.Append(')');
        return sb.ToString();
      }
    }
    // Drop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/drop
    public sealed record Drop(Position Position, WasmExpression Value) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => $"(drop {Value.ToWat(ctx)})";
    }
    // If - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/if...else
    public sealed record If(
#nullable enable
      Position Position, WasmExpression Condition, WasmExpression TrueBranch, WasmExpression? FalseBranch
#nullable disable
    ) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) {
        var sb = new StringBuilder();
        sb.Append($"(if {Condition.ToWat(ctx)}");
        sb.AppendLine();
        sb.Append($"  (then {TrueBranch.ToWat(ctx)})");
        if (FalseBranch != null) {
          sb.AppendLine();
          sb.Append($"  (else {FalseBranch.ToWat(ctx)})");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Loop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/loop
    public sealed record Loop(Position Position, WasmLabel Label, IEnumerable<WasmExpression> Body) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) {
        var sb = new StringBuilder();
        sb.Append($"(loop {Label.ToWat(ctx)}");
        foreach (var expr in Body) {
          sb.AppendLine();
          sb.Append($"  {expr.ToWat(ctx)}");
        }
        sb.Append(")");
        return sb.ToString();
      }
    }
    // Nop - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/nop
    public sealed record Nop(Position Position) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "(nop)";
    }
    // Return - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/return
#nullable enable
    public sealed record Return(Position Position, WasmExpression? Value) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => $"(return {Value?.ToWat(ctx) ?? string.Empty})";
    }
#nullable disable
    // TODO: Select
    // Unreachable - https://developer.mozilla.org/en-US/docs/WebAssembly/Reference/Control_flow/unreachable
    public sealed record Unreachable(Position Position) : WasmExpression(Position) {
      internal override string ToWat(WasmBuildCtx ctx) => "(unreachable)";
    }
  }
}
