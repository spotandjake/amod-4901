// TODO: It would probably make sense to using binaryen for code generation, we get pretty much nothing from doing it ourselves, and it would prevent a lot of subtle semantic bugs. However we are doing it this way for the learning experience as of now.

// This is the core of the code generation phase. It takes an AnfTree and produces a WasmTree.
// The WasmTree can then independently be transformed directly into a wasm module.
namespace Decaf.Backend {
  using System;
  using System.Collections.Generic;
  using Decaf.WasmBuilder;
  using Decaf.Utils;
  using AnfTree = Decaf.IR.AnfTree;
  using Signature = Decaf.IR.Signature;
  using System.Linq;

  public static partial class Codegen {
#nullable enable
    private record struct CodegenContext {
      public CodegenUtils.Runtime Runtime;
      public WasmModule WasmModule; // The module we are currently building itself
      public Dictionary<Signature.Signature, WasmLabel> WasmTypes; // A mapping from signatures to the labels of their corresponding types in the module
      public Dictionary<WasmLabel, WasmType>? WasmLocals; // A list of locals in the current function

      public WasmLabel? BreakLabel; // The label to break to when we encounter a `break`
      public WasmLabel? ContinueLabel; // The label to break to when we encounter
      public static CodegenContext CreateInitialContext(CodegenUtils.Runtime Runtime, WasmModule WasmModule) {
        return new CodegenContext {
          Runtime = Runtime,
          WasmModule = WasmModule,
          WasmTypes = [],
          WasmLocals = null,
          // Looping
          BreakLabel = null,
          ContinueLabel = null,
        };
      }
    }
#nullable restore
    // --- Symbols ---
    private static WasmLabel.Label CompileSymbol(Position position, Symbol symbol) {
      return new WasmLabel.Label(position, symbol.GetUniqueName());
    }
    // --- Code Units ---
    #region CodeUnits
    public static WasmModule CompileProgram(CompilationConfig config, AnfTree.ProgramNode node) {
      // Create our wasm module
      var module = new WasmModule(node.Position);
      // Find the runtime class
      var runtimeModule = node.Modules.FirstOrDefault(m => m.ID.Name == CodegenUtils.Runtime.RuntimeModuleName);
      if (runtimeModule == null)
        throw new Exception($"Could not find runtime module with name {CodegenUtils.Runtime.RuntimeModuleName}");
      var runtime = new CodegenUtils.Runtime(runtimeModule.Signature);
      // Create our initial codegen context
      var ctx = CodegenContext.CreateInitialContext(runtime, module);
      // Add the memory to the module
      var memory = new WasmMemory(node.Position, new WasmLabel.Label(node.Position, "memory"), InitialPages: 1);
      ctx.WasmModule.AddMemory(memory);
      // Generate code for each module
      var startCalls = new List<WasmLabel>();
      foreach (var mod in node.Modules) {
        // Compile the module
        var mainFuncLabel = CompileModule(ctx, mod);
        // Add the start function names to a list
        startCalls.Add(mainFuncLabel);
      }
      // Create a start function that calls each of the `main` functions and add it to the module
      var body = startCalls.ConvertAll(label => new WasmExpression.Call(node.Position, label, []));
      var startLabel = new WasmLabel.Label(node.Position, "_start");
      var startFunc = new WasmFunction(
        Position: node.Position,
        Label: startLabel,
        Params: [],
        Results: [],
        Locals: new Dictionary<WasmLabel, WasmType>(),
        Body: body.ToArray()
      );
      module.AddFunction(startFunc);
      if (config.UseStartSection) module.SetStartFunction(startLabel);
      else {
        // TODO: export this module as `_start` so that it gets called when the module is instantiated
      }
      return module;
    }
    private static WasmLabel CompileModule(CodegenContext ctx, AnfTree.ModuleNode node) {
      // Create a new context for the module
      var moduleCtx = ctx with { WasmLocals = [] };
      // Compile the imports in the module
      foreach (var imp in node.Imports) {
        var import = new WasmImport(
          Position: imp.Position,
          Label: CompileSymbol(imp.Position, imp.ID),
          Module: imp.ExternalModule,
          Name: imp.ExternalName,
          Type: GetWasmTypeFromSignature(moduleCtx, imp.Signature, returnRef: false)
        );
        moduleCtx.WasmModule.AddImport(import);
      }
      // Compile the functions in the module
      foreach (var func in node.Functions) {
        var wasmFunc = CompileFunction(moduleCtx, func);
        // Add the compiled function to the module
        ctx.WasmModule.AddFunction(wasmFunc);
      }
      // Compile the module body
      var body = CompileBlockInstruction(moduleCtx, node.Body);
      // Collect the local from the post compilation context
      var locals = new Dictionary<WasmLabel, WasmType>();
      foreach (var local in moduleCtx.WasmLocals) {
        locals.Add(local.Key, local.Value);
      }
      // Create a new function called `main` for the module body and add it to the module
      var label = CompileSymbol(node.Position, node.ID);
      var mainFunc = new WasmFunction(
        Position: node.Position,
        Label: label,
        Params: [],
        Results: [],
        Locals: locals,
        Body: [body]
      );
      ctx.WasmModule.AddFunction(mainFunc);
      // Return the name of the `main` function for the start function to call
      return label;
    }
    private static WasmFunction CompileFunction(CodegenContext ctx, AnfTree.FunctionNode node) {
      // Create a new context for the function
      var funcCtx = ctx with {
        WasmLocals = [],
        BreakLabel = null,
        ContinueLabel = null
      };
      // Compile the body of the function
      var body = new List<WasmExpression> {
        CompileBlockInstruction(funcCtx, node.Body)
      };
      if (node.Signature.ReturnType is not Signature.Signature.PrimitiveSig { Type: Signature.PrimitiveType.Void }) {
        body.Add(new WasmExpression.Unreachable(node.Position));
      }
      // Collect the parameters
      var parameters = new Dictionary<WasmLabel, WasmType>();
      foreach (var param in node.Parameters) {
        parameters.Add(CompileSymbol(param.Position, param.ID), GetWasmTypeFromSignature(funcCtx, param.Signature));
      }
      // Collect the return types from the signature
      var returnTypes = new List<WasmType>();
      if (node.Signature.ReturnType is not Signature.Signature.PrimitiveSig { Type: Signature.PrimitiveType.Void }) {
        returnTypes.Add(GetWasmTypeFromSignature(funcCtx, node.Signature.ReturnType));
      }
      // Collect the local from the post compilation context
      var locals = new Dictionary<WasmLabel, WasmType>();
      foreach (var local in funcCtx.WasmLocals) {
        locals.Add(local.Key, local.Value);
      }
      // Build our wasm function
      return new WasmFunction(
        Position: node.Position,
        Label: CompileSymbol(node.Position, node.ID),
        Params: parameters,
        Results: returnTypes,
        Locals: locals,
        Body: body.ToArray()
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
      CodegenContext ctx, AnfTree.InstructionNode.BlockNode node
    ) {
      var instructions = new List<WasmExpression>();
      foreach (var instruction in node.Instructions) {
        instructions.Add(CompileInstruction(ctx, instruction));
      }
      return new WasmExpression.Block(node.Position, null, instructions);
    }
    private static WasmExpression CompileBindInstruction(CodegenContext ctx, AnfTree.InstructionNode.BindNode node) {
      // Compile the expression
      var compiledExpr = CompileSimpleExpr(ctx, node.SimpleExpression);
      // If this is a top level bind, we need to create a global variable for it
      if (node.ID.IsGlobal) {
        var wasmType = GetWasmTypeFromSignature(ctx, node.SimpleExpression.ExpressionType);
        var label = CompileSymbol(node.Position, node.ID);
        var global = new WasmGlobal(
          node.Position,
          Label: label,
          Type: wasmType,
          IsMutable: true,
          Init: GetDefaultValueFromSignature(ctx, node.SimpleExpression.ExpressionType)
        );
        // Add the global to the module
        ctx.WasmModule.AddGlobal(global);
        // Return the initializer for the global
        return new WasmExpression.Global.Set(node.Position, label, compiledExpr);
      }
      else {
        // Add the local variable to the context
        var localLabel = CompileSymbol(node.Position, node.ID);
        var localType = GetWasmTypeFromSignature(ctx, node.SimpleExpression.ExpressionType);
        ctx.WasmLocals!.Add(localLabel, localType);
        // Otherwise, we can just create a local variable for it
        return new WasmExpression.Local.Set(node.Position, localLabel, compiledExpr);
      }
    }
    private static WasmExpression CompileAssignmentInstruction(CodegenContext ctx, AnfTree.InstructionNode.AssignmentNode node) {
      // Compile the immediate
      var compiledValue = CompileImmediate(ctx, node.Imm);
      // Compile the assignment to the location
      return CompileLocationSet(ctx, node.Location, compiledValue);
    }
    private static WasmExpression.If CompileIfInstruction(CodegenContext ctx, AnfTree.InstructionNode.IfNode node) {
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
      // Properly generate the labels
      var loop_label = new WasmLabel.UniqueLabel(node.Position, "loop_inner");
      var block_label = new WasmLabel.UniqueLabel(node.Position, "loop_outer");
      var newCtx = parentCtx with {
        BreakLabel = block_label,
        ContinueLabel = loop_label,
      };
      // Compile the body
      var compiledBody = CompileInstruction(newCtx, node.Body);
      // Create the wasm loop
      var compiledLoop = new WasmExpression.Loop(node.Position, loop_label, [compiledBody, new WasmExpression.Br(node.Position, loop_label)]);
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
      if (ctx.BreakLabel == null) throw new Exception("Break statement not within a loop");
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
      CodegenContext ctx,
      AnfTree.SimpleExpressionNode node
    ) {
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
      // Resolve the function name being called
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
        CompileSymbol(node.Position, ctx.Runtime.RuntimeAllocateArray),
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
        AnfTree.LiteralNode.StringNode stringNode => CompileStringLiteral(ctx, stringNode),
        AnfTree.LiteralNode.FunctionReferenceNode funcRefNode =>
          new WasmExpression.Ref.Func(
            funcRefNode.Position,
            CompileSymbol(funcRefNode.Position, funcRefNode.ID)
          ),
        _ => throw new Exception($"Unknown literal node kind: {node.Kind}"),
      };
    }
    private static WasmExpression CompileStringLiteral(
      CodegenContext ctx,
      AnfTree.LiteralNode.StringNode node
    ) {
      // Create a data segment for the string literal
      var rawData = System.Text.Encoding.UTF8.GetBytes(node.Value);
      var dataSegment = new WasmDataSegment(
        Position: node.Position,
        Label: new WasmLabel.UniqueLabel(node.Position, $"str"),
        Data: rawData
      );
      // Add the data segment to the module
      ctx.WasmModule.AddDataSegment(dataSegment);
      // Create a local for the pointer
      var localLabel = new WasmLabel.UniqueLabel(node.Position, "str_ptr");
      ctx.WasmLocals.Add(localLabel, new WasmType.I32(node.Position));
      // Allocate the string
      var allocatedStringPtr = new WasmExpression.Call(
        node.Position,
        CompileSymbol(node.Position, ctx.Runtime.RuntimeAllocateString),
        [new WasmExpression.I32.Const(node.Position, rawData.Length)]
      );
      var storeAllocatedPtr = new WasmExpression.Local.Set(node.Position, localLabel, allocatedStringPtr);
      // Copy the data segment content into the allocated memory
      var copyData = new WasmExpression.Memory.Init(
        node.Position,
        dataSegment.Label,
        new WasmExpression.I32.Add(
          node.Position,
          new WasmExpression.Local.Get(node.Position, localLabel),
          new WasmExpression.I32.Const(node.Position, 4)
        ),
        new WasmExpression.I32.Const(node.Position, 0),
        new WasmExpression.I32.Const(node.Position, rawData.Length)
      );
      // Create a block to wrap the allocation and copy
      return new WasmExpression.Block(
        node.Position,
        null,
        [storeAllocatedPtr, copyData, new WasmExpression.Local.Get(node.Position, localLabel)],
        new WasmType.I32(node.Position)
      );
    }
    #endregion
    // --- Locations ---
    #region Locations
    private static WasmExpression CompileLocationSet(
      CodegenContext ctx,
      AnfTree.LocationNode node,
      WasmExpression value
    ) {
      switch (node) {
        case AnfTree.LocationNode.ArrayNode arrNode: {
            // Compile the root location get expression
            var compiledRoot = CompileLocationGet(ctx, arrNode.Root);
            // Compile a statement to get the index
            var compiledIndex = CompileImmediate(ctx, arrNode.IndexImm);
            var compiledByteOffset = CompileArrayByteIndex(arrNode.Position, compiledIndex);
            var compiledByteIndex = new WasmExpression.I32.Add(arrNode.Position, compiledRoot, compiledByteOffset);
            // Compile a statement for a bounds check
            var compiledBoundsCheck = CompileArrayBoundsCheck(node.Position, compiledRoot, compiledIndex);
            // Compile a statement to set the value
            var compiledSet = new WasmExpression.I32.Store(node.Position, compiledByteIndex, value, 0);
            // Wrap everything up
            return new WasmExpression.Block(
              node.Position,
              new WasmLabel.UniqueLabel(node.Position, "arr_set"),
              [compiledBoundsCheck, compiledSet]
            );
          }
        case AnfTree.LocationNode.SymbolLocation symbolNode: {
            // Based on the global state we generate either a `global.set` or a `local.set` here
            if (symbolNode.ID.IsGlobal) {
              // This is a global variable, we need to generate a `global.set`
              var globalLabel = CompileSymbol(node.Position, symbolNode.ID);
              return new WasmExpression.Global.Set(node.Position, globalLabel, value);
            }
            else {
              // This is a local variable, we need to generate a `local.set`
              var localLabel = CompileSymbol(node.Position, symbolNode.ID);
              return new WasmExpression.Local.Set(node.Position, localLabel, value);
            }
          }
        // NOTE: This should be impossible in most cases
        default: throw new Exception($"Unknown location node kind: {node.Kind}");
      }
    }
    private static WasmExpression CompileLocationGet(
      CodegenContext ctx,
      AnfTree.LocationNode node
    ) {
      switch (node) {
        case AnfTree.LocationNode.ArrayNode arrNode: {
            // Compile the root get expression
            var compiledRoot = CompileLocationGet(ctx, arrNode.Root);
            // Compile a statement to get the index
            var compiledIndex = CompileImmediate(ctx, arrNode.IndexImm);
            var compiledByteOffset = CompileArrayByteIndex(arrNode.Position, compiledIndex);
            var compiledByteIndex = new WasmExpression.I32.Add(arrNode.Position, compiledRoot, compiledByteOffset);
            // Compile a statement for a bounds check
            var compiledBoundsCheck = CompileArrayBoundsCheck(arrNode.Position, compiledRoot, compiledIndex);
            // Compile a statement to get the value
            var compiledGet = new WasmExpression.I32.Load(arrNode.Position, compiledByteIndex, 0);
            // Wrap everything up
            return new WasmExpression.Block(
              arrNode.Position,
              new WasmLabel.UniqueLabel(arrNode.Position, "arr_get"),
              [compiledBoundsCheck, compiledGet],
              new WasmType.I32(arrNode.Position)
            );
          }
        case AnfTree.LocationNode.SymbolLocation symbolNode: {
            // Based on the global state we generate either a `global.get` or a `local.get` here
            if (symbolNode.ID.IsGlobal) {
              // This is a global variable, we need to generate a `global.get`
              var globalLabel = CompileSymbol(node.Position, symbolNode.ID);
              return new WasmExpression.Global.Get(node.Position, globalLabel);
            }
            else {
              // This is a local variable, we need to generate a `local.get`
              var localLabel = CompileSymbol(node.Position, symbolNode.ID);
              return new WasmExpression.Local.Get(node.Position, localLabel);
            }
          }
        // NOTE: This should be impossible in most cases
        default: throw new Exception($"Unknown location node kind: {node.Kind}");
      }
    }
    #endregion
  }
}
