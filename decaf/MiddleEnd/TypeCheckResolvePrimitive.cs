// This file contains logic responsible for resolving a primitive call
namespace Decaf.MiddleEnd.TypeChecker {
  using System;
  using System.Collections.Generic;

  using ParseTree = Decaf.IR.ParseTree;
  using TypedTree = Decaf.IR.TypedTree;
  using Decaf.IR.PrimitiveDefinition;
  using Decaf.Utils.Errors.TypeCheckingErrors;
  using Decaf.Utils;

  public static class PrimitiveTypes {
    // A small helper function to get the path from a location
    private static List<string> GetLocationPath(Position position, ParseTree.LocationNode node, List<string> acc) {
      // Very quick way to check if the node is a primitive call
      if (!node.IsPrimitive) throw new UnknownPrimitiveCall(position, node.ToString());
      // Get the path
      return node switch {
        // We do not support array primitives yet, so we can quickly throw if we encounter one.
        ParseTree.LocationNode.ArrayNode => throw new UnknownPrimitiveCall(position, node.ToString()),
        // Build our path by walking the tree
        ParseTree.LocationNode.MemberNode memberNode => GetLocationPath(position, memberNode.Root, [memberNode.Member, .. acc]),
        ParseTree.LocationNode.IdentifierNode identifierNode => [identifierNode.Name, .. acc],
        // NOTE: We can never encounter this case because we are exhausting all possible cases of LocationNode, 
        // but we need it to satisfy the compiler.
        _ => throw new Exception("Unreachable code in GetLocationPath"),
      };
    }
    // The main resolver
    public static PrimDefinition ResolvePrimitive(
      Position position, ParseTree.LocationNode node, TypedTree.ExpressionNode[] args
    ) {
      var path = GetLocationPath(position, node, []);
      return path switch {
      ["@getPointer"] => PrimDefinition.GetPointer,
      // We found the wasm namespace, so we can delegate to the wasm resolver
      ["@wasm", .. var rest] => ResolveWasmPrimitive(position, node, rest),
      // We found the cast namespace, so we can delegate to the cast resolver
      ["@cast", .. var rest] => ResolveCastNameSpace(position, node, rest),
        // Unknown primitive call
        _ => throw new UnknownPrimitiveCall(position, node.ToString()),
      };
    }
    // --- Wasm Primitives ---
    // NOTE: This resolver resolves anything in the @wasm namespace, which contains primitives that map to wasm instructions
    private static PrimDefinition ResolveWasmPrimitive(Position position, ParseTree.LocationNode node, List<string> path) {
      return path switch {
      // General namespace
      ["unreachable"] => PrimDefinition.Unreachable,
      // Memory namespace
      ["memory", .. var subPath] => subPath switch {
      // () => int
      ["size"] => PrimDefinition.WasmMemorySize,
      // (pageCount: int) => int
      ["grow"] => PrimDefinition.WasmMemoryGrow,
      // (pointer: int, value: int, byteCount: int) => int
      ["fill"] => PrimDefinition.WasmMemoryFill,
      // (dest: int, src: int, byteCount: int) => void
      ["copy"] => PrimDefinition.WasmMemoryCopy,
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      },
      // I32 namespace
      ["i32", .. var subPath] => subPath switch {
      // (ptr: int, value: int) => void
      ["store"] => PrimDefinition.WasmI32Store,
      // (ptr: int, value: int) => void
      ["store8"] => PrimDefinition.WasmI32Store8,
      // (ptr: int, value: int) => void
      ["store16"] => PrimDefinition.WasmI32Store16,
      // ptr: int => int
      ["load"] => PrimDefinition.WasmI32Load,
      ["load8_s"] => PrimDefinition.WasmI32Load8S,
      ["load8_u"] => PrimDefinition.WasmI32Load8U,
      ["load16_s"] => PrimDefinition.WasmI32Load16S,
      ["load16_u"] => PrimDefinition.WasmI32Load16U,
      // (val: int) => int
      ["remS"] => PrimDefinition.WasmI32RemS,
      // (val: int) => int
      ["remU"] => PrimDefinition.WasmI32RemU,
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      },
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      };
    }
    // --- Wasm Primitives ---
    // NOTE: This resolver resolves anything in the @cast namespace, which contains primitives that perform type level casting
    private static PrimDefinition ResolveCastNameSpace(Position position, ParseTree.LocationNode node, List<string> path) {
      return path switch {
      ["ptrToString"] => PrimDefinition.CastPtrToString,
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      };
    }
  }
}

