namespace Decaf.Backend {
  using System;

  using Decaf.WasmBuilder;
  using AnfTree = Decaf.IR.AnfTree;
  using Ops = Decaf.IR.Operators;


  public static partial class Codegen {
    private static WasmExpression CompilePrefixSimpleExpr(
      CodegenContext ctx,
      AnfTree.SimpleExpressionNode.PrefixNode node
    ) {
      WasmExpression op = CompileImmediate(ctx, node.Operand);
      // Determine what operator we are mapping
      return (node.Operator, node.ExpressionType) switch {
        // (boolean) => boolean 
        (Ops.PrefixOperator.Not, _) =>
          new WasmExpression.I32.Eqz(node.Position, op),
        // (int) => int
        (Ops.PrefixOperator.BitwiseNot, _) =>
          new WasmExpression.I32.Xor(
            node.Position, op, new WasmExpression.I32.Const(node.Position, -1)
          ),
        // Unknown (should be impossible)
        _ => throw new Exception($"Unknown binary operator {node.Operator}"),
      };
    }
  }
}
