using System;
using System.Collections.Generic;
using System.Linq;

using ParseTree = Decaf.IR.ParseTree;
using Signature = Decaf.IR.Signature;
using Decaf.IR.PrimitiveDefinition;
using Decaf.Utils.Errors.TypeCheckingErrors;
using Decaf.Utils;

// This file contains the logic responsible for resolving primitive calls to their signatures and definitions.
namespace Decaf.MiddleEnd.TypeChecker {
  // A type alias used for the return of a primitive resolution
  using PrimResolution = (Signature.Signature.MethodSig, PrimDefinition);
  public static class PrimitiveTypes {
    // A few helper functions for constructing types
    private static Signature.Signature.MethodSig MakeSimpleMethod(
      Position position,
      Signature.PrimitiveType[] paramTypes,
      Signature.PrimitiveType returnType
    ) {
      return new Signature.Signature.MethodSig(
        position,
        paramTypes.Select(t => new Signature.Signature.PrimitiveSig(position, t)).ToArray(),
        new Signature.Signature.PrimitiveSig(position, returnType)
      );
    }
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
    public static PrimResolution ResolvePrimitive(Position position, ParseTree.LocationNode node) {
      var path = GetLocationPath(position, node, []);
      return path switch {
      // We found the wasm namespace, so we can delegate to the wasm resolver
      ["@wasm", .. var rest] => ResolveWasmPrimitive(position, node, rest),
        // Unknown primitive call
        _ => throw new UnknownPrimitiveCall(position, node.ToString()),
      };
    }
    // --- Wasm Primitives ---
    // NOTE: This resolver resolves anything in the @wasm namespace, which contains primitives that map to wasm instructions
    private static PrimResolution ResolveWasmPrimitive(Position position, ParseTree.LocationNode node, List<string> path) {
      return path switch {
      // Memory namespace
      ["memory", .. var subPath] => subPath switch {
      // () => int
      ["size"] =>
        (
          MakeSimpleMethod(position, [], Signature.PrimitiveType.Int),
          PrimDefinition.WasmMemorySize
        ),
        // (pageCount: int) => int
        ["grow"] =>
                      (
                        MakeSimpleMethod(position, [Signature.PrimitiveType.Int], Signature.PrimitiveType.Int),
                        PrimDefinition.WasmMemoryGrow
                      ),
                      // (pointer: int, value: int, byteCount: int) => int
                      ["fill"] =>
                  (
                    MakeSimpleMethod(
                      position,
                      [Signature.PrimitiveType.Int, Signature.PrimitiveType.Int, Signature.PrimitiveType.Int],
                      Signature.PrimitiveType.Int
                    ),
                    PrimDefinition.WasmMemoryFill
                  ),
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      },
      // I32 namespace
      ["i32", .. var subPath] => subPath switch {
      // (ptr: int, offset: int, value: int) => void
      ["store"] =>
      (
          MakeSimpleMethod(
            position,
            [Signature.PrimitiveType.Int, Signature.PrimitiveType.Int, Signature.PrimitiveType.Int],
            Signature.PrimitiveType.Void
          ),
          PrimDefinition.WasmI32Store
        ),
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      },
        // Unknown
        _ => throw new UnknownPrimitiveCall(position, node.ToString())
      };
    }
  }
}

