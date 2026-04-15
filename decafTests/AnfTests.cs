using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.Compiler;
using Signature = Decaf.IR.Signature;
using Decaf.IR.AnfTree;
using Decaf.Utils;

// NOTE: We test the anf tree rather lightly, furthur testing is done in the end to end and codegen tests.
//       There are not really any failure modes for the anf mapping as it is 1 to 1 with the typed tree,
//       these tests are mainly to ensure that the temporary binding generation makes sense, and the pass works.
//       You may question we are not testing the validity of the tree, and thats because the AnfTree itself
//       is defined in a way where it can only be valid.

[TestClass]
public class DecafAnfTests : VerifyBase {
  private static VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Anf/");
    return settings;
  }
#nullable enable
  private static ProgramNode? Anf(string text) {
    var frontEndProgram = Compiler.FrontEnd(text, null, bundleRuntime: false);
    var typeCheckingProgram = Compiler.TypeCheck(frontEndProgram);
    var anfProgram = Compiler.LowerToAnf(typeCheckingProgram);
    var optimizedProgram = Compiler.OptimizeAnf(anfProgram);
    return optimizedProgram;
  }
#nullable disable
  [TestMethod]
  public Task TestSimpleCompoundExpr1() {
    var result = Anf(@"
    module Program {
      let x: int = 1 + 2 + 3;
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  // Test Dead Code Elimination
  [TestMethod]
  public Task TestDeadCodeEliminationAfterReturn() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      let test = (): void => {
        y = 1;
        return;
        y = 2;
      };
      test();
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationAfterBreak() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      let test = (): void => {
        while (y == 0) {
          y = 1;
          break;
          y = 2;
        }
      };
      test();
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationAfterContinue() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      let test = (): void => {
        while (y == 0) {
          y = 1;
          continue;
          y = 2;
        }
      };
      test();
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfTrue1() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      if (true) {
        y = 1;
      } else {
        y = 2;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfTrue2() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      if (true) {
        y = 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfFalse1() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      if (false) {
        y = 1;
      } else {
        y = 2;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfFalse2() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      if (false) {
        y = 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationWhileFalse() {
    var result = Anf(@"
    module Program {
      let y: int = 0;
      while (false) {}
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>()
      .IgnoreInstance<Signature.Signature>(x => true);
  }
}
