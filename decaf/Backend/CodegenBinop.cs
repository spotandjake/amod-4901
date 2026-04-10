using System;

using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;

namespace Decaf.Backend {
  public static partial class Codegen {
    private static WasmExpression CompileBinopNode(
      CodegenContext ctx,
      AnfTree.ExpressionNode.BinopNode node
    ) {
      WasmExpression lhs = CompileImmediate(ctx, node.Lhs);
      WasmExpression rhs = CompileImmediate(ctx, node.Rhs);
      // Determine what operator we are mapping
      return (node.Operator, node.ExpressionType) switch {
        // (int, int) => int - note as there is only one type we don't match it
        ("+", _) => new WasmExpression.I32.Add(node.Position, lhs, rhs),
        ("-", _) => new WasmExpression.I32.Sub(node.Position, lhs, rhs),
        ("*", _) => new WasmExpression.I32.Mul(node.Position, lhs, rhs),
        ("/", _) => new WasmExpression.I32.DivS(node.Position, lhs, rhs),
        // (int, int) => boolean
        ("<", _) => new WasmExpression.I32.LtS(node.Position, lhs, rhs),
        (">", _) => new WasmExpression.I32.GtS(node.Position, lhs, rhs),
        ("<=", _) => new WasmExpression.I32.LeS(node.Position, lhs, rhs),
        (">=", _) => new WasmExpression.I32.GeS(node.Position, lhs, rhs),
        // (a, a) => boolean - note as every literal currently is an i32 with no structural component we don't match the type
        ("==", _) => new WasmExpression.I32.Eq(node.Position, lhs, rhs),
        ("!=", _) => new WasmExpression.I32.Ne(node.Position, lhs, rhs),
        // (boolean, boolean) => boolean
        ("&&", _) =>
          // NOTE: we can use bitwise `and` for logical because we represent true as 1 and false as 0
          new WasmExpression.I32.And(node.Position, lhs, rhs),
        ("||", _) =>
          // NOTE: we can use bitwise `or` for logical because we represent true as 1 and false as 0
          new WasmExpression.I32.Or(node.Position, lhs, rhs),
        // (int, int) => int
        ("&", _) => new WasmExpression.I32.And(node.Position, lhs, rhs),
        ("|", _) => new WasmExpression.I32.Or(node.Position, lhs, rhs),
        ("<<", _) => new WasmExpression.I32.Shl(node.Position, lhs, rhs),
        (">>", _) => new WasmExpression.I32.ShrS(node.Position, lhs, rhs),
        // Unknown (should be impossible)
        _ => throw new Exception($"Unknown binary operator {node.Operator}"),
      };
    }
  }
}
