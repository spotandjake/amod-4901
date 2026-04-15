using System;

using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;
using Decaf.IR.PrimitiveDefinition;

namespace Decaf.Backend {
  public static partial class Codegen {
    private static WasmExpression CompilePrimCallSimpleExpr(
      CodegenContext ctx,
      AnfTree.SimpleExpressionNode.PrimCallNode node
    ) {
      return node.Callee switch {
        // --- @wasm namespace ---
        // memory sub namespace
        PrimDefinition.WasmMemorySize => new WasmExpression.Memory.Size(node.Position),
        PrimDefinition.WasmMemoryGrow => new WasmExpression.Memory.Grow(node.Position, CompileImmediate(ctx, node.Arguments[0])),
        PrimDefinition.WasmMemoryFill =>
          new WasmExpression.Memory.Fill(
            node.Position,
            CompileImmediate(ctx, node.Arguments[0]),
            CompileImmediate(ctx, node.Arguments[1]),
            CompileImmediate(ctx, node.Arguments[2])
          ),
        // I32 sub namespace
        PrimDefinition.WasmI32Store =>
          new WasmExpression.I32.Store(
            node.Position,
            CompileImmediate(ctx, node.Arguments[0]),
            CompileImmediate(ctx, node.Arguments[2]),
            CompileImmediate(ctx, node.Arguments[1])
          ),
        // Unknown
        _ => throw new Exception($"Unknown primitive: {node.Callee}"),
      };
    }
  }
}
