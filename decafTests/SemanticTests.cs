using VerifyMSTest;

using Decaf.Compiler;
using Decaf.IR.ParseTree;
using Decaf.Utils.Errors.SemanticErrors;

// TODO: Reimplement these semantic tests

[TestClass]
public class DecafSemanticTests : VerifyBase {
  private ProgramNode SemanticAnalysis(string text) {
    var lexer = Compiler.LexSource(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.ParseSource(tokenStream);
    var scopedProgram = Compiler.CheckSemantics(program);
    return scopedProgram;
  }
  #region SemanticTests
  [TestMethod]
  public void TestInvalidSemanticProgram() {
    try {
      SemanticAnalysis(@"
      module Program {
        while (true) {
          continue;
          break;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  // Loop Checks
  [TestMethod]
  public void TestValidSemanticLoops() {
    Assert.Throws<ProgramModuleNotFound>(() => {
      SemanticAnalysis(@"
      module Main {
        continue;
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidSemanticNestedLoops() {
    try {
      SemanticAnalysis(@"
      module Program {
        while (true) {
          if (true) {
            continue;
            break;
          }
        }
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidSemanticContinue() {
    Assert.Throws<ContinueStatementOutsideOfLoop>(() => {
      SemanticAnalysis(@"
      module Program {
        continue;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidSemanticBreak() {
    Assert.Throws<BreakStatementOutsideOfLoop>(() => {
      SemanticAnalysis(@"
      module Program {
        break;
      }
    ");
    });
  }
  // Arithmetic Checks
  [TestMethod]
  public void TestValidSemanticArithmetic() {
    try {
      SemanticAnalysis(@"
      module Program {
        let x: int = 1;
        x = 1 / 2;
        x = 0 / 1;
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidSemanticArithmetic() {
    Assert.Throws<DivisionByZero>(() => {
      SemanticAnalysis(@"
      module Program {
        let x: int = 1 / 0;
      }
    ");
    });
  }
  // Array Checks
  [TestMethod]
  public void TestValidSemanticArrayInit() {
    try {
      SemanticAnalysis(@"
      module Program {
        let a: int[] = new int[5];
        let x: int = 0;
        a = new int[0];
        a = new int[x];
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidSemanticArrayInit() {
    Assert.Throws<ArraySizeMustBePositive>(() => {
      SemanticAnalysis(@"
      module Program {
        let a: int[] = new int[-1];
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidArrayIndex() {
    try {
      SemanticAnalysis(@"
      module Program {
        let a: int[] = new int[5];
        a[0] = 1;
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidSemanticArrayIndex() {
    Assert.Throws<ArrayIndexMustBeNonNegative>(() => {
      SemanticAnalysis(@"
      module Program {
        let a: int[] = new int[5];
        a[-1] = 1;
      }
    ");
    });
  }
  #endregion
}
