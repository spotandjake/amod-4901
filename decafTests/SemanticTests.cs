using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.IR.ParseTree;
using Decaf.Utils.Errors.ScopeErrors;
using Decaf.Utils.Errors.SemanticErrors;

// TODO: Reimplement these semantic tests

[TestClass]
public class DecafSemanticTests : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Semantic/");
    return settings;
  }
  private ProgramNode SemanticAnalysis(string text) {
    var lexer = Compiler.Compiler.LexString(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.Compiler.ParseTokenStream(tokenStream);
    var scopedProgram = Compiler.Compiler.SemanticAnalysis(program);
    return scopedProgram;
  }
  #region ScopeTests
  [TestMethod]
  public void TestBasicValidScope() {
    try {
      SemanticAnalysis(@"
        module Program {
        }
        module Second {
          let hello = (): void => {};
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestComplexValidScope() {
    try {
      SemanticAnalysis(@"
        module Program {
          let a: int = 1;
          let b: int = 2;
          let main = (): void => {};
          let foo = (): void => {};
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestMultipleBindsValidScope() {
    try {
      SemanticAnalysis(@"
        module Program {
          let a: int = 1, b: int = 2, c: int = 3, d: int = 4;
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestManyFunctionsValidScope() {
    try {
      SemanticAnalysis(@"
        module Program {
          let main = (): void => {};
          let main1 = (): void => {};
          let main2 = (): void => {};
          let main3 = (): void => {};
          let main4 = (): void => {};
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestNestedValidScope() {
    try {
      SemanticAnalysis(@"
        module Program {
          let a: int = 1;
          let main = (): void => {
            let b: int = 2;
            b = 2;
            if (true) {
              let c: int = 3;
              c = 3;
            }
          };
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestRedefinitionNestedValidScope() {
    try {
      SemanticAnalysis(@"
        module Program {
          let a: int = 1;
          let main = (): void => {
            let a: int = 2;
            if (true) {
              let a: int = 3;
            }
          };
        }
      ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestDuplicateModuleInvalidScope() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {}
        module Duplicate {}
        module Duplicate {}
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateVariable1() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          let a: int = 1;
          let a: int = 2;
        }
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateVariable2() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          let main = (): void => {
            let a: int = 1;
            let a: int = 2;
          };
        }
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateVariable3() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          let a: int = 1, a: int = 2, b: int = 3;
        }
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateMethod() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          let main = (): void => {};
          let foo = (): void => {};
          let foo = (): void => {};
        }
      ");
    });
  }
  [TestMethod]
  public void TestUndefined1() {
    Assert.Throws<DeclarationNotDefinedException>(() => {
      SemanticAnalysis(@"
        module Program {
          x = 1;
        }
      ");
    });
  }
  [TestMethod]
  public void TestUndefined2() {
    Assert.Throws<DeclarationNotDefinedException>(() => {
      SemanticAnalysis(@"
        module Program {
          let main = (): void => {
            let x: int = 1;
          };
          let foo = (): void => {
            x = 1;
          };
        }
      ");
    });
  }
  [TestMethod]
  public void TestUndefined3() {
    Assert.Throws<DeclarationNotDefinedException>(() => {
      SemanticAnalysis(@"
        module Program {
          Second.main();
        }
      ");
    });
  }
  [TestMethod]
  public void TestMutation1() {
    try {
      SemanticAnalysis(@"
        module Program {
          let x: int = 1;
          let main = (): void => {
            x = 1;
          };
        }
      ");
    }
    catch (DeclarationNotMutableException) {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestMutation2() {
    Assert.Throws<DeclarationNotMutableException>(() => {
      SemanticAnalysis(@"
        module Program {
          let add = (x: int): void => {
            x = 1;
          };
        }
      ");
    });
  }
  #endregion
  #region SemanticTests
  // Loop Checks
  [TestMethod]
  public void TestValidSemanticLoops() {
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
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {
        continue;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidSemanticBreak() {
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {
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
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {
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
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {
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
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {
        let a: int[] = new int[-1];
        a[-1] = 1;
      }
    ");
    });
  }
  #endregion
}
