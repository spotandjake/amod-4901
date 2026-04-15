using VerifyMSTest;
using VerifyTests;

using Decaf.Compiler;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils.Errors.ScopeErrors;

namespace decafTests.FrontEnd;


[TestClass]
public class ScopeTest : VerifyBase {
  private static VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Scope/");
    return settings;
  }
  private static ParseTree.ProgramNode Test(string text) {
    var lexer = Compiler.LexSource(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.ParseSource(tokenStream);
    var scopedProgram = Compiler.CheckSemantics(program);
    return scopedProgram;
  }
  // --- Valid Scope ---
  [TestMethod]
  public void TestValidGlobalVariableDefined() {
    try {
      Test(@"
        module Program {
          let a: int = 1;
          let test = (): int => {
            return a;
          };
          test();
        }
      ");
    }
    catch {
      Assert.Fail("Scope: Global variable defined threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestValidLocalVariableDefined() {
    try {
      Test(@"
        module Program {
          let test = (): int => {
            let b: int = 2;
            return b;
          };
          test();
        }
      ");
    }
    catch {
      Assert.Fail("Scope: Local variable defined threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestValidModuleDefined() {
    try {
      Test(@"
        module Second {
          let hello = (): void => {};
        }
        module Program {
          Second.hello();
        }
      ");
    }
    catch {
      Assert.Fail("Scope: Module defined threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestValidMutation() {
    try {
      Test(@"
        module Program {
          let a: int = 1;
          a = 2;
        }
      ");
    }
    catch {
      Assert.Fail("Scope: Mutation threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestValidRecursiveModule() {
    try {
      Test(@"
        module Program {
          let a: int = 1;
          Program.a = 1;
        }
      ");
    }
    catch {
      Assert.Fail("Scope: Recursive module threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestValidRecursiveFunction() {
    try {
      Test(@"
        module Program {
          let test = (): int => {
            return test();
          };
          test();
        }
      ");
    }
    catch {
      Assert.Fail("Scope: Recursive function threw an exception on a valid program.");
    }
  }
  // --- DuplicateDeclarationException ---
  [TestMethod]
  public void TestInvalidDuplicateModule() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DuplicateDeclarationException>(() => {
      Test(@"
        module Duplicate {}
        module Duplicate {}
        module Program {}
      ");
    });
  }
  [TestMethod]
  public void TestInvalidDuplicateFunction() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DuplicateDeclarationException>(() => {
      Test(@"
        module Program {
          let duplicate = (): void => {};
          let duplicate = (): void => {};
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidDuplicateGlobal() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DuplicateDeclarationException>(() => {
      Test(@"
        module Program {
          let duplicate: int = 1;
          let duplicate: int = 1;
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidDuplicateLocal() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DuplicateDeclarationException>(() => {
      Test(@"
        module Program {
          let test = (): void => {
            let duplicate: int = 1;
            let duplicate: int = 1;
          };
          test();
        }
      ");
    });
  }
  // --- DeclarationNotDefinedException ---
  [TestMethod]
  public void TestInvalidVariableNotDefined() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotDefinedException>(() => {
      Test(@"
        module Program {
          notDefined = 1;
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidModuleNotDefined() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotDefinedException>(() => {
      Test(@"
        module Program {
          Test.notDefined = 1;
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidNeighborNotDefined() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotDefinedException>(() => {
      Test(@"
        module Program {
          let test = (): void => {
            notDefined = 1;
          };
          test();
          notDefined = 1;
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidAfterVariableNotDefined() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotDefinedException>(() => {
      Test(@"
        module Program {
          notDefined = 1;
          let notDefined: int = 1;
        }
      ");
    });
  }
  // --- DeclarationNotMutableException ---
  [TestMethod]
  public void TestInvalidParamMutation() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotMutableException>(() => {
      Test(@"
        module Program {
          let test = (x: int): void => {
            x = 1;
          };
          test(1);
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidFunctionMutation() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotMutableException>(() => {
      Test(@"
        module Program {
          let test = (x: int): void => {};
          test = 1;
        }
      ");
    });
  }
  [TestMethod]
  public void TestInvalidPrimitiveMutation() {
    // NOTE: These all cover the same code paths but we test in case we change the implementation in the future
    Assert.Throws<DeclarationNotMutableException>(() => {
      Test(@"
        module Program {
          @wasm.memory.size = 1;
        }
      ");
    });
  }
}
