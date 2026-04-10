using System;
using System.Collections.Generic;
using System.Linq;
using Decaf.IR.TypedTree;
using Decaf.Utils;
using Decaf.WasmBuilder;
using AnfTree = Decaf.IR.AnfTree;
using TypedTree = Decaf.IR.TypedTree;

// This is the core of the code generation phase. It takes an AnfTree and produces a WasmTree.
// The WasmTree can then independently be transformed directly into a wasm module.
namespace Decaf.Backend {
  public static partial class Codegen {
    private record struct CodegenContext {
      public WasmModule WasmModule; // The module we are currently building itself
      // Related to looping
#nullable enable
      public WasmLabel? BreakLabel; // The label to break to when we encounter a `break`
      public WasmLabel? ContinueLabel; // The label to break to when we encounter
#nullable disable
      public static CodegenContext CreateInitialContext(WasmModule WasmModule) {
        return new CodegenContext {
          WasmModule = WasmModule,
          // Looping
          BreakLabel = null,
          ContinueLabel = null,
        };
      }
    }
    public static WasmModule CompileProgram(AnfTree.ProgramNode node) {
      // Create our wasm module
      var module = new WasmModule(node.Position);
      // Create our initial codegen context
      var ctx = CodegenContext.CreateInitialContext(module);
      // Generate code for each module
      var startCalls = new List<WasmExpression>();
      foreach (var mod in node.Modules) {
        // Compile the module
        CompileModule(ctx, mod);
        // Create the start call
        if (mod.Methods.Any((m) => m.Name == "Main") && mod.Name != "Program") {
          startCalls.Add(new WasmExpression.Call(
            mod.Position,
            CodegenUtils.GetMemberLabel(mod.Position, mod.Name, "Main"),
            []
          ));
        }
      }
      // Add our call to `Program.Main`
      startCalls.Add(new WasmExpression.Call(
        node.Position,
        CodegenUtils.GetMemberLabel(node.Position, "Program", "Main"),
        []
      ));
      // TODO: Generate a `_start` function that calls the `<x>.Main` method
      return module;
    }
    // Code Units
    private static void CompileModule(CodegenContext ctx, AnfTree.DeclarationNode.ModuleNode node) {
      //Create a global for each member
      foreach (var global in node.Globals) {
        var globalLabel = CodegenUtils.GetMemberLabel(global.Position, node.Name, global.Name);
        var wasmGlobal = new WasmGlobal(
          node.Position,
          globalLabel,
          // TODO: Map the signature
          new WasmType.I32(node.Position), // TODO: Support more types
          true, // TODO: Support immutable globals
          null // TODO: Support global initializers
        );
        // Add the global to the module
        ctx.WasmModule.AddGlobal(wasmGlobal);
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
        // Looping
        BreakLabel = null,
        ContinueLabel = null,
      };
      var compiledBody = CompileBlock(newCtx, node.Body);
      Console.WriteLine($"Compiled method {node.Name}:");
      Console.WriteLine(compiledBody.ToWat());
      // TODO: Create a wasm function with the compiled body and the signature
      // TODO: This should return the compiled function, the caller is responsible for adding it the module
    }
    // Instructions
    private static WasmExpression CompileInstruction(
      CodegenContext ctx,
      AnfTree.InstructionNode node
    ) {
      return node switch {
        AnfTree.InstructionNode.BindNode bindNode => CompileBindNode(ctx, bindNode),
        AnfTree.InstructionNode.AssignmentNode assignmentNode => CompileAssignmentNode(ctx, assignmentNode),
        AnfTree.InstructionNode.BlockNode blockNode => CompileBlock(ctx, blockNode),
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
    private static WasmExpression.Block CompileBlock(CodegenContext ctx, AnfTree.InstructionNode.BlockNode node) {
      var instructions = new List<WasmExpression>();
      foreach (var instruction in node.Instructions) {
        instructions.Add(CompileInstruction(ctx, instruction));
      }
      return new WasmExpression.Block(node.Position, null, instructions);
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
      var compiledTrueBranch = CompileInstruction(ctx, node.TrueBranch);
      // Compile the false branch
      var compiledFalseBranch = node.FalseBranch != null ? CompileInstruction(ctx, node.FalseBranch) : null;
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
      // Resolve the method name being called
      var methodLabel = node.Path switch {
        // TODO: More robust name resolution here
        AnfTree.LocationNode.IdentifierAccessNode idNode => new WasmLabel.Label(idNode.Position, idNode.Name),
        AnfTree.LocationNode.MemberAccessNode memberNode =>
          CodegenUtils.GetMemberLabel(memberNode.Position, memberNode.Root.Name, memberNode.Member),
        // NOTE: This is an enforced restriction due to the fact that we can't have arrays of functions
        _ => throw new Exception("Impossible, method call with unexpected path type"),
      };
      // Compile the arguments to the call
      var args = new List<WasmExpression>();
      foreach (var arg in node.Arguments) {
        args.Add(CompileImmediate(ctx, arg));
      }
      // Create the call expression
      return new WasmExpression.Call(node.Position, methodLabel, args);
    }
    private static WasmExpression CompileAllocateArrayNode(
      CodegenContext ctx,
      AnfTree.ExpressionNode.AllocateArrayNode node
    ) {
      return new WasmExpression.Call(
        node.Position,
        new WasmLabel.Label(node.Position, CodegenUtils.Runtime.RuntimeAllocateArray),
        [CompileImmediate(ctx, node.SizeImm)]
      );
    }
    // Immediate Expressions
    private static WasmExpression CompileImmediate(
      CodegenContext ctx,
      AnfTree.ImmediateNode node
    ) {
      return node switch {
        AnfTree.ImmediateNode.ConstantNode constantNode => CompileLiteral(ctx, constantNode.Value),
        AnfTree.ImmediateNode.LocationAccessNode idNode => CompileLocationGet(ctx, idNode.Location),
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
    private static WasmExpression.Local.Set CompileLocationIdentifierSet(
      CodegenContext ctx,
      AnfTree.LocationNode.IdentifierAccessNode node,
      WasmExpression value
    ) {
      // TODO: We should be smarter about name resolution
      return new WasmExpression.Local.Set(
        node.Position,
        new WasmLabel.Label(node.Position, node.Name),
        value
      );
    }
    private static WasmExpression.Global.Set CompileLocationMemberAccessSet(
      CodegenContext ctx,
      AnfTree.LocationNode.MemberAccessNode node,
      WasmExpression value
    ) {
      // TODO: We should be smarter about name resolution
      // Get the mangled name
      var globalLabel = CodegenUtils.GetMemberLabel(node.Position, node.Root.Name, node.Member);
      // Build the global.set
      return new WasmExpression.Global.Set(node.Position, globalLabel, value);
    }
    private static WasmExpression.Block CompileLocationArrayAccessSet(
      CodegenContext ctx,
      AnfTree.LocationNode.ArrayAccessNode node,
      WasmExpression value
    ) {
      // TODO: Consider moving this into the runtime
      // Compile the root get expression
      var compiledRoot = CompileLocationGet(ctx, node.Root);
      // Compile a statement to get the index
      var compiledIndex = CompileImmediate(ctx, node.IndexImm);
      var compiledByteIndex = CompileArrayByteIndex(node.Position, compiledIndex);
      // Compile a statement for a bounds check
      var compiledBoundsCheck = CompileArrayBoundsCheck(node.Position, compiledRoot, compiledIndex);
      // Compile a statement to set the value
      var compiledSet = new WasmExpression.I32.Store(node.Position, compiledRoot, value, compiledByteIndex);
      // Wrap everything up
      return new WasmExpression.Block(
        node.Position,
        new WasmLabel.UniqueLabel(node.Position, "arr_set"),
        [compiledBoundsCheck, compiledSet]
      );
    }
    private static WasmExpression CompileLocationGet(
      CodegenContext ctx,
      AnfTree.LocationNode node
    ) {
      return node switch {
        AnfTree.LocationNode.IdentifierAccessNode idNode => CompileLocationIdentifierGet(ctx, idNode),
        AnfTree.LocationNode.MemberAccessNode memberNode => CompileLocationMemberAccessGet(ctx, memberNode),
        AnfTree.LocationNode.ArrayAccessNode arrNode => CompileLocationArrayAccessGet(ctx, arrNode),
        _ => throw new Exception($"Unknown location node kind: {node.Kind}"),
      };
    }
    private static WasmExpression.Local.Get CompileLocationIdentifierGet(
      CodegenContext ctx,
      AnfTree.LocationNode.IdentifierAccessNode node
    ) {
      // TODO: We should be smarter about name resolution
      return new WasmExpression.Local.Get(
        node.Position,
        new WasmLabel.Label(node.Position, node.Name)
      );
    }
    private static WasmExpression.Global.Get CompileLocationMemberAccessGet(
      CodegenContext ctx,
      AnfTree.LocationNode.MemberAccessNode node
    ) {
      // TODO: We should be smarter about name resolution
      // Get the mangled name
      var globalLabel = CodegenUtils.GetMemberLabel(node.Position, node.Root.Name, node.Member);
      // Build the global.get
      return new WasmExpression.Global.Get(node.Position, globalLabel);
    }
    private static WasmExpression.Block CompileLocationArrayAccessGet(
      CodegenContext ctx,
      AnfTree.LocationNode.ArrayAccessNode node
    ) {
      // TODO: Consider moving this into the runtime
      // Compile the root get expression
      var compiledRoot = CompileLocationGet(ctx, node.Root);
      // Compile a statement to get the index
      var compiledIndex = CompileImmediate(ctx, node.IndexImm);
      var compiledByteIndex = CompileArrayByteIndex(node.Position, compiledIndex);
      // Compile a statement for a bounds check
      var compiledBoundsCheck = CompileArrayBoundsCheck(node.Position, compiledRoot, compiledIndex);
      // Compile a statement to get the value
      var compiledGet = new WasmExpression.I32.Load(node.Position, compiledRoot, compiledByteIndex);
      // Wrap everything up
      return new WasmExpression.Block(
        node.Position,
        new WasmLabel.UniqueLabel(node.Position, "arr_get"),
        [compiledBoundsCheck, compiledGet]
      );
    }
    // Array Helper
    private static WasmExpression.I32.Add CompileArrayByteIndex(
      Position position,
      WasmExpression index
    ) {
      // Each element is 4 bytes (i32)
      return new WasmExpression.I32.Add(
        position,
        new WasmExpression.I32.Mul(
          position,
          index,
          new WasmExpression.I32.Const(position, 4)
        ),
        new WasmExpression.I32.Const(position, 4) // add 4 to skip the length field at the start of the array
      );
    }
    private static WasmExpression.I32.If CompileArrayBoundsCheck(
      Position position,
      WasmExpression pointer,
      WasmExpression index
    ) {
      // Load the length from the start of the array
      var compiledLength = new WasmExpression.I32.Load(
        position,
        pointer,
        new WasmExpression.I32.Const(position, 0) // length is at offset 0
      );
      // Compare the index to the length
      var compiledBoundsCheck = new WasmExpression.I32.GeS(position, index, compiledLength);
      // If in bounds, continue with the original expression, otherwise execute the alternative (which should be a trap)
      return new WasmExpression.If(
        position,
        compiledBoundsCheck,
        // If the index is out of bounds, we currently just throw using `unreachable`
        // TODO: Use a proper wasm exception
        new WasmExpression.Unreachable(position),
        null
      );
    }
  }
}
