using System;
using System.Collections.Generic;
using Decaf.WasmBuilder;
using Spectre.Console;
using AnfTree = Decaf.IR.AnfTree;
using Signature = Decaf.IR.Signature;

// TODO: It would probably make sense to using binaryen for code generation, we get pretty much nothing from doing it ourselves, and it would prevent a lot of subtle semantic bugs. However we are doing it this way for the learning experience as of now.

// This is the core of the code generation phase. It takes an AnfTree and produces a WasmTree.
// The WasmTree can then independently be transformed directly into a wasm module.
namespace Decaf.Backend {
  public static partial class Codegen {
#nullable enable
    private record struct CodegenContext {
      public WasmModule WasmModule; // The module we are currently building itself
      public Dictionary<Signature.Signature, WasmLabel> WasmTypes; // A mapping from signatures to the labels of their corresponding types in the module
      public Dictionary<WasmLabel, WasmType>? WasmLocals; // A list of locals in the current function
      public string? ModuleName; // The name of the module we are currently building, used for generating labels
      public bool TopLevel; // Whether we are currently compiling at the top level of a module (as opposed to within a block)
                            // Related to looping

      public WasmLabel? BreakLabel; // The label to break to when we encounter a `break`
      public WasmLabel? ContinueLabel; // The label to break to when we encounter
      public static CodegenContext CreateInitialContext(WasmModule WasmModule) {
        return new CodegenContext {
          WasmModule = WasmModule,
          WasmTypes = new Dictionary<Signature.Signature, WasmLabel>(),
          WasmLocals = null,
          ModuleName = null,
          TopLevel = true,
          // Looping
          BreakLabel = null,
          ContinueLabel = null,
        };
      }
    }
#nullable restore
    // --- Code Units ---
    #region CodeUnits
    public static WasmModule CompileProgram(AnfTree.ProgramNode node) {
      // Create our wasm module
      var module = new WasmModule(node.Position);
      // Create our initial codegen context
      var ctx = CodegenContext.CreateInitialContext(module);
      // Generate code for each module
      var startCalls = new List<WasmLabel>();
      foreach (var mod in node.Modules) {
        // Compile the module
        var mainFuncLabel = CompileModule(ctx, mod);
        // Add the start function names to a list
        startCalls.Add(mainFuncLabel);
      }
      // Create a start function that calls each of the `main` functions and add it to the module
      var body = new WasmExpression.Block(
        node.Position,
        null,
        startCalls.ConvertAll(label => new WasmExpression.Call(node.Position, label, []))
      );
      var startFunc = new WasmFunction(
        Position: node.Position,
        // TODO: Validate that this is right according to WASI
        Label: new WasmLabel.Label(node.Position, "_start"),
        Params: [],
        Results: [],
        Locals: new Dictionary<WasmLabel, WasmType>(),
        Body: body
      );
      module.AddFunction(startFunc);
      // TODO: Set this as the module start function
      // TODO: Add the wasm types to the module from the context
      return module;
    }
    private static WasmLabel CompileModule(CodegenContext ctx, AnfTree.ModuleNode node) {
      // Create a new context for the module
      var moduleCtx = ctx with { TopLevel = true, ModuleName = node.Name };
      // Compile the functions in the module
      foreach (var func in node.Functions) {
        var wasmFunc = CompileFunction(moduleCtx, func);
        // Add the compiled function to the module
        ctx.WasmModule.AddFunction(wasmFunc);
      }
      // Compile the module body
      var body = CompileBlockInstruction(moduleCtx, node.Body, isTopLevel: true);
      // Create a new function called `main` for the module body and add it to the module
      var label = CodegenUtils.GetMemberLabel(node.Position, node.Name, "_main");
      var mainFunc = new WasmFunction(
        Position: node.Position,
        Label: label,
        Params: [],
        Results: [],
        // TODO: Actually collect these from the function signature and body
        Locals: new Dictionary<WasmLabel, WasmType>(),
        Body: body
      );
      ctx.WasmModule.AddFunction(mainFunc);
      // Return the name of the `main` function for the start function to call
      return label;
    }
    private static WasmFunction CompileFunction(CodegenContext ctx, AnfTree.FunctionNode node) {
      // Create a new context for the function
      var funcCtx = ctx with {
        TopLevel = false,
        WasmLocals = new Dictionary<WasmLabel, WasmType>(),
        BreakLabel = null,
        ContinueLabel = null
      };
      // Compile the body of the function
      var body = CompileBlockInstruction(funcCtx, node.Body);
      // Collect the parameters
      var parameters = new Dictionary<WasmLabel, WasmType>();
      foreach (var param in node.Parameters) {
        parameters.Add(new WasmLabel.Label(param.Position, param.Name), GetWasmTypeFromSignature(funcCtx, param.Signature));
      }
      // Collect the return types from the signature
      var returnTypes = new List<WasmType>();
      if (node.Signature is not Signature.Signature.MethodSig methodSig) {
        // TODO: Enforce this variant in the IR
        throw new Exception($"Impossible, function has a non function signature type");
      }
      if (methodSig.ReturnType is not Signature.Signature.PrimitiveSig { Type: Signature.PrimitiveType.Void }) {
        returnTypes.Add(GetWasmTypeFromSignature(funcCtx, methodSig.ReturnType));
      }
      // Collect the local from the post compilation context
      var locals = new Dictionary<WasmLabel, WasmType>();
      foreach (var local in funcCtx.WasmLocals) {
        locals.Add(local.Key, local.Value);
      }
      // Build our wasm function
      return new WasmFunction(
        Position: node.Position,
        // TODO: Generate the label properly
        Label: CodegenUtils.GetMemberLabel(node.Position, ctx.ModuleName, node.Name),
        Params: parameters,
        // TODO: Collect the return types from the signature
        Results: returnTypes,
        Locals: locals,
        Body: body
      );
    }
    #endregion
    // --- Instructions ---
    #region Instructions
    private static WasmExpression CompileInstruction(
      CodegenContext ctx,
      AnfTree.InstructionNode node
    ) {
      return node switch {
        AnfTree.InstructionNode.BlockNode blockNode => CompileBlockInstruction(ctx, blockNode),
        AnfTree.InstructionNode.BindNode bindNode => CompileBindInstruction(ctx, bindNode),
        AnfTree.InstructionNode.AssignmentNode assignmentNode => CompileAssignmentInstruction(ctx, assignmentNode),
        AnfTree.InstructionNode.IfNode ifNode => CompileIfInstruction(ctx, ifNode),
        AnfTree.InstructionNode.LoopNode loopNode => CompileLoopInstruction(ctx, loopNode),
        AnfTree.InstructionNode.ReturnNode returnNode => CompileReturnInstruction(ctx, returnNode),
        AnfTree.InstructionNode.ContinueNode continueNode => CompileContinueInstruction(ctx, continueNode),
        AnfTree.InstructionNode.BreakNode breakNode => CompileBreakInstruction(ctx, breakNode),
        AnfTree.InstructionNode.SimpleExprInstructionNode exprNode => CompileSimpleExprInstruction(ctx, exprNode),
        _ => throw new Exception($"Unknown instruction node kind: {node.Kind}"),
      };
    }
    private static WasmExpression.Block CompileBlockInstruction(
      CodegenContext ctx, AnfTree.InstructionNode.BlockNode node, bool isTopLevel = false
    ) {
      var newCtx = ctx with { TopLevel = isTopLevel };
      var instructions = new List<WasmExpression>();
      foreach (var instruction in node.Instructions) {
        instructions.Add(CompileInstruction(newCtx, instruction));
      }
      return new WasmExpression.Block(node.Position, null, instructions);
    }
    private static WasmExpression CompileBindInstruction(CodegenContext ctx, AnfTree.InstructionNode.BindNode node) {
      // TODO: Investigate weather register allocation would be beneficial here
      // Compile the expression
      var compiledExpr = CompileSimpleExpr(ctx, node.SimpleExpression);
      // If this is a top level bind, we need to create a global variable for it
      if (ctx.TopLevel) {
        // TODO: We should probably do more anf analysis and figure out if this actually needs to be a global later
        var wasmType = GetWasmTypeFromSignature(ctx, node.SimpleExpression.ExpressionType);
        var label = CodegenUtils.GetMemberLabel(node.Position, ctx.ModuleName, node.Name);
        var global = new WasmGlobal(
          node.Position,
          Label: label,
          Type: wasmType,
          IsMutable: true,
          Init: GetDefaultValueFromSignature(node.SimpleExpression.ExpressionType)
        );
        // Add the global to the module
        ctx.WasmModule.AddGlobal(global);
        // Return the initializer for the global
        return new WasmExpression.Global.Set(node.Position, label, compiledExpr);
      }
      else {
        // Add the local variable to the context
        var localLabel = new WasmLabel.Label(node.Position, node.Name);
        var localType = GetWasmTypeFromSignature(ctx, node.SimpleExpression.ExpressionType);
        ctx.WasmLocals!.Add(localLabel, localType);
        // Otherwise, we can just create a local variable for it
        return new WasmExpression.Local.Set(node.Position, localLabel, compiledExpr);
      }
    }
    private static WasmExpression CompileAssignmentInstruction(CodegenContext parentCtx, AnfTree.InstructionNode.AssignmentNode node) {
      var ctx = parentCtx with { TopLevel = false };
      // Compile the immediate
      var compiledValue = CompileImmediate(ctx, node.Imm);
      // Compile the assignment to the location
      return CompileLocationSet(ctx, node.Location, compiledValue);
    }
    private static WasmExpression.If CompileIfInstruction(CodegenContext parentCtx, AnfTree.InstructionNode.IfNode node) {
      var ctx = parentCtx with { TopLevel = false };
      // Compile the condition
      var compiledCondition = CompileImmediate(ctx, node.Condition);
      // Compile the true branch
      var compiledTrueBranch = CompileInstruction(ctx, node.TrueBranch);
      // Compile the false branch
      var compiledFalseBranch = node.FalseBranch != null ? CompileInstruction(ctx, node.FalseBranch) : null;
      // Create a wasm `if` expression
      return new WasmExpression.If(node.Position, compiledCondition, compiledTrueBranch, compiledFalseBranch);
    }
    private static WasmExpression.Block CompileLoopInstruction(CodegenContext parentCtx, AnfTree.InstructionNode.LoopNode node) {
      var ctx = parentCtx with { TopLevel = false };
      // Properly generate the labels
      var loop_label = new WasmLabel.UniqueLabel(node.Position, "loop_inner");
      var block_label = new WasmLabel.UniqueLabel(node.Position, "loop_outer");
      var newCtx = ctx with {
        BreakLabel = block_label,
        ContinueLabel = loop_label,
      };
      // Compile the body
      var compiledBody = CompileInstruction(newCtx, node.Body);
      // Create the wasm loop
      var compiledLoop = new WasmExpression.Loop(node.Position, loop_label, [compiledBody]);
      // Create the outer block
      var compiledBlock = new WasmExpression.Block(node.Position, block_label, [compiledLoop]);
      // Return the block
      return compiledBlock;
    }
    private static WasmExpression.Return CompileReturnInstruction(CodegenContext ctx, AnfTree.InstructionNode.ReturnNode node) {
      var compiledValue = node.Value != null ? CompileImmediate(ctx, node.Value) : null;
      return new WasmExpression.Return(node.Position, compiledValue);
    }
    private static WasmExpression.Br CompileContinueInstruction(CodegenContext ctx, AnfTree.InstructionNode.ContinueNode node) {
      // NOTE: Impossible due to semantic analysis
      if (ctx.ContinueLabel == null) throw new Exception("Continue statement not within a loop");
      return new WasmExpression.Br(node.Position, ctx.ContinueLabel);
    }
    private static WasmExpression.Br CompileBreakInstruction(CodegenContext ctx, AnfTree.InstructionNode.BreakNode node) {
      // NOTE: Impossible due to semantic analysis
      if (ctx.BreakLabel == null) throw new Exception("Continue statement not within a loop");
      return new WasmExpression.Br(node.Position, ctx.BreakLabel);
    }
    private static WasmExpression CompileSimpleExprInstruction(
      CodegenContext ctx, AnfTree.InstructionNode.SimpleExprInstructionNode node
    ) {
      var compiledExpr = CompileSimpleExpr(ctx, node.Expr);
      if (node.Expr.ExpressionType is not Signature.Signature.PrimitiveSig { Type: Signature.PrimitiveType.Void }) {
        // We can't just leave a value on the stack, we need to drop it
        return new WasmExpression.Drop(node.Position, compiledExpr);
      }
      else return compiledExpr;
    }
    #endregion
    // --- Simple Expressions ---
    #region SimpleExpressions
    private static WasmExpression CompileSimpleExpr(
      CodegenContext parentCtx,
      AnfTree.SimpleExpressionNode node
    ) {
      var ctx = parentCtx with { TopLevel = false };
      return node switch {
        AnfTree.SimpleExpressionNode.PrefixNode prefixNode => CompilePrefixSimpleExpr(ctx, prefixNode),
        AnfTree.SimpleExpressionNode.BinopNode binopNode => CompileBinopSimpleExpr(ctx, binopNode),
        AnfTree.SimpleExpressionNode.CallNode callNode => CompileCallSimpleExpr(ctx, callNode),
        AnfTree.SimpleExpressionNode.PrimCallNode primCallNode => CompilePrimCallSimpleExpr(ctx, primCallNode),
        AnfTree.SimpleExpressionNode.ArrayInitNode arrayInitNode => CompileArrayInitSimpleExpr(ctx, arrayInitNode),
        AnfTree.SimpleExpressionNode.ImmediateExpressionNode immExprNode => CompileImmediateSimpleExpr(ctx, immExprNode.Imm),
        _ => throw new Exception($"Unknown simple expression node kind: {node.Kind}"),
      };
    }
    // See: CodegenPrefix for CompilePrefixSimpleExpr
    // See: CodegenBinop for CompileBinopSimpleExpr
    private static WasmExpression.CallRef CompileCallSimpleExpr(
      CodegenContext ctx,
      AnfTree.SimpleExpressionNode.CallNode node
    ) {
      // Resolve the method name being called
      // TODO: Handle the type properly here???
      var compiledCalleeType = GetWasmTypeFromSignature(ctx, node.Callee.LocationType);
      var compiledCallee = CompileLocationGet(ctx, node.Callee);
      // Compile the arguments to the call
      var args = new List<WasmExpression>();
      foreach (var arg in node.Arguments) args.Add(CompileImmediate(ctx, arg));
      // Create the call expression
      return new WasmExpression.CallRef(node.Position, compiledCalleeType, compiledCallee, args);
    }
    private static WasmExpression.Call CompileArrayInitSimpleExpr(
      CodegenContext ctx,
      AnfTree.SimpleExpressionNode.ArrayInitNode node
    ) {
      return new WasmExpression.Call(
        node.Position,
        new WasmLabel.Label(node.Position, CodegenUtils.Runtime.RuntimeAllocateArray),
        [CompileImmediate(ctx, node.SizeImm)]
      );
    }
    private static WasmExpression CompileImmediateSimpleExpr(
      CodegenContext ctx,
      AnfTree.ImmediateNode node
    ) {
      return CompileImmediate(ctx, node);
    }
    #endregion
    // --- Immediate Expressions ---
    #region ImmediateExpressions
    private static WasmExpression CompileImmediate(
      CodegenContext ctx,
      AnfTree.ImmediateNode node
    ) {
      return node switch {
        AnfTree.ImmediateNode.ConstantNode constantNode => CompileLiteral(ctx, constantNode.Value),
        AnfTree.ImmediateNode.LocationImmNode idNode => CompileLocationGet(ctx, idNode.Location),
        _ => throw new Exception($"Unknown immediate node kind: {node.Kind}"),
      };
    }
    #endregion
    // --- Literals ---
    #region Literals
    private static WasmExpression CompileLiteral(
      CodegenContext ctx,
      AnfTree.LiteralNode node
    ) {
      return node switch {
        // We represent integers as `(i32.const <value>)`
        AnfTree.LiteralNode.IntegerNode integerNode =>
          new WasmExpression.I32.Const(integerNode.Position, integerNode.Value),
        // We represent booleans as `(i32.const 1)` for true and `(i32.const 0)` for false
        AnfTree.LiteralNode.BooleanNode booleanNode =>
          new WasmExpression.I32.Const(booleanNode.Position, booleanNode.Value ? 1 : 0),
        // We represent characters as `(i32.const <value>)` where <value> is the unicode code point of the character
        AnfTree.LiteralNode.CharacterNode characterNode =>
          new WasmExpression.I32.Const(characterNode.Position, characterNode.Value),
        // TODO: Implement strings according to codegen design doc
        AnfTree.LiteralNode.StringNode stringNode =>
          throw new NotImplementedException("String literals are not yet supported"),
        AnfTree.LiteralNode.FunctionReferenceNode funcRefNode =>
          new WasmExpression.Ref.Func(funcRefNode.Position, CodegenUtils.GetMemberLabel(funcRefNode.Position, ctx.ModuleName, funcRefNode.FunctionName)),
        _ => throw new Exception($"Unknown literal node kind: {node.Kind}"),
      };
    }
    #endregion
    // --- Locations ---
    #region Locations
    private static WasmExpression CompileLocationSet(
      CodegenContext ctx,
      AnfTree.LocationNode node,
      WasmExpression value
    ) {
      // TODO: We should be smarter about name resolution
      switch (node) {
        case AnfTree.LocationNode.ArrayNode arrNode: {
            // Compile the root location get expression
            var compiledRoot = CompileLocationGet(ctx, arrNode.Root);
            // Compile a statement to get the index
            var compiledIndex = CompileImmediate(ctx, arrNode.IndexImm);
            var compiledByteOffset = CompileArrayByteIndex(arrNode.Position, compiledIndex);
            var compiledByteIndex = new WasmExpression.I32.Add(arrNode.Position, compiledRoot, compiledByteOffset);
            // Compile a statement for a bounds check
            var compiledBoundsCheck = CompileArrayBoundsCheck(node.Position, compiledRoot, compiledByteIndex);
            // Compile a statement to set the value
            var compiledSet = new WasmExpression.I32.Store(node.Position, compiledRoot, value, 0);
            // Wrap everything up
            return new WasmExpression.Block(
              node.Position,
              new WasmLabel.UniqueLabel(node.Position, "arr_set"),
              [compiledBoundsCheck, compiledSet]
            );
          }
        case AnfTree.LocationNode.MemberNode memberNode: {
            // NOTE: We currently have a restriction that all member accesses must be on a global variable
            //       This means that member accesses should always have the form `<module>.<member>`.
            if (memberNode.Root is not AnfTree.LocationNode.IdentifierNode rootIdNode) {
              // This shouldn't be possible due to typechecking and language construct constraints
              throw new Exception("Impossible, member access with unexpected root type");
            }
            // Get the mangled name
            var globalLabel = CodegenUtils.GetMemberLabel(node.Position, rootIdNode.Name, memberNode.Member);
            // Build the global.set
            return new WasmExpression.Global.Set(node.Position, globalLabel, value);
          }
        case AnfTree.LocationNode.IdentifierNode idNode:
          return new WasmExpression.Local.Set(
            node.Position,
            new WasmLabel.Label(node.Position, idNode.Name),
            value
          );
        // NOTE: This should be impossible in most cases
        default: throw new Exception($"Unknown location node kind: {node.Kind}");
      }
    }
    private static WasmExpression CompileLocationGet(
      CodegenContext ctx,
      AnfTree.LocationNode node
    ) {
      // TODO: We should be smarter about name resolution
      switch (node) {
        case AnfTree.LocationNode.ArrayNode arrNode: {
            // Compile the root get expression
            var compiledRoot = CompileLocationGet(ctx, arrNode.Root);
            // Compile a statement to get the index
            var compiledIndex = CompileImmediate(ctx, arrNode.IndexImm);
            var compiledByteOffset = CompileArrayByteIndex(arrNode.Position, compiledIndex);
            var compiledByteIndex = new WasmExpression.I32.Add(arrNode.Position, compiledRoot, compiledByteOffset);
            // Compile a statement for a bounds check
            var compiledBoundsCheck = CompileArrayBoundsCheck(arrNode.Position, compiledRoot, compiledByteOffset);
            // Compile a statement to get the value
            var compiledGet = new WasmExpression.I32.Load(arrNode.Position, compiledByteIndex, 0);
            // Wrap everything up
            return new WasmExpression.Block(
              arrNode.Position,
              new WasmLabel.UniqueLabel(arrNode.Position, "arr_get"),
              [compiledBoundsCheck, compiledGet]
            );
          }
        case AnfTree.LocationNode.MemberNode memberNode: {
            // NOTE: We currently have a restriction that all member accesses must be on a global variable
            //       This means that member accesses should always have the form `<module>.<member>`.
            if (memberNode.Root is not AnfTree.LocationNode.IdentifierNode rootIdNode) {
              // This shouldn't be possible due to typechecking and language construct constraints
              throw new Exception("Impossible, member access with unexpected root type");
            }
            // Get the mangled name
            var globalLabel = CodegenUtils.GetMemberLabel(node.Position, rootIdNode.Name, memberNode.Member);
            // Build the global.get
            return new WasmExpression.Global.Get(node.Position, globalLabel);
          }
        case AnfTree.LocationNode.IdentifierNode idNode:
          return new WasmExpression.Local.Get(
            idNode.Position,
            new WasmLabel.Label(idNode.Position, idNode.Name)
          );
        // NOTE: This should be impossible in most cases
        default: throw new Exception($"Unknown location node kind: {node.Kind}");
      }
    }
    #endregion
  }
}
