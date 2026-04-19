using System.Collections.Generic;
using System.Linq;
using AnfTree = Decaf.IR.AnfTree;

namespace Decaf.MiddleEnd.Optimizations {
  /// <summary>
  /// This class is responsible for performing a variety of dead code elimination optimizations on the ANF tree.
  /// This includes:
  /// * Unreachable code elimination:
  ///   * Instructions after a `return` statement can be removed
  ///   * Instructions after a `break` statement can be removed
  ///   * Instructions after a `continue` statement can be removed
  /// * Constant condition elimination:
  ///   * `if (false)` blocks can have their else branch lifted up and the if removed entirely
  ///   * `if (true)` blocks can have their then branch lifted up and the
  ///   * `loop { break }` can be removed entirely
  ///   * `{ <expr> }` blocks can have the expr lifted up and the block removed entirely
  /// 
  /// When to apply this optimization, this optimization is best applied after constants are propagated and inlined, as this will open
  /// more opportunities for dead code elimination. However, it may also make sense to apply this optimization immediately to cut down
  /// the size of the tree before performing other optimizations.
  /// </summary>
  internal static class DeadCodeOptimization {
    public static AnfTree.ProgramNode Optimize(AnfTree.ProgramNode node) {
      // Visit all the modules in the program and optimize them
      // NOTE: There is no reason we couldn't do this in parallel if we wanted to, but it is not a bottleneck at the moment so we can save that for later to avoid extra overhead from parallelization
      var optimizedModules = node.Modules.Select(OptimizeModule).ToArray();
      // Rebuild the node with the optimizations applied
      return new AnfTree.ProgramNode(node.Position, optimizedModules);
    }
    private static AnfTree.ModuleNode OptimizeModule(AnfTree.ModuleNode node) {
      // Optimize all the functions in the module
      var optimizedFunctions = new List<AnfTree.FunctionNode>();
      foreach (var func in node.Functions) {
        // Optimize the body of the function
        var optimizedBody = OptimizeBlockInstruction(func.Body);
        // Rebuild the function with the optimizations applied
        optimizedFunctions.Add(new AnfTree.FunctionNode(
          func.Position,
          func.Name,
          func.Parameters,
          optimizedBody,
          func.Signature
        ));
      }
      // Optimize the module body as well
      var optimizedModuleBody = OptimizeBlockInstruction(node.Body);
      // Rebuild the node with the optimizations applied
      return new AnfTree.ModuleNode(
        node.Position,
        node.Name,
        node.Imports,
        optimizedFunctions.ToArray(),
        optimizedModuleBody,
        node.Signature
      );
    }
#nullable enable
    private static AnfTree.InstructionNode? OptimizeInstruction(AnfTree.InstructionNode? node) {
#nullable disable
      if (node == null) return null;
      switch (node) {
        // We map anything with a body
        case AnfTree.InstructionNode.BlockNode blockNode: {
            var optimizedBlock = OptimizeBlockInstruction(blockNode);
            if (optimizedBlock.Instructions.Length == 0) return null;
            // If the block only has one instruction, we can just return that instruction and remove the block
            if (optimizedBlock.Instructions.Length == 1) return optimizedBlock.Instructions[0];
            return optimizedBlock;
          }
        case AnfTree.InstructionNode.IfNode ifNode: {
            var optimizedTrueBranch = OptimizeInstruction(ifNode.TrueBranch);
            var optimizedFalseBranch = ifNode.FalseBranch != null ? OptimizeInstruction(ifNode.FalseBranch) : null;
            return ifNode.Condition switch {
              // In the case that the condition is a constant true, we are always going to take the true branch
              AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: true } } =>
                optimizedTrueBranch,
              // In the case that the condition is a constant false, we are always going to take the false branch
              AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: false } } =>
                optimizedFalseBranch,
              // By default we just rebuild with the optimized branches
              _ => new AnfTree.InstructionNode.IfNode(ifNode.Position, ifNode.Condition, optimizedTrueBranch, optimizedFalseBranch),
            };
          }
        case AnfTree.InstructionNode.LoopNode loopNode: {
            var optimizedBody = OptimizeInstruction(loopNode.Body);
            if (optimizedBody == null) return null;
            // If the body is just a break statement, we can remove the entire loop
            if (optimizedBody is AnfTree.InstructionNode.BreakNode) return null;
            // Otherwise we just rebuild the loop with the optimized body
            return new AnfTree.InstructionNode.LoopNode(loopNode.Position, optimizedBody);
          }
        // By default we don't need to map
        default: return node;
      }
    }
    private static AnfTree.InstructionNode.BlockNode OptimizeBlockInstruction(AnfTree.InstructionNode.BlockNode node) {
      var optimizedInstructions = new List<AnfTree.InstructionNode>();
      foreach (var instr in node.Instructions) {
        var optimizedInstrs = OptimizeInstruction(instr);
        if (optimizedInstrs == null) continue; // If the instruction was optimized away, skip it
        optimizedInstructions.Add(optimizedInstrs);
        // If the instruction is a return, break, or continue statement, we can stop processing the rest of the block
        if (
          instr is AnfTree.InstructionNode.ReturnNode ||
              instr is AnfTree.InstructionNode.BreakNode ||
              instr is AnfTree.InstructionNode.ContinueNode
        ) break;
      }
      // Rebuild the node with the optimizations applied
      return new AnfTree.InstructionNode.BlockNode(node.Position, optimizedInstructions.ToArray());
    }
  }
}
