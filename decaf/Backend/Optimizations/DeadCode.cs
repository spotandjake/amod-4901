using System.Collections.Generic;
using System.Linq;
using AnfTree = Decaf.IR.AnfTree;
using TypedTree = Decaf.IR.TypedTree;

namespace Decaf.Backend.Optimizations {
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
  /// 
  /// TODO: Optimize `while(false)` by checking loop body
  /// 
  /// When to apply this optimization, this optimization is best applied after constants are propagated and inlined, as this will open
  /// more opportunities for dead code elimination. However, it may also make sense to apply this optimization immediately to cut down
  /// the size of the tree before performing other optimizations.
  /// </summary>
  internal static class DeadCodeOptimization {
    public static AnfTree.ProgramNode Optimize(AnfTree.ProgramNode node) {
      // Visit all the modules in the program and optimize them
      var optimizedModules = node.Modules.Select(OptimizeModule).ToArray();
      // Rebuild the node with the optimizations applied
      return new AnfTree.ProgramNode(node.Position, optimizedModules);
    }
    private static AnfTree.DeclarationNode.ModuleNode OptimizeModule(AnfTree.DeclarationNode.ModuleNode node) {
      // Optimize all the methods in the module
      var optimizedFunctions = node.Methods.Select(OptimizeMethod).ToArray();
      // Rebuild the node with the optimizations applied
      return new AnfTree.DeclarationNode.ModuleNode(
        node.Position,
        node.Name,
        node.Globals,
        optimizedFunctions.ToArray(),
        node.Signature
      );
    }
    private static AnfTree.DeclarationNode.MethodNode OptimizeMethod(AnfTree.DeclarationNode.MethodNode node) {
      // Optimize the body of the method
      var optimizedBody = OptimizeBlock(node.Body);
      // Rebuild the node with the optimizations applied
      return new AnfTree.DeclarationNode.MethodNode(
        node.Position,
        node.Name,
        node.Parameters,
        optimizedBody,
        node.Signature
      );
    }
    private static AnfTree.InstructionNode.BlockNode OptimizeBlock(AnfTree.InstructionNode.BlockNode node) {
      var optimizedInstructions = new List<AnfTree.InstructionNode>();
      foreach (var instr in node.Instructions) {
        var optimizedInstrs = OptimizeInstruction(instr);
        if (instr == null) continue; // If the instruction was optimized away, skip it
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
#nullable enable
    private static AnfTree.InstructionNode? OptimizeInstruction(AnfTree.InstructionNode? node) {
#nullable disable
      if (node == null) return null;
      switch (node) {
        // We map anything with a body
        case AnfTree.InstructionNode.BlockNode blockNode: {
            var optimizedBlock = OptimizeBlock(blockNode);
            if (optimizedBlock.Instructions.Length == 0) return null;
            // If the block only has one instruction, we can just return that instruction and remove the block
            if (optimizedBlock.Instructions.Length == 1) return optimizedBlock.Instructions[0];
            return optimizedBlock;
          }
        case AnfTree.InstructionNode.IfNode ifNode: {
            var optimizedTrueBranch = OptimizeInstruction(ifNode.TrueBranch);
            var optimizedFalseBranch = ifNode.FalseBranch != null ? OptimizeInstruction(ifNode.FalseBranch) : null;
            return ifNode.Condition switch {
              // In the case that the condition is a constant false, we are always going to take the false branch
              AnfTree.ImmediateNode.ConstantNode { Value: TypedTree.LiteralNodes.BooleanNode { Value: true } } =>
                optimizedTrueBranch,
              // In the case that the condition is a constant false, we are always going to take the false branch
              AnfTree.ImmediateNode.ConstantNode { Value: TypedTree.LiteralNodes.BooleanNode { Value: false } } =>
                optimizedFalseBranch,
              // By default we just rebuild with the optimized branches
              _ => new AnfTree.InstructionNode.IfNode(ifNode.Position, ifNode.Condition, optimizedTrueBranch, optimizedFalseBranch),
            };
          }
        case AnfTree.InstructionNode.LoopNode loopNode: {
            var optimizedBody = OptimizeBlock(loopNode.Body);
            if (optimizedBody.Instructions.Length == 0) return null;
            if (optimizedBody.Instructions.Length == 1) {
              var singleInstr = optimizedBody.Instructions[0];
              // If the single instruction is a break node, we can remove the entire loop
              if (singleInstr is AnfTree.InstructionNode.BreakNode) return null;
            }
            return new AnfTree.InstructionNode.LoopNode(loopNode.Position, optimizedBody);
          }
        // By default we don't need to map
        default: return node;
      }
    }
  }
}
