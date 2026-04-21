using System;
using Decaf.IR.Signature;
using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;
using Ops = Decaf.IR.Operators;

namespace Decaf.Backend {
  public static partial class Codegen {
    private static WasmExpression CompileBinopSimpleExpr(
      CodegenContext ctx,
      AnfTree.SimpleExpressionNode.BinopNode node
    ) {
      WasmExpression lhs = CompileImmediate(ctx, node.Lhs);
      WasmExpression rhs = CompileImmediate(ctx, node.Rhs);
      // Determine what operator we are mapping
      return (node.Operator, node.Lhs.Signature) switch {
        // (int, int) => int - note as there is only one type we don't match it
        (Ops.BinaryOperator.Add, _) => new WasmExpression.I32.Add(node.Position, lhs, rhs),
        (Ops.BinaryOperator.Minus, _) => new WasmExpression.I32.Sub(node.Position, lhs, rhs),
        (Ops.BinaryOperator.Multiply, _) => new WasmExpression.I32.Mul(node.Position, lhs, rhs),
        (Ops.BinaryOperator.Divide, _) => new WasmExpression.I32.DivS(node.Position, lhs, rhs),
        // (int, int) => boolean
        (Ops.BinaryOperator.LessThan, _) => new WasmExpression.I32.LtS(node.Position, lhs, rhs),
        (Ops.BinaryOperator.GreaterThan, _) => new WasmExpression.I32.GtS(node.Position, lhs, rhs),
        (Ops.BinaryOperator.LessThanOrEqual, _) => new WasmExpression.I32.LeS(node.Position, lhs, rhs),
        (Ops.BinaryOperator.GreaterThanOrEqual, _) => new WasmExpression.I32.GeS(node.Position, lhs, rhs),
        // (a, a) => boolean
        (Ops.BinaryOperator.Equal, Signature.PrimitiveSig { Type: PrimitiveType.String }) =>
          new WasmExpression.Call(
            node.Position,
            CompileSymbol(node.Position, ctx.Runtime.RuntimeStringEqual),
            [lhs, rhs]
          ),
        (Ops.BinaryOperator.Equal, Signature.FunctionSig) => throw new Exception("Cannot compare functions for equality"),
        (Ops.BinaryOperator.Equal, _) => new WasmExpression.I32.Eq(node.Position, lhs, rhs),
        (Ops.BinaryOperator.NotEqual, Signature.PrimitiveSig { Type: PrimitiveType.String }) =>
          new WasmExpression.Call(
            node.Position,
            CompileSymbol(node.Position, ctx.Runtime.RuntimeStringNotEqual),
            [lhs, rhs]
          ),
        (Ops.BinaryOperator.NotEqual, Signature.FunctionSig) => throw new Exception("Cannot compare functions for equality"),
        (Ops.BinaryOperator.NotEqual, _) => new WasmExpression.I32.Ne(node.Position, lhs, rhs),
        // (boolean, boolean) => boolean
        (Ops.BinaryOperator.And, _) =>
          // NOTE: we can use bitwise `and` for logical because we represent true as 1 and false as 0
          new WasmExpression.I32.And(node.Position, lhs, rhs),
        (Ops.BinaryOperator.Or, _) =>
          // NOTE: we can use bitwise `or` for logical because we represent true as 1 and false as 0
          new WasmExpression.I32.Or(node.Position, lhs, rhs),
        // (int, int) => int
        (Ops.BinaryOperator.BitwiseAnd, _) => new WasmExpression.I32.And(node.Position, lhs, rhs),
        (Ops.BinaryOperator.BitwiseOr, _) => new WasmExpression.I32.Or(node.Position, lhs, rhs),
        (Ops.BinaryOperator.BitwiseLeftShift, _) => new WasmExpression.I32.Shl(node.Position, lhs, rhs),
        (Ops.BinaryOperator.BitwiseRightShift, _) => new WasmExpression.I32.ShrS(node.Position, lhs, rhs),
        // Unknown (should be impossible)
        _ => throw new Exception($"Unknown binary operator {node.Operator}"),
      };
    }
  }
}
