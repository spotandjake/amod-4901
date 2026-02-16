using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

[TestClass]
public class DecafParserTests : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory(System.IO.Path.Combine("Snapshots", nameof(DecafParserTests)));
    return settings;
  }
#nullable enable
  private ParseTree.ProgramNode? Parse(string text) {
    var lexer = Compiler.Compiler.LexString(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.Compiler.ParseTokenStream(tokenStream, null);
    return program;
  }
  #region ValidTests
  // Empty Program
  [TestMethod]
  public void TestEmpty() {
    Assert.Throws<Antlr4.Runtime.Misc.ParseCanceledException>(() => Parse(""));
  }
  // Classes
  [TestMethod]
  public Task TestEmptyBaseClass() {
    var result = Parse("class Main {}");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestEmptyClass() {
    var result = Parse("class Main extends Base {}");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestEmptyMultiClass() {
    var result = Parse("class Main {} class Main2 {}");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // Class Variable Declarations
  [TestMethod]
  public Task TestSingleVariableDeclaration() {
    var result = Parse("class Main { int x; }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestMultiVariableDeclaration() {
    var result = Parse("class Main { int x; int y; }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestMultiBindsDeclaration() {
    var result = Parse("class Main { int x, y, z; }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestArrayBindsDeclaration() {
    var result = Parse("class Main { int x[]; }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // Class Method Declarations
  [TestMethod]
  public Task TestSingleBasicMethodDeclaration() {
    var result = Parse("class Main { void foo() {} }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestMultiBasicMethodDeclaration() {
    var result = Parse("class Main { void foo() {} void bar() {} }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // Method Parameters
  [TestMethod]
  public Task TestMethodSingleParamDeclaration() {
    var result = Parse("class Main { void foo(int x) {} }");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestMethodMultiParamDeclaration() {
    var result = Parse("class Main { void foo(int x, int y) {} }");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestArrayMethodParamDeclaration() {
    var result = Parse("class Main { void foo(int x[]) {} }");
    return Verify(result, CreateSettings());
  }
  // Block Statements
  [TestMethod]
  public Task TestBasicBlock() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        int x;
        x = 1 + 1;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestVarOnlyBasicBlock() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        int x, y;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestStatementOnlyBasicBlock() {
    // NOTE: Because parsing doesn't do symbol validation this is valid (however it would fail semantically)
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = 1 + 1;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Statements
  // TODO: Assignment Statement (Simple something like x = 1)
  // TODO: Assignment Statement (Complex something  an x.y = 1)
  // TODO: Assignment Statement (Complex something  an x.y = 1)
  // TODO: Call Statement (Simple: foo())
  // TODO: Call Statement (Complex: x.foo())
  // TODO: Call Statement (Single Arg: foo(1))
  // TODO: Call Statement (Multi Arg: foo(1, 2))
  // TODO: Call Statement (Nested Expr Arg: foo(1 + 1))
  // NOTE: We currently do not handle strings in callouts just yet (so the test might not be perfect here)
  // TODO: Call Statement (Callout callout("Test", 1, 2))
  // TODO: If Statement (Simple: if (true) { })
  // TODO: If Statement (Complex: if (true) { } else { })
  // TODO: While Statement (while (true) { })
  // TODO: Return Statement (Simple: return)
  // TODO: Return Statement (Simple: return 1)
  // TODO: Add a failing statement test (where you test something like return with no semi, to the invalid tests area)
  // TODO: Expressions
  // TODO: Add similar tests for expressions as I listed for statements above
  #region Precedence
  // TODO: Validate Precedence (I don't think it's currently correct)
  [TestMethod]
  public Task TestPrecedence1() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = 1 + 1 * 2;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestPrecedence2() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = 1 + 1 + 2;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestPrecedence3() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = 1 + 1 - 2;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestPrecedence4() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = 1 - 1 + 2;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestPrecedence5() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = 1 - (1 + 2);
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Add more precedence testing
  #endregion
  //  Types
  [TestMethod]
  public Task TestTypes() {
    // NOTE: Because parsing doesn't do symbol validation this is valid (however it would fail semantically)
    var result = Parse(@"
    class Main {
      int x;
      boolean y;
      void z;
      t y;
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }

  // Full Stress Test Programs
  [TestMethod]
  public Task TestFullProgram() {
    // NOTE: The purpose of this test is just to ensure a practical program fully parses
    var result = Parse(@"
    class Main extends Base {
      int x, y, z[];

      void foo(int a, boolean b, int c[]) {
        if (a < 10 && b) {
          x = x + 1;
        } else {
          x = x - 1;
        }
      }

      int bar() {
        return x;
      }
    }

    class Base {
      void calloutMethod() {
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  #endregion
  // Invalid Tests
  [TestMethod]
  public void TestInvalidBlock() {
    // NOTE: This test is invalid because you can only define variables at the top of a block 
    Assert.Throws<Antlr4.Runtime.Misc.ParseCanceledException>(() => Parse(@"
    class Main {
      void testMethod() {
        int x;
        x = 1 + 1;
        int y;
      }
    }
    "));
  }
}
