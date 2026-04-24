namespace Decaf.MiddleEnd.Optimizations {
  using System;

  using AnfTree = Decaf.IR.AnfTree;
  using Decaf.Utils;

  /// <summary>The different optimization passes that can be run on the ANF tree.</summary>
  public enum OptimizationPasses {
    DeadCodeElimination,
    ConstantOptimization,
  }
  /// <summary>
  /// This is the main entry pointer for running all optimizations on the ANF tree.
  /// </summary>
  public static class Optimizer {
    /// <summary>
    /// The default optimizations and the order they should be run in.
    /// 
    /// This is a good starting point for tuning the optimizations and finding the right balance between running enough optimizations to 
    /// get good performance but not running so many that it becomes inefficient.
    /// </summary>
    /// <returns>The default optimization passes.</returns>
    public static string[] GetDefaultPasses() => [
      // We run this first because it can cut down the size of the tree immediately and is very cheap
      Enum.GetName(OptimizationPasses.DeadCodeElimination),
      // Run constant folding and propogation to open up more opportunities for dead code elimination
      Enum.GetName(OptimizationPasses.ConstantOptimization),
      // Re-run dead code elimination to cut down the new opportunities created by constant folding and propagation
      Enum.GetName(OptimizationPasses.DeadCodeElimination),
    ];
    public static AnfTree.ProgramNode Optimize(CompilationConfig config, AnfTree.ProgramNode node) {
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
      //  Run the optimizations in the order specified by the config
      foreach (var pass in Enum.GetValues<OptimizationPasses>()) {
        if (config.SkipOptimizationPasses.Contains(Enum.GetName(pass))) continue;
        node = pass switch {
          OptimizationPasses.DeadCodeElimination => DeadCodeOptimization.Optimize(node),
          OptimizationPasses.ConstantOptimization => ConstantOptimization.Optimize(node),
          // NOTE: This will never be hit
          _ => throw new Exception($"Unknown optimization pass: {pass}"),
        };
      }
      // Return the optimized tree
      return node;
    }
  }
}
