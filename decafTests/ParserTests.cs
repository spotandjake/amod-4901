using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.Compiler;
using Decaf.IR.ParseTree;
using Decaf.Utils;
using Decaf.Utils.Errors.ParsingErrors;

[TestClass]
public class DecafParserTests : VerifyBase {
  private static VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Parser/");
    return settings;
  }
  private static ProgramNode Parse(string text) {
#nullable enable
    var lexer = Compiler.LexSource(text, null);
#nullable disable
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.ParseSource(tokenStream);
    return program;
  }
  #region ValidTests
  // Empty Program
  [TestMethod]
  public void TestEmpty() {
    Assert.Throws<UnrecognizedTokenException>(() => Parse(""));
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
    var result = Parse("module Main { let x: int = 1; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiVariableDeclaration() {
    var result = Parse("module Main { let x: int = 1; let y: int = 2; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiBindsDeclaration() {
    var result = Parse("module Main { let x: int = 1, y: int = 2, z: int = 3; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestArrayBindsDeclaration() {
    var result = Parse("module Main { let x: int[] = new int[5]; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Module Method Declarations
  [TestMethod]
  public Task TestSingleBasicMethodDeclaration() {
    var result = Parse("module Main { let foo = (): void => {}; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiBasicMethodDeclaration() {
    var result = Parse("module Main { let foo = (): void => {}; let bar = (): void => {}; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Method Parameters
  [TestMethod]
  public Task TestMethodSingleParamDeclaration() {
    var result = Parse("module Main { let foo = (x: int): void => {}; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMethodMultiParamDeclaration() {
    var result = Parse("module Main { let foo = (x: int, y: int): void => {}; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestArrayMethodParamDeclaration() {
    var result = Parse("module Main { let foo = (x: int[]): void => {}; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  // Block Statements
  [TestMethod]
  public Task TestBasicBlock() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        let x: int = 0;
        x = 1 + 1;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestVarOnlyBasicBlock() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        let x: int = 0, y: int = 1;
      };
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
      let testMethod = (): void => {
        x = 1 + 1;
      };
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
      let x: int = 0;
      x = 1;
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexAssignmentStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        Base.x = 1;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleCallStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        foo();
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexCallStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        Base.foo();
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSingleArgCallStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        foo(1);
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestMultiArgCallStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        foo(1, 2);
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestNestedArgCallStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        foo(foo());
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCalloutStatement() {
    var result = Parse("module Main { let testMethod = (): void => { @test(1, 2, y); }; }");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleIfStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        if (true) {
          x = 1;
        }
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexIfStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        let x: int = 0;
        if (true) {
          x = 1;
        } else {
          x = 2;
        }
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestWhileStatement() {
    var result = Parse(@"
    module Main {
      while (true) {
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
      let testMethod = (): void => {
        return;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestComplexReturnStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): int => {
        return 1;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestContinueStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        while (true) {
          continue;
        }
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBreakStatement() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        while (true) {
          break;
        }
      };
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
      let testMethod = (): void => {
        x = y;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestDotExpression() {
    return Verify(Parse(@"
    module Main {
      let  testMethod = (): void => { 
        x = Base.y; 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCallExpression1() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = foo(); 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCallExpression2() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = foo(1); 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCallExpression3() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = foo(1, 2); 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCalloutExpression() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => {
        x = @test(1, 2, y);
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestArrayDeclaration() {
    return Verify(Parse(@"
    module Main { 
      let x: int[] = new int[1];
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestIntExpression() {
    return Verify(Parse(@"
    module Main { 
      let x: int = 1;
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestCharExpression() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = 'a'; 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBoolExpression1() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = true; 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBoolExpression2() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = false; 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrefixExpression() {
    return Verify(Parse(@"
    module Main { 
      let booly: boolean = !true;
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBinopExpression1() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = 1 + 1; 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestBinopExpression2() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = 1 + 1 + 1; 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestParenExpression() {
    return Verify(Parse(@"
    module Main { 
      let testMethod = (): void => { 
        x = (1 + 1); 
      };
    }
    "), CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestSimpleExpression() {
    var result = Parse(@"
    module main {
      let testMethod = (): void => {
        x = 5 * (3 + 2);
      };
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
      let testMethod = (): void => {
        x = 1 + 1 * 2;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence2() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        x = 1 + 1 + 2;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence3() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        x = 1 + 1 - 2;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence4() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        x = 1 - 1 + 2;
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestPrecedence5() {
    var result = Parse(@"
    module Main {
      let testMethod = (): void => {
        x = 1 - (1 + 2);
      };
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
      let s: string = ""hello"";
      let x: int = 0;
      let y: boolean = true;
      let z: void = 0;
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
      let x: int = 0, y: int = 0, z: int[] = new int[5];

      let foo = (a: int, b: boolean, c: int[]): void => {
        if (a < 10 && b) {
          x = x + 1;
        } else {
          x = x - 1;
        }
      };

      let bar = (): int => {
        return x;
      };
    }

    module Base {
      let calloutMethod = (): void => {
      };
    }
    ");
    return Verify(result, CreateSettings())
      .IgnoreMembersWithType<Position>();
  }
  #endregion
  // Invalid Tests
  [TestMethod]
  public void TestInvalidStmtNoSemi() {
    Assert.Throws<UnrecognizedTokenException>(() => Parse(@"
    module Main {
      let x = 1
    }
    "));
  }
  [TestMethod]
  public void TestInvalidParenExpression() {
    Assert.Throws<UnrecognizedTokenException>(() => Parse(@"
    module Main { 
      let x = ();
    }
    "));
  }
}
