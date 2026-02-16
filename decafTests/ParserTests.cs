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
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(""));
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
  #region Statements
  [TestMethod]
  public Task TestSimpleAssignmentStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        int x;
        x = 1;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestComplexAssignmentStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        Base.x = 1;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestSimpleCallStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        foo();
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestComplexCallStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        Base.foo();
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestSingleArgCallStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        foo(1);
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestMultiArgCallStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        foo(1, 2);
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestNestedArgCallStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        foo(foo());
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestCalloutStatement() {
    // NOTE: We currently do not handle strings in callouts just yet (so the test might not be perfect here)
    var result = Parse("class Main { void testMethod() { callout(\"Test\", 1, 2); } }");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestSimpleIfStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        if (true) {
          x = 1;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestComplexIfStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        int x;
        if (true) {
          x = 1;
        } else {
          x = 2;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestWhileStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        int x;
        while (true) {
          x = 1;
        }
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestSimpleReturnStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        return;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  [TestMethod]
  public Task TestComplexReturnStatement() {
    var result = Parse(@"
    class Main {
      void testMethod() {
        return 1;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Expressions (Test these with something like x = <expr>)
  [TestMethod] 
  public Task TestSimpleExpression() {
    var result = Parse(@"
    class main {
      void testMethod() {
        x = 5 * (3 + 2);
      }
    } 
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Location Expression (Simple: x = y)
  [TestMethod]
  public Task TestSimpleLocationAssignment() {
    // Testing the assignment of one variable to another (LocationExpr)
    var result = Parse(@"
    class Main {
      void testMethod() {
        x = y;
      }
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Location Expression (Simple: x = Base.y)
  [TestMethod]
  public Task TestDotExpression() {
    return Verify(Parse(@"
    class Main {
      void testMethod() { 
        x = Base.y; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Location Expression (Simple: x = this)
  [TestMethod]
  public Task TestThisExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = this; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Call Expression (x = foo())
  [TestMethod]
  public Task TestCallExpression1() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = foo(); 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Call Expression (x = foo(1))
  [TestMethod]
  public Task TestCallExpression2() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = foo(1); 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Call Expression (x = foo(1, 2))
  [TestMethod]
  public Task TestCallExpression3() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = foo(1, 2); 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Callout Expression (x = callout("test", 1, 2))
  [TestMethod]
  public Task TestCalloutExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = callout(""test"", 1, 2); 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // NOTE: We don't test `new test()` as our parseTree won't handle this
  // NOTE: We don't test `new int 1` as our parseTree won't handle this
  // TODO: Int Expression (x = 1)
  [TestMethod]
  public Task TestIntExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = 1; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: char Expression (x = 'a')
  [TestMethod]
  public Task TestCharExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = 'a'; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: bool Expression (x = true)
  [TestMethod]
  public Task TestBoolExpression1() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = true; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: bool Expression (x = false)
  [TestMethod]
  public Task TestBoolExpression2() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = false; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: null Expression (x = null)
  [TestMethod]
  public Task TestNullExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = null; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Prefix Expression (!true)
  [TestMethod]
  public Task TestPrefixExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
    bool booly;
    booly = !true; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Binop Expression (Simple: x = 1 + 1) (Feel free to add more binop tests)
  [TestMethod]
  public Task TestBinopExpression1() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = 1 + 1; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Binop Expression (Chained: x = 1 + 1 + 1)
  [TestMethod]
  public Task TestBinopExpression2() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = 1 + 1 + 1; 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Paren Expression (x = (1 + 1))
  [TestMethod]
  public Task TestParenExpression() {
    return Verify(Parse(@"
    class Main { 
    void testMethod() { 
      x = (1 + 1); 
      } 
    }
    "), CreateSettings()).IgnoreMembers<ParseTree.Node>(x => x.Position);
  }
  // TODO: Add an invalid paren Expression `x = ()` to the invalid section (this is just empty)
  [TestMethod]
  public void TestInvalidParenExpression() {
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    class Main { 
    void testMethod() { 
      x = (); 
      } 
    }
    ")); 
  }
  #endregion
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
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    class Main {
      void testMethod() {
        int x;
        x = 1 + 1;
        int y;
      }
    }
    "));
  }
  [TestMethod]
  public void TestInvalidWhileSemi() {
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    class Main {
      void testMethod() {
        while (true) {};
      }
    }
    "));
  }
  [TestMethod]
  public void TestInvalidStmtNoSemi() {
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    class Main {
      void testMethod() {
        x = 1
      }
    }
    "));
  }
}
