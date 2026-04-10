using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.IR.AnfTree;

// NOTE: We test the anf tree rather lightly, furthur testing is done in the end to end and codegen tests.
//       There are not really any failure modes for the anf mapping as it is 1 to 1 with the typed tree,
//       these tests are mainly to ensure that the temporary binding generation makes sense, and the pass works.
//       You may question we are not testing the validity of the tree, and thats because the AnfTree itself
//       is defined in a way where it can only be valid.

[TestClass]
public class DecafAnfTests : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Anf/");
    return settings;
  }
#nullable enable
  private static ProgramNode? Anf(string text) {
    var lexer = Compiler.Compiler.LexString(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var parsed = Compiler.Compiler.ParseTokenStream(tokenStream);
    var scopedProgram = Compiler.Compiler.SemanticAnalysis(parsed);
    var typeCheckingProgram = Compiler.Compiler.TypeChecking(scopedProgram);
    var anfProgram = Compiler.Compiler.AnfMapping(typeCheckingProgram);
    return anfProgram;
  }
#nullable disable
  [TestMethod]
  public Task TestSimpleCompoundExpr1() {
    var result = Anf(@"
    class Program {
      void Main() {
        int x;
        x = 1 + 2 + 3;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  // Test Dead Code Elimination
  [TestMethod]
  public Task TestDeadCodeEliminationAfterReturn() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        y = 1;
        return;
        y = 2;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationAfterBreak() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        while (y == 0) {
          y = 1;
          break;
          y = 2;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationAfterContinue() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        while (y == 0) {
          y = 1;
          continue;
          y = 2;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfTrue1() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        if (true) {
          y = 1;
        } else {
          y = 2;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfTrue2() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        if (true) {
          y = 1;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfFalse1() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        if (false) {
          y = 1;
        } else {
          y = 2;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationIfFalse2() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        if (false) {
          y = 1;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestDeadCodeEliminationWhileFalse() {
    var result = Anf(@"
    class Program {
      int y;
      void Main() {
        while (false) {
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<Node>(x => x.Position);
  }
}
