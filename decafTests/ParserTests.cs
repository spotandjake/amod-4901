using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.IR.ParseTree;
using Decaf.Utils;

[TestClass]
public class DecafParserTests : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Parser/");
    return settings;
  }
  private ProgramNode Parse(string text) {
#nullable enable
    var lexer = Compiler.Compiler.LexString(text, null);
#nullable disable
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.Compiler.ParseTokenStream(tokenStream);
    return program;
  }
  #region ValidTests
  // Empty Program
  [TestMethod]
  public void TestEmpty() {
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(""));
  }
  // Modules
  [TestMethod]
  public Task TestEmptyBaseModule() {
    var result = Parse("module Main {}");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestEmptyMultiModule() {
    var result = Parse("module Main {} module Main2 {}");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Module Variable Declarations
  [TestMethod]
  public Task TestSingleVariableDeclaration() {
    var result = Parse("module Main { int x; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiVariableDeclaration() {
    var result = Parse("module Main { int x; int y; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiBindsDeclaration() {
    var result = Parse("module Main { int x, y, z; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestArrayBindsDeclaration() {
    var result = Parse("module Main { int x[]; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Module Method Declarations
  [TestMethod]
  public Task TestSingleBasicMethodDeclaration() {
    var result = Parse("module Main { void foo() {} }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiBasicMethodDeclaration() {
    var result = Parse("module Main { void foo() {} void bar() {} }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Method Parameters
  [TestMethod]
  public Task TestMethodSingleParamDeclaration() {
    var result = Parse("module Main { void foo(int x) {} }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMethodMultiParamDeclaration() {
    var result = Parse("module Main { void foo(int x, int y) {} }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestArrayMethodParamDeclaration() {
    var result = Parse("module Main { void foo(int x[]) {} }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Block Statements
  [TestMethod]
  public Task TestBasicBlock() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        int x;
        x = 1 + 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestVarOnlyBasicBlock() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        int x, y;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestStatementOnlyBasicBlock() {
    // NOTE: Because parsing doesn't do symbol validation this is valid (however it would fail semantically)
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = 1 + 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  #region Statements
  [TestMethod]
  public Task TestSimpleAssignmentStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        int x;
        x = 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexAssignmentStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        Base.x = 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleCallStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        foo();
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexCallStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        Base.foo();
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSingleArgCallStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        foo(1);
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiArgCallStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        foo(1, 2);
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestNestedArgCallStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        foo(foo());
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCalloutStatement() {
    // NOTE: We currently do not handle strings in callouts just yet (so the test might not be perfect here)
    var result = Parse("module Main { void testMethod() { callout(\"Test\", 1, 2, \"y\"); } }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleIfStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        if (true) {
          x = 1;
        }
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexIfStatement() {
    var result = Parse(@"
    module Main {
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
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestWhileStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        int x;
        while (true) {
          x = 1;
        }
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleReturnStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        return;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexReturnStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        return 1;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestContinueStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        while (true) {
          continue;
        }
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBreakStatement() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        while (true) {
          break;
        }
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Expressions
  [TestMethod]
  public Task TestSimpleLocationAssignment() {
    // Testing the assignment of one variable to another (LocationExpr)
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = y;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestDotExpression() {
    return Verify(Parse(@"
    module Main {
      void testMethod() { 
        x = Base.y; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCallExpression1() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = foo(); 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCallExpression2() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = foo(1); 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCallExpression3() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = foo(1, 2); 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCalloutExpression() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = callout(""test"", 1, 2, ""y""); 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestArrayDeclaration() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        int x[];
        x = new int[1];
      }
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestIntExpression() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = 1; 
      }
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCharExpression() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = 'a'; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBoolExpression1() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = true; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBoolExpression2() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = false; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrefixExpression() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        boolean booly;
        booly = !true; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBinopExpression1() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = 1 + 1; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBinopExpression2() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = 1 + 1 + 1; 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestParenExpression() {
    return Verify(Parse(@"
    module Main { 
      void testMethod() { 
        x = (1 + 1); 
      } 
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleExpression() {
    var result = Parse(@"
    module main {
      void testMethod() {
        x = 5 * (3 + 2);
      }
    } 
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  #endregion
  #region Precedence
  // TODO: Validate Precedence (I don't think it's currently correct)
  [TestMethod]
  public Task TestPrecedence1() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = 1 + 1 * 2;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence2() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = 1 + 1 + 2;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence3() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = 1 + 1 - 2;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence4() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = 1 - 1 + 2;
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence5() {
    var result = Parse(@"
    module Main {
      void testMethod() {
        x = 1 - (1 + 2);
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // TODO: Add more precedence testing
  #endregion
  //  Types
  [TestMethod]
  public Task TestTypes() {
    // NOTE: Because parsing doesn't do symbol validation this is valid (however it would fail semantically)
    var result = Parse(@"
    module Main {
      string s;
      int x;
      boolean y;
      void z;
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }

  // Full Stress Test Programs
  [TestMethod]
  public Task TestFullProgram() {
    // NOTE: The purpose of this test is just to ensure a practical program fully parses
    var result = Parse(@"
    module Main {
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

    module Base {
      void calloutMethod() {
      }
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  #endregion
  // Invalid Tests
  [TestMethod]
  public void TestInvalidBlock() {
    // NOTE: This test is invalid because you can only define variables at the top of a block 
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    module Main {
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
    module Main {
      void testMethod() {
        while (true) {};
      }
    }
    "));
  }
  [TestMethod]
  public void TestInvalidStmtNoSemi() {
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    module Main {
      void testMethod() {
        x = 1
      }
    }
    "));
  }
  [TestMethod]
  public void TestInvalidParenExpression() {
    Assert.Throws<System.Data.SyntaxErrorException>(() => Parse(@"
    module Main { 
    void testMethod() { 
      x = (); 
      } 
    }
    "));
  }
}
