using System;
using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;
using TypedTree = Decaf.IR.TypedTree;

// This is the core of the code generation phase. It takes an AnfTree and produces a WasmTree.
// The WasmTree can then independently be transformed directly into a wasm module.
namespace Decaf.Backend {
  public static class Codegen {
    public static void CompileProgram(AnfTree.ProgramNode node) {
      // TODO: Create our wasm module
      // TODO: Generate code for each class
      // TODO: Generate a `_start` function that calls the `<x>.Main` method
      // TODO: Ensure we call `Program.Main` after all static initializers have run
      // TODO: Document the behavior of static initializers
    }
    // Code Units
    private static void CompileClass(AnfTree.DeclarationNode.ModuleNode node) {
      // TODO: Create a global for each member
      // TODO: Create a function for each method
    }
    private static void CompileMethod(AnfTree.DeclarationNode.MethodNode node) {
      // TODO: Create a wasm function type signature for the method
      // TODO: Compile the body
      // TODO: Create a wasm function with the compiled body and the signature
      // TODO: This should return the compiled function, the caller is responsible for adding it the module
    }
    // Instructions
    private static void CompileInstruction(AnfTree.InstructionNode node) {
      // TODO: This should return the compiled statement
      switch (node) {
        case AnfTree.InstructionNode.BindNode bindNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.AssignmentNode assignmentNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.ExprNode exprNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.IfNode ifNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.LoopNode loopNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.ContinueNode continueNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.BreakNode breakNode:
          // TODO:
          break;
        case AnfTree.InstructionNode.ReturnNode returnNode:
          // TODO:
          break;
        default: throw new Exception($"Unknown instruction node kind: {node.Kind}");
      }
    }
    // Simple Expressions
    private static WasmBuilder.WasmExpression CompileSimpleExpr(AnfTree.ExpressionNode node) {
      return node switch {
        AnfTree.ExpressionNode.CallNode callNode => CompileCallNode(callNode),
        AnfTree.ExpressionNode.PrimitiveNode primitiveNode => CompilePrimitiveNode(primitiveNode),
        AnfTree.ExpressionNode.BinopNode binopNode => CompileBinopNode(binopNode),
        AnfTree.ExpressionNode.PrefixNode prefixNode => CompilePrefixNode(prefixNode),
        AnfTree.ExpressionNode.AllocateArrayNode allocateArrayNode => CompileAllocateArrayNode(allocateArrayNode),
        _ => throw new Exception($"Unknown simple expression node kind: {node.Kind}"),
      };
    }
    private static WasmExpression CompileCallNode(AnfTree.ExpressionNode.CallNode node) {
      // TODO:
      throw new NotImplementedException("Method calls are not yet supported");
    }
    private static WasmExpression CompilePrimitiveNode(AnfTree.ExpressionNode.PrimitiveNode node) {
      // TODO:
      throw new NotImplementedException("Primitive operations are not yet supported");
    }
    private static WasmExpression CompileBinopNode(AnfTree.ExpressionNode.BinopNode node) {
      WasmExpression lhs = CompileImmediate(node.Lhs);
      WasmExpression rhs = CompileImmediate(node.Rhs);
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
    private static WasmExpression CompilePrefixNode(AnfTree.ExpressionNode.PrefixNode node) {
      WasmExpression op = CompileImmediate(node.Operand);
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
    private static WasmExpression CompileAllocateArrayNode(AnfTree.ExpressionNode.AllocateArrayNode node) {
      // TODO: Call `Runtime.calloc` with the size expression + 4 (for the array length)
      // TODO: Write the array length to memory
      // TODO: Return the pointer to the array
      throw new NotImplementedException("Array allocation is not yet supported");
    }
    // Immediate Expressions
    private static WasmExpression CompileImmediate(AnfTree.ImmediateNode node) {
      return node switch {
        AnfTree.ImmediateNode.ConstantNode constantNode => CompileLiteral(constantNode.Value),
        // TODO: Implement location access
        AnfTree.ImmediateNode.LocationAccessNode idNode =>
          throw new NotImplementedException("Location access is not yet supported"),
        _ => throw new Exception($"Unknown immediate node kind: {node.Kind}"),
      };
    }
    // Literal Nodes
    private static WasmExpression CompileLiteral(TypedTree.LiteralNode node) {
      return node switch {
        // We represent integers as `(i32.const <value>)`
        TypedTree.LiteralNodes.IntegerNode integerNode =>
          new WasmExpression.I32.Const(integerNode.Position, integerNode.Value),
        // We represent characters as `(i32.const <value>)` where <value> is the unicode code point of the character
        TypedTree.LiteralNodes.CharacterNode characterNode =>
          new WasmExpression.I32.Const(characterNode.Position, characterNode.Value),
        // TODO: Add the string to the data section of the module
        // TODO: Add the string content to a data section
        // TODO: Construct an expression
        // TODO: The expression should `Runtime.malloc` the length of the string + 4 (for the string length)
        // TODO: The expression should then write the string length to memory
        // TODO: The expression should then write the string content to memory
        // TODO: The expression should then return the pointer to the string
        TypedTree.LiteralNodes.StringNode stringNode =>
          throw new NotImplementedException("String literals are not yet supported"),
        // We represent booleans as `(i32.const 1)` for true and `(i32.const 0)` for false
        TypedTree.LiteralNodes.BooleanNode booleanNode =>
          new WasmExpression.I32.Const(booleanNode.Position, booleanNode.Value ? 1 : 0),
        // For now we represent null as `(i32.const 0)` this means null has falsy semantics by default
        // NOTE: `Null` isn't very useful without class support, and it's more similar to `undefined`
        TypedTree.LiteralNodes.NullNode nullNode =>
          new WasmExpression.I32.Const(nullNode.Position, 0),
        _ => throw new Exception($"Unknown literal node kind: {node.Kind}"),
      };
    }
  }
}
