using System;

using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;

namespace Decaf.Backend {
  public static partial class Codegen {
    private static WasmExpression CompilePrefixNode(
      CodegenContext ctx,
      AnfTree.ExpressionNode.PrefixNode node
    ) {
      WasmExpression op = CompileImmediate(ctx, node.Operand);
      // Determine what operator we are mapping
      return (node.Operator, node.ExpressionType) switch {
        // (boolean) => boolean 
        ("!", _) => new WasmExpression.I32.Eqz(node.Position, op),
        // (int) => int
        ("~", _) => new WasmExpression.I32.Xor(node.Position, op, new WasmExpression.I32.Const(node.Position, -1)),
        // Unknown (should be impossible)
        _ => throw new Exception($"Unknown binary operator {node.Operator}"),
      };
    }
  }
}
