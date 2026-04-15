using AnfTree = Decaf.IR.AnfTree;

namespace Decaf.MiddleEnd.Optimizations {
  /// <summary>
  /// This is the main entry pointer for running all optimizations on the ANF tree.
  /// </summary>
  public static class Optimizer {
    public static AnfTree.ProgramNode Optimize(AnfTree.ProgramNode node) {
      /*
       * Optimization passes are a really hard thing to get right, and there are a lot of constraints on using them,
       * when to use them, and how to use them. For example, the order we run optimizations in is really important,
       * something like constant folding and propagation opens up a lot of opportunities for dead code elimination,
       * so we want to run constant folding and propagation before dead code elimination. However, we also want to run
       * dead code elimination as soon as possible to cut down the size of the tree before running other optimizations.
       * So it's important we find a balance and decide how to run the optimization in a way that is both efficient and effective.
       * 
       * This file needs a lot more tuning and testing to find the right balance but currently we are deciding the 
       * order based on what we think will be most effective.
       */
      // First we run Dead Code Elimination to cut down the immediate trees (e.g. removing unreachable code after a return statement)
      node = DeadCodeOptimization.Optimize(node);
      // TODO: Run constant folding and propogation to open up more opportunities for dead code elimination
      // TODO: Re-run dead code elimination to cut down the new opportunities created by constant folding and propagation
      // node = DeadCodeOptimization.Optimize(node);

      return node;
    }
  }
}
