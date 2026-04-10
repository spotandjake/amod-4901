using System;
using System.Collections.Generic;
using Decaf.IR.PrimitiveDefinition;
using Decaf.IR.TypedTree;
using Decaf.Utils;
using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;
using TypedTree = Decaf.IR.TypedTree;

// This is the core of the code generation phase. It takes an AnfTree and produces a WasmTree.
// The WasmTree can then independently be transformed directly into a wasm module.
namespace Decaf.Backend {
  public static class Codegen {
    private record struct CodegenContext {
      public Scope<int> Scope;
      // Related to looping
#nullable enable
      public WasmLabel? BreakLabel; // The label to break to when we encounter a `break`
      public WasmLabel? ContinueLabel; // The label to break to when we encounter
#nullable disable
      public static CodegenContext CreateInitialContext() {
        return new CodegenContext {
          Scope = new Scope<int>(null),
          // Looping
          BreakLabel = null,
          ContinueLabel = null,
        };
      }
    }
    public static WasmModule CompileProgram(AnfTree.ProgramNode node) {
      // Create our wasm module
      WasmModule module = new WasmModule(node.Position);
      // Create our initial codegen context
      var ctx = CodegenContext.CreateInitialContext();
      // TODO: Generate code for each module
      foreach (var mod in node.Modules) CompileModule(ctx, mod);
      // TODO: Generate a `_start` function that calls the `<x>.Main` method
      // TODO: Ensure we call `Program.Main` after all static initializers have run
      return module;
    }
    // Code Units
    private static void CompileModule(CodegenContext ctx, AnfTree.DeclarationNode.ModuleNode node) {
      // TODO: Create a global for each member
      foreach (var global in node.Globals) {
        // TODO: Add a global to the module for this global variable???
      }
      // TODO: Create a function for each method
      foreach (var method in node.Methods) {
        CompileMethod(ctx, method);
      }
    }
    private static void CompileMethod(CodegenContext ctx, AnfTree.DeclarationNode.MethodNode node) {
      // TODO: Create a wasm function type signature for the method
      // TODO: Compile the body
      var newCtx = ctx with {
        Scope = new Scope<int>(ctx.Scope),
        // Looping
        BreakLabel = null,
        ContinueLabel = null,
      };
      var compiledBody = CompileBlock(newCtx, node.Body);
      Console.WriteLine(compiledBody);
      // TODO: Create a wasm function with the compiled body and the signature
      // TODO: This should return the compiled function, the caller is responsible for adding it the module
    }
    // Blocks
    private static WasmExpression.Block CompileBlock(CodegenContext ctx, AnfTree.BlockNode node) {
      var instructions = new List<WasmExpression>();
      foreach (var instruction in node.Instructions) {
        instructions.Add(CompileInstruction(ctx, instruction));
      }
      return new WasmExpression.Block(node.Position, null, instructions);
    }
    // Instructions
    private static WasmExpression CompileInstruction(
      CodegenContext ctx,
      AnfTree.InstructionNode node
    ) {
      return node switch {
        AnfTree.InstructionNode.BindNode bindNode => CompileBindNode(ctx, bindNode),
        AnfTree.InstructionNode.AssignmentNode assignmentNode => CompileAssignmentNode(ctx, assignmentNode),
        AnfTree.InstructionNode.ExprNode exprNode => CompileExprNode(ctx, exprNode),
        AnfTree.InstructionNode.IfNode ifNode => CompileIfNode(ctx, ifNode),
        AnfTree.InstructionNode.LoopNode loopNode => CompileLoopNode(ctx, loopNode),
        AnfTree.InstructionNode.ContinueNode continueNode => CompileContinueNode(ctx, continueNode),
        AnfTree.InstructionNode.BreakNode breakNode => CompileBreakNode(ctx, breakNode),
        AnfTree.InstructionNode.ReturnNode returnNode => CompileReturnNode(ctx, returnNode),
        _ => throw new Exception($"Unknown instruction node kind: {node.Kind}"),
      };
    }
    private static WasmExpression CompileBindNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.BindNode node
    ) {
      // Compile the expression
      var compiledExpr = CompileSimpleExpr(ctx, node.Expression);
      // Compile the bind to the location
      return CompileLocationSet(ctx, node.Location, compiledExpr);
    }
    private static WasmExpression CompileAssignmentNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.AssignmentNode node
    ) {
      // Compile the immediate
      var compiledValue = CompileImmediate(ctx, node.Expression);
      // Compile the assignment to the location
      return CompileLocationSet(ctx, node.Location, compiledValue);
    }
    private static WasmExpression CompileExprNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.ExprNode node
    ) {
      var compiledExpr = CompileImmediate(ctx, node.Content);
      if (node.Content.Signature is not Signature.PrimitiveSignature { Type: PrimitiveType.Void }) {
        return new WasmExpression.Drop(node.Position, compiledExpr);
      }
      else return compiledExpr;
    }
    private static WasmExpression.If CompileIfNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.IfNode node
    ) {
      // Compile the condition
      var compiledCondition = CompileImmediate(ctx, node.Condition);
      // Compile the true branch
      var compiledTrueBranch = CompileBlock(ctx, node.TrueBranch);
      // Compile the false branch
      var compiledFalseBranch = node.FalseBranch != null ? CompileBlock(ctx, node.FalseBranch) : null;
      // Create a wasm `if` expression
      return new WasmExpression.If(node.Position, compiledCondition, compiledTrueBranch, compiledFalseBranch);
    }
    private static WasmExpression.Block CompileLoopNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.LoopNode node
    ) {
      // Properly generate the labels
      var loop_label = new WasmLabel.UniqueLabel(node.Position, "loop_inner");
      var block_label = new WasmLabel.UniqueLabel(node.Position, "loop_outer");
      var newCtx = ctx with {
        BreakLabel = block_label,
        ContinueLabel = loop_label,
      };
      // Compile the body
      var compiledBody = CompileBlock(newCtx, node.Body);
      // Create the wasm loop
      var compiledLoop = new WasmExpression.Loop(node.Position, loop_label, [compiledBody]);
      // Create the outer block
      var compiledBlock = new WasmExpression.Block(node.Position, block_label, [compiledLoop]);
      // Return the block
      return compiledBlock;
    }
    private static WasmExpression.Br CompileContinueNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.ContinueNode node
    ) {
      if (ctx.ContinueLabel == null) {
        // NOTE: Impossible due to semantic analysis
        throw new Exception("Continue statement not within a loop");
      }
      return new WasmExpression.Br(node.Position, ctx.ContinueLabel);
    }
    private static WasmExpression.Br CompileBreakNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.BreakNode node
    ) {
      if (ctx.BreakLabel == null) {
        // NOTE: Impossible due to semantic analysis
        throw new Exception("Continue statement not within a loop");
      }
      return new WasmExpression.Br(node.Position, ctx.BreakLabel);
    }
    private static WasmExpression.Return CompileReturnNode(
      CodegenContext ctx,
      AnfTree.InstructionNode.ReturnNode node
    ) {
      var compiledValue = node.Value != null ? CompileImmediate(ctx, node.Value) : null;
      return new WasmExpression.Return(node.Position, compiledValue);
    }
    // Simple Expressions
    private static WasmExpression CompileSimpleExpr(
      CodegenContext ctx,
      AnfTree.ExpressionNode node
    ) {
      return node switch {
        AnfTree.ExpressionNode.CallNode callNode => CompileCallNode(ctx, callNode),
        AnfTree.ExpressionNode.PrimitiveNode primitiveNode => CompilePrimitiveNode(ctx, primitiveNode),
        AnfTree.ExpressionNode.BinopNode binopNode => CompileBinopNode(ctx, binopNode),
        AnfTree.ExpressionNode.PrefixNode prefixNode => CompilePrefixNode(ctx, prefixNode),
        AnfTree.ExpressionNode.AllocateArrayNode allocateArrayNode => CompileAllocateArrayNode(ctx, allocateArrayNode),
        _ => throw new Exception($"Unknown simple expression node kind: {node.Kind}"),
      };
    }
    private static WasmExpression CompileCallNode(
      CodegenContext ctx,
      AnfTree.ExpressionNode.CallNode node
    ) {
      // TODO: Resolve the method being called to it's actual qualified name
      // TODO: Compile the arguments to the call
      // TODO: Create a `call` expression with the resolved method name and the compiled arguments
      throw new NotImplementedException("Method calls are not yet supported");
    }
    private static WasmExpression CompilePrimitiveNode(
      CodegenContext ctx,
      AnfTree.ExpressionNode.PrimitiveNode node
    ) {
      return node.Primitive switch {
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
        // Unknown
        _ => throw new Exception($"Unknown primitive: {node.Primitive}"),
      };
    }
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
    private static WasmExpression CompileAllocateArrayNode(
      CodegenContext ctx,
      AnfTree.ExpressionNode.AllocateArrayNode node
    ) {
      // TODO: Call `Runtime.calloc` with the size expression + 4 (for the array length)
      // TODO: Write the array length to memory
      // TODO: Return the pointer to the array
      throw new NotImplementedException("Array allocation is not yet supported");
    }
    // Immediate Expressions
    private static WasmExpression CompileImmediate(
      CodegenContext ctx,
      AnfTree.ImmediateNode node
    ) {
      return node switch {
        AnfTree.ImmediateNode.ConstantNode constantNode => CompileLiteral(ctx, constantNode.Value),
        // TODO: Implement location access
        AnfTree.ImmediateNode.LocationAccessNode idNode =>
          CompileLocationGet(ctx, idNode.Location),
        _ => throw new Exception($"Unknown immediate node kind: {node.Kind}"),
      };
    }
    // Literal Nodes
    private static WasmExpression.I32.Const CompileLiteral(
      CodegenContext ctx,
      TypedTree.LiteralNode node
    ) {
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
    // Locations
    private static WasmExpression CompileLocationSet(
      CodegenContext ctx,
      AnfTree.LocationNode node,
      WasmExpression value
    ) {
      return node switch {
        AnfTree.LocationNode.IdentifierAccessNode idNode => CompileLocationIdentifierSet(ctx, idNode, value),
        AnfTree.LocationNode.MemberAccessNode memberNode => CompileLocationMemberAccessSet(ctx, memberNode, value),
        AnfTree.LocationNode.ArrayAccessNode arrNode => CompileLocationArrayAccessSet(ctx, arrNode, value),
        _ => throw new Exception($"Unknown location node kind: {node.Kind}"),
      };
    }
    private static WasmExpression CompileLocationIdentifierSet(
      CodegenContext ctx,
      AnfTree.LocationNode.IdentifierAccessNode node,
      WasmExpression value
    ) {
      throw new NotImplementedException("Identifier sets are not yet supported");
    }
    private static WasmExpression CompileLocationMemberAccessSet(
      CodegenContext ctx,
      AnfTree.LocationNode.MemberAccessNode node,
      WasmExpression value
    ) {
      throw new NotImplementedException("Member access sets are not yet supported");
    }
    private static WasmExpression CompileLocationArrayAccessSet(
      CodegenContext ctx,
      AnfTree.LocationNode.ArrayAccessNode node,
      WasmExpression value
    ) {
      // Compile the root get expression
      var compiledRoot = CompileLocationGet(ctx, node.Root);
      // Compile a statement to get the length
      var compiledLength = new WasmExpression.I32.Load(
        node.Position,
        compiledRoot,
        new WasmExpression.I32.Const(node.Position, 0) // length is at offset 0
      );
      // Compile a statement to get the right index
      var compiledIndex = CompileImmediate(ctx, node.IndexImm);
      var compiledByteIndex = new WasmExpression.I32.Add(
        node.Position,
        new WasmExpression.I32.Mul(
          node.Position,
          compiledIndex,
          new WasmExpression.I32.Const(node.Position, 4) // Each element is 4 bytes (i32)
        ),
        new WasmExpression.I32.Const(node.Position, 4) // add 4 to skip the length field at the start of the array
      );
      // Compile a statement for a bounds check
      var compiledBoundsCheck = new WasmExpression.If(
        node.Position,
        new WasmExpression.I32.LtS(
          node.Position,
          CompileImmediate(ctx, node.IndexImm),
          compiledLength
        ),
        // Set the value
        new WasmExpression.I32.Store(
          node.Position,
          compiledRoot, // The base array pointer
          value,
          compiledByteIndex // Offset for the index
        ),
        // If the index is out of bounds, we currently just throw using `unreachable`
        // TODO: Use a proper wasm exception
        new WasmExpression.Unreachable(node.Position)
      );
      // Compile a statement to set the value at the index
      return new WasmExpression.Block(
        node.Position,
        new WasmLabel.UniqueLabel(node.Position, "array_set"),
        [compiledBoundsCheck]
      );
    }
    private static WasmExpression CompileLocationGet(
      CodegenContext ctx,
      AnfTree.LocationNode node
    ) {
      throw new NotImplementedException("Location gets are not yet supported");
    }
  }
}
