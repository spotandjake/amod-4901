using System;
using System.Linq;
using System.Collections.Generic;

using ParseTree = Decaf.IR.ParseTree;
using TypedTree = Decaf.IR.TypedTree;
using Signature = Decaf.IR.Signature;
using Decaf.IR.PrimitiveDefinition;
using Decaf.Utils;

/// <summary>
/// This the plumbing for the type checker, the core type checking logic itself is implemented in `TypeCheckerCore`,
/// this file is responsible for traversing the tree, mapping the signatures and scopes. 
/// And finally emitting the typed tree with the correct signature on each node.
/// </summary>
namespace Decaf.MiddleEnd.TypeChecker {
  // The type checker itself
  public static partial class TypeChecker {
    // A simple helper function to construct simple function signatures for primitives
    private static Signature.Signature.FunctionSig MakeSimpleFunc(
      Position position,
      Signature.PrimitiveType[] paramTypes,
      Signature.PrimitiveType returnType
    ) {
      return new Signature.Signature.FunctionSig(
        position,
        paramTypes.Select(t => new Signature.Signature.PrimitiveSig(position, t)).ToArray(),
        new Signature.Signature.PrimitiveSig(position, returnType)
      );
    }
    // A simple resolver to resolve primitive nodes
    // The type checking function for primitive calls
    private static TypedTree.ExpressionNode.PrimCallNode TypePrimitiveCallExpressionNode(
      Context parentCtx,
      ParseTree.ExpressionNode.CallNode node
    ) {
      // Map the arguments
      var args = new List<TypedTree.ExpressionNode>();
      var argTypes = new List<Signature.Signature>();
      foreach (var arg in node.Arguments) {
        var typedArg = TypeExpressionNode(parentCtx, arg);
        args.Add(typedArg);
        argTypes.Add(typedArg.ExpressionType);
      }
      // Resolve the primitive being called
      var callee = PrimitiveTypes.ResolvePrimitive(node.Position, node.Callee, args.ToArray());
      // Determine the signature of the primitive being called
      var expectedSignature = callee switch {
        // General purpose primitives
        PrimDefinition.GetPointer =>
          // (val: a) => int
          new Signature.Signature.FunctionSig(node.Position,
            [args[0].ExpressionType],
            new Signature.Signature.PrimitiveSig(node.Position, Signature.PrimitiveType.Int)
          ),
        // --- @wasm namespace ---
        // memory sub namespace
        // () => int
        PrimDefinition.WasmMemorySize => MakeSimpleFunc(node.Position, [], Signature.PrimitiveType.Int),
        // (pageCount: int) => int
        PrimDefinition.WasmMemoryGrow => MakeSimpleFunc(node.Position, [Signature.PrimitiveType.Int], Signature.PrimitiveType.Int),
        // (pointer: int, value: int, byteCount: int) => int
        PrimDefinition.WasmMemoryFill =>
          MakeSimpleFunc(
            node.Position,
            [Signature.PrimitiveType.Int, Signature.PrimitiveType.Int, Signature.PrimitiveType.Int],
            Signature.PrimitiveType.Void
          ),
        // I32 sub namespace
        // (ptr: int, value: int) => void
        PrimDefinition.WasmI32Store or PrimDefinition.WasmI32Store8 or PrimDefinition.WasmI32Store16 =>
          MakeSimpleFunc(
            node.Position,
            [Signature.PrimitiveType.Int, Signature.PrimitiveType.Int],
            Signature.PrimitiveType.Void
          ),
        PrimDefinition.WasmI32Load =>
          MakeSimpleFunc(
            node.Position,
            [Signature.PrimitiveType.Int],
            Signature.PrimitiveType.Int
          ),
        // (val: int, val: int) => int
        PrimDefinition.WasmI32RemS or PrimDefinition.WasmI32RemU =>
          MakeSimpleFunc(
            node.Position,
            [Signature.PrimitiveType.Int, Signature.PrimitiveType.Int],
            Signature.PrimitiveType.Int
          ),
        // --- @cast namespace ---
        PrimDefinition.CastPtrToString =>
          MakeSimpleFunc(
            node.Position,
            [Signature.PrimitiveType.Int],
            Signature.PrimitiveType.String
          ),
        // NOTE: We can never encounter this case because we are exhausting all possible cases of PrimDefinition, 
        // but we need it to satisfy the compiler.
        _ => throw new Exception("Unreachable code in TypePrimitiveCallExpressionNode"),
      };
      // Construct the signature
      var signature = new Signature.Signature.FunctionSig(
        node.Position,
        argTypes.ToArray(),
        expectedSignature.ReturnType
      );
      // Check the signature matches the expected signature
      TypeCheckerCore.CheckSignature(expected: expectedSignature, received: signature);
      // Map the node itself
      return new TypedTree.ExpressionNode.PrimCallNode(node.Position, callee, args.ToArray(), expectedSignature.ReturnType);
    }
  }
}
