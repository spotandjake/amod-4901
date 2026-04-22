namespace decafTests.FrontEnd;

using VerifyMSTest;

using Decaf.Compiler;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils.Errors.SemanticErrors;



[TestClass]
public class SemanticTest : VerifyBase {
  private static ParseTree.ProgramNode Test(string text) {
    var lexer = Compiler.LexSource(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.ParseSource(tokenStream);
    var checkedProgram = Compiler.CheckSemantics(program);
    return checkedProgram;
  }
  // --- Module Checks ---
  [TestMethod]
  public void TestValidProgramContainsProgram() {
    try {
      Test(@"
        module Program {}
      ");
    }
    catch {
      Assert.Fail("Module checks threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInvalidProgramNoProgramOtherModule() {
    Assert.Throws<ProgramModuleNotFound>(() => {
      Test("module NotProgram {}");
    });
  }
  // --- Loop Checks ---
  [TestMethod]
  public void TestValidBreakInLoop() {
    try {
      Test(@"
      module Program {
        while (true) {break;}
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInvalidBreakOutsideLoop1() {
    Assert.Throws<BreakStatementOutsideOfLoop>(() => {
      Test(@"
      module Program {
        break;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidBreakOutsideLoop2() {
    Assert.Throws<BreakStatementOutsideOfLoop>(() => {
      Test(@"
      module Program {
        if (true) {break;}
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidBreakOutsideLoop3() {
    Assert.Throws<BreakStatementOutsideOfLoop>(() => {
      Test(@"
      module Program {
        let x = (): void => {
          break;
        };
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidContinueInLoop() {
    try {
      Test(@"
      module Program {
        while (true) {continue;}
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInvalidContinueOutsideLoop1() {
    Assert.Throws<ContinueStatementOutsideOfLoop>(() => {
      Test(@"
      module Program {
        continue;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidContinueOutsideLoop2() {
    Assert.Throws<ContinueStatementOutsideOfLoop>(() => {
      Test(@"
      module Program {
        if (true) {continue;}
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidContinueOutsideLoop3() {
    Assert.Throws<ContinueStatementOutsideOfLoop>(() => {
      Test(@"
      module Program {
        let x = (): void => {
          continue;
        };
      }
    ");
    });
  }
  // --- Function Literal Checks ---
  // NOTE: These only exist because we provide first class function syntax without first class functions
  // NOTE: This restriction can be lifted in the future (all we need really is closures)
  [TestMethod]
  public void TestValidFunctionLiteral() {
    try {
      Test(@"
      module Program {
        let x = (): void => {};
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInvalidFunctionLiteral1() {
    Assert.Throws<FunctionLiteralMustBeDirectRhsOfVarDecl>(() => {
      Test(@"
      module Program {
        let x = (): void => {
          let y = (): void => {};
        };
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidFunctionLiteral2() {
    Assert.Throws<FunctionsCanOnlyBeDefinedAtTopLevelOfModule>(() => {
      Test(@"
      module Program {
        if (true) {
          let x = (): void => {};
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidFunctionLiteral3() {
    Assert.Throws<FunctionLiteralMustBeDirectRhsOfVarDecl>(() => {
      Test(@"
      module Program {
        (): void => {};
      }
    ");
    });
  }
  // --- Return Checks ---
  [TestMethod]
  public void TestValidSemanticReturn() {
    try {
      Test(@"
        module Program {
          let x = (): void => {
            return;
          };
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInvalidSemanticReturn1() {
    Assert.Throws<ReturnStatementOutsideOfFunction>(() => {
      Test(@"
      module Program {
        return;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidSemanticReturn2() {
    Assert.Throws<ReturnStatementOutsideOfFunction>(() => {
      Test(@"
      module Program {
        if (true) {
          return;
        }
      }
    ");
    });
  }
  // Divide by 0
  [TestMethod]
  public void TestValidDivide() {
    try {
      Test(@"
      module Program {
        let x: int = 1 / 2;
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidDivideByZero() {
    Assert.Throws<DivisionByZero>(() => {
      Test(@"
      module Program {
        let x: int = 1 / 0;
      }
      ");
    });
  }
  // --- Array negative size checks ---
  [TestMethod]
  public void TestValidArraySize() {
    try {
      Test(@"
      module Program {
        let x: int[] = new int[10];
        let y: int[] = new int[0];
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidArraySize() {
    Assert.Throws<ArraySizeMustBePositive>(() => {
      Test(@"
      module Program {
        let x: int[] = new int[-1];
      }
      ");
    });
  }
  // --- Array index non negative checks ---
  [TestMethod]
  public void TestValidArrayIndex() {
    try {
      Test(@"
      module Program {
        let x: int[] = new int[10];
        x[0] = 1;
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidArrayIndex() {
    Assert.Throws<ArraySizeMustBePositive>(() => {
      Test(@"
      module Program {
        let x: int[] = new int[-1];
        x[-1] = 1;
      }
      ");
    });
  }
}
