namespace decafTests.MiddleEnd;

using VerifyMSTest;
using VerifyTests;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using Decaf.Compiler;
using Signature = Decaf.IR.Signature;
using AnfTree = Decaf.IR.AnfTree;
using Decaf.MiddleEnd.Optimizations;
using Decaf.Utils;


[TestClass]
public class AnfTest : VerifyBase {
  private static VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Anf/");
    return settings;
  }
  private static AnfTree.ProgramNode Test(string text, List<string> skipOptimizationPasses) {
    var config = new CompilationConfig {
      UseStartSection = false,
      SkipOptimizationPasses = skipOptimizationPasses,
      BundleRuntime = false
    };
    var frontEndProgram = Compiler.FrontEnd(config, text, null);
    var middleEndProgram = Compiler.MiddleEnd(config, frontEndProgram);
    return middleEndProgram;
  }
  private static List<string> OnlyDeadCodeElimination =>
    Optimizer.GetDefaultPasses().Where(
      p => p != Enum.GetName(OptimizationPasses.DeadCodeElimination)
    ).ToList();
  private static List<string> OnlyConstantFoldingAndPropagation =>
    Optimizer.GetDefaultPasses().Where(
      p => p != Enum.GetName(OptimizationPasses.ConstantOptimization)
    ).ToList();
  // --- Basic Anf Test --
  [TestMethod]
  public Task TestValidAnf() {
    // NOTE: This test just validates the basic anf mapping
    var result = Test(@"
        module Program {
          let x: int = 1 + 2 + 3;
        }
      ",
      // Apply no optimizations to just test plain anf
      Optimizer.GetDefaultPasses().ToList()
    );
    // NOTE: We ignore the position and the signatures cause we don't really care about them (less noise == more clarity)
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  // --- Test Dead Code Elimination ---
  #region DeadCodeElimination
  [TestMethod]
  public void TestDeadCodeEliminationAfterReturn() {
    var result = Test(@"
        module Program {
          let test = (): void => {
            let y: int = 0;
            return;
            let x: int = 1;
          };
          test();
        }
      ",
      OnlyDeadCodeElimination
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(1, programModule.Functions);
    var testFunction = programModule.Functions[0];
    Assert.IsNotNull(testFunction);
    // NOTE: This is the main test, DCE got rid of the `let x: int = 1;` after the return
    Assert.HasCount(2, testFunction.Body.Instructions);
    Assert.IsTrue(testFunction.Body.Instructions[0] is AnfTree.InstructionNode.BindNode);
    Assert.IsTrue(testFunction.Body.Instructions[1] is AnfTree.InstructionNode.ReturnNode);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationAfterContinue() {
    var result = Test(@"
        module Program {
          while (1 == 0) {
            let y: int = 0;
            continue;
            let x: int = 1;
          }
        }
      ",
      OnlyDeadCodeElimination
    );
    // NOTE: We use a snapshot test here instead of structure because anf mangles loops,
    //       so snapshot testing is a bit more clear then trying to navigate the mangled loop structure 
    //       and validate the DCE that way.
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationAfterBreak() {
    var result = Test(@"
        module Program {
          while (1 == 0) {
            let y: int = 0;
            break;
            let x: int = 1;
          }
        }
      ",
      OnlyDeadCodeElimination
    );
    // NOTE: We use a snapshot test here instead of structure because anf mangles loops,
    //       so snapshot testing is a bit more clear then trying to navigate the mangled loop structure 
    //       and validate the DCE that way.
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public void TestDeadCodeEliminationIfTrue1() {
    // NOTE: This test just validates the basic anf mapping
    var result = Test(@"
        module Program {
          if (true) {
            let x: int = 1;
          } else {
            let y: int = 0;
          }
        }
      ",
      OnlyDeadCodeElimination
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(1, programModule.Body.Instructions);
    var instr = programModule.Body.Instructions[0];
    // NOTE: DCE got rid of the if statement so we expect the bind to be hoisted
    Assert.IsTrue(instr is AnfTree.InstructionNode.BindNode);
    // NOTE: We need to validate we hoisted the `x` bind instead of the `y` bind to make sure we got rid of the correct branch
    var bindNode = instr as AnfTree.InstructionNode.BindNode;
    Assert.IsNotNull(bindNode);
    Assert.AreEqual("x", bindNode.ID.Name);
  }
  [TestMethod]
  public void TestDeadCodeEliminationIfTrue2() {
    // NOTE: This test just validates the basic anf mapping
    var result = Test(@"
        module Program {
          if (true) {
            let x: int = 1;
          }
        }
      ",
      OnlyDeadCodeElimination
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(1, programModule.Body.Instructions);
    var instr = programModule.Body.Instructions[0];
    // NOTE: DCE got rid of the if statement so we expect the bind to be hoisted
    Assert.IsTrue(instr is AnfTree.InstructionNode.BindNode);
    // NOTE: We need to validate we hoisted the `x` bind instead of the `y` bind to make sure we got rid of the correct branch
    var bindNode = instr as AnfTree.InstructionNode.BindNode;
    Assert.IsNotNull(bindNode);
    Assert.AreEqual("x", bindNode.ID.Name);
  }
  [TestMethod]
  public void TestDeadCodeEliminationIfFalse1() {
    // NOTE: This test just validates the basic anf mapping
    var result = Test(@"
        module Program {
          if (false) {
            let x: int = 1;
          } else {
            let y: int = 0;
          }
        }
      ",
      OnlyDeadCodeElimination
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(1, programModule.Body.Instructions);
    var instr = programModule.Body.Instructions[0];
    // NOTE: DCE got rid of the if statement so we expect the bind to be hoisted
    Assert.IsTrue(instr is AnfTree.InstructionNode.BindNode);
    // NOTE: We need to validate we hoisted the `x` bind instead of the `y` bind to make sure we got rid of the correct branch
    var bindNode = instr as AnfTree.InstructionNode.BindNode;
    Assert.IsNotNull(bindNode);
    Assert.AreEqual("y", bindNode.ID.Name);
  }
  [TestMethod]
  public void TestDeadCodeEliminationIfFalse2() {
    // NOTE: This test just validates the basic anf mapping
    var result = Test(@"
        module Program {
          if (false) {
            let x: int = 1;
          }
        }
      ",
      // Apply no optimizations to just test plain anf
      OnlyDeadCodeElimination
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    // NOTE: Because DCE got rid of the if statement, and there was no else
    //       there is nothing to hoist so we expect the body to be empty
    Assert.HasCount(0, programModule.Body.Instructions);
  }
  [TestMethod]
  public void TestDeadCodeEliminationWhileFalse() {
    // NOTE: This test takes advantage of a lot of the dce optimizations working together,
    //       anf converts `while (<condition>) { <body> }` into `loop { if (<condition>) { <body> } else { break; } }`.
    //       in this case that would be `loop { if (false) { let x: int = 1; } else { break; } }`,
    //       DCE then handles the if statement by hoisting the false branch so we get `loop { { break; } }`,
    //       DCE then collapses the extra block and gets `loop { break; }`,
    //       DCE then realizes the loop breaks instantly and just gets rid of the loop entirely.
    var result = Test(@"
        module Program {
          while (false) {
            let x: int = 1;
          }
        }
      ",
      // Apply no optimizations to just test plain anf
      OnlyDeadCodeElimination
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    // NOTE: Because DCE got rid of the while statement, we expect the body to be empty
    Assert.HasCount(0, programModule.Body.Instructions);
  }
  #endregion
  // --- Test Constant Propagation And Folding ---
  #region ConstantFoldingAndPropagation
  [TestMethod]
  public void TestConstantFoldingAddition() {
    var result = Test(@"
        module Program {
          let x: int = 1 + 50;
        }
      ",
      OnlyConstantFoldingAndPropagation
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(0, programModule.Functions);
    Assert.HasCount(1, programModule.Body.Instructions);
    var instr = programModule.Body.Instructions[0];
    Assert.IsTrue(instr is AnfTree.InstructionNode.BindNode);
    var bindNode = instr as AnfTree.InstructionNode.BindNode;
    Assert.IsNotNull(bindNode);
    Assert.AreEqual("x", bindNode.ID.Name);
    Assert.IsTrue(bindNode.SimpleExpression is AnfTree.SimpleExpressionNode.ImmediateExpressionNode);
    var immExpr = bindNode.SimpleExpression as AnfTree.SimpleExpressionNode.ImmediateExpressionNode;
    Assert.IsTrue(immExpr.Imm is AnfTree.ImmediateNode.ConstantNode);
    var constNode = immExpr.Imm as AnfTree.ImmediateNode.ConstantNode;
    Assert.IsTrue(constNode.Value is AnfTree.LiteralNode.IntegerNode);
    var intNode = constNode.Value as AnfTree.LiteralNode.IntegerNode;
    // NOTE: This is the part that actually matters
    Assert.AreEqual(51, intNode.Value);
  }
  [TestMethod]
  public void TestConstantFoldingMultiplicationOnZero() {
    var result = Test(@"
        module Program {
          let x: int = 0 * 50;
        }
      ",
      OnlyConstantFoldingAndPropagation
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(0, programModule.Functions);
    Assert.HasCount(1, programModule.Body.Instructions);
    var instr = programModule.Body.Instructions[0];
    Assert.IsTrue(instr is AnfTree.InstructionNode.BindNode);
    var bindNode = instr as AnfTree.InstructionNode.BindNode;
    Assert.IsNotNull(bindNode);
    Assert.AreEqual("x", bindNode.ID.Name);
    Assert.IsTrue(bindNode.SimpleExpression is AnfTree.SimpleExpressionNode.ImmediateExpressionNode);
    var immExpr = bindNode.SimpleExpression as AnfTree.SimpleExpressionNode.ImmediateExpressionNode;
    Assert.IsTrue(immExpr.Imm is AnfTree.ImmediateNode.ConstantNode);
    var constNode = immExpr.Imm as AnfTree.ImmediateNode.ConstantNode;
    Assert.IsTrue(constNode.Value is AnfTree.LiteralNode.IntegerNode);
    var intNode = constNode.Value as AnfTree.LiteralNode.IntegerNode;
    // NOTE: This is the part that matters
    Assert.AreEqual(0, intNode.Value);
  }
  [TestMethod]
  public void TestConstantFoldingMultiplicativeIdentityProperty() {
    var result = Test(@"
        module Program {
          let x: int = 10;
          let y: int = x / x;
        }
      ",
      OnlyConstantFoldingAndPropagation
    );
    Assert.IsNotNull(result);
    Assert.HasCount(1, result.Modules);
    var programModule = result.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.HasCount(0, programModule.Functions);
    Assert.HasCount(2, programModule.Body.Instructions);
    var instr = programModule.Body.Instructions[1];
    Assert.IsTrue(instr is AnfTree.InstructionNode.BindNode);
    var bindNode = instr as AnfTree.InstructionNode.BindNode;
    Assert.IsNotNull(bindNode);
    Assert.AreEqual("y", bindNode.ID.Name);
    Assert.IsTrue(bindNode.SimpleExpression is AnfTree.SimpleExpressionNode.ImmediateExpressionNode);
    var immExpr = bindNode.SimpleExpression as AnfTree.SimpleExpressionNode.ImmediateExpressionNode;
    Assert.IsTrue(immExpr.Imm is AnfTree.ImmediateNode.ConstantNode);
    var constNode = immExpr.Imm as AnfTree.ImmediateNode.ConstantNode;
    Assert.IsTrue(constNode.Value is AnfTree.LiteralNode.IntegerNode);
    var intNode = constNode.Value as AnfTree.LiteralNode.IntegerNode;
    // NOTE: This is the part that matters
    Assert.AreEqual(1, intNode.Value);
  }
  [TestMethod]
  public Task TestConstantPropagation() {
    var result = Test(@"
        module Program {
          let x: int = 5;
          let y: int = x + 1;
        }
      ",
      OnlyConstantFoldingAndPropagation
    );
    // NOTE: After propagation, `x` is substituted into `y = x + 1`, then folding reduces it to `y = 6`
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestConstantPropagationLoop() {
    var result = Test(@"
        module Program {
          let x: int = 5;
          while (true) {
            let y: int = x + 1;
            x = 2;
          }
          let z: int = x + 1;
        }
      ",
      OnlyConstantFoldingAndPropagation
    );
    // NOTE: We can't propagate into loops as they are instanced multiple times, so we expect `y` to still be `x + 1` and not `6`
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestConstantPropagationIf() {
    var result = Test(@"
        module Program {
          let x: int = 5;
          if (true) {
            let y: int = x + 1;
            x = 3;
          }
          let z: int = x + 1;
        }
      ",
      OnlyConstantFoldingAndPropagation
    );
    // NOTE: We can't propogate after the `if` because we don't know if the `if` will execute and change the value of `x`, so we expect 
    // `z` to still be `x + 1` and not `6`
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  #endregion
}
