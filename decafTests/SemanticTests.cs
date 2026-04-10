using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.IR.ParseTree;
using Decaf.Utils.Errors.ScopeErrors;
using Decaf.Utils.Errors.SemanticErrors;

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
  public Task TestBasicProgram() {
    var program = SemanticAnalysis(@"
      module Program {
        void Main() {}
      }
      module Second {
        void Hello() {}
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.IsTrue(program.Scope.HasDeclaration("Second"));
    return Verify(program.Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestBasicModule() {
    var program = SemanticAnalysis(@"
      module Program {
        int a;
        int b;
        void Main() {}
        void foo() {}
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Modules);
    Assert.IsNotNull(program.Modules[0]);
    Assert.AreEqual("Program", program.Modules[0].Name);
    return Verify(program.Modules[0].Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestMultipleBinds() {
    var program = SemanticAnalysis(@"
      module Program {
        int a, b, c, d;
        void Main() {}
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Modules);
    Assert.IsNotNull(program.Modules[0]);
    Assert.AreEqual("Program", program.Modules[0].Name);
    return Verify(program.Modules[0].Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestManyFunctions() {
    var program = SemanticAnalysis(@"
      module Program {
        void Main() {}
        void Main1() {}
        void Main2() {}
        void Main3() {}
        void Main4() {}
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Modules);
    Assert.IsNotNull(program.Modules[0]);
    Assert.AreEqual("Program", program.Modules[0].Name);
    return Verify(program.Modules[0].Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestNestedScopes() {
    var program = SemanticAnalysis(@"
      module Program {
        int a;
        void Main() {
          int b;
          a = 1;
          b = 2;
          if (true) {
            int c;
            c = 3;}
        }
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Modules);
    var programModule = program.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.AreEqual("Program", programModule.Name);
    Assert.HasCount(1, programModule.Fields);
    Assert.HasCount(1, programModule.Methods);
    var mainMethod = programModule.Methods[0];
    Assert.IsNotNull(mainMethod);
    Assert.AreEqual("Main", mainMethod.Name);
    Assert.HasCount(3, mainMethod.Body.Statements);
    Assert.IsTrue(mainMethod.Body.Statements[0] is StatementNode);
    Assert.IsTrue(mainMethod.Body.Statements[1] is StatementNode);
    Assert.IsTrue(mainMethod.Body.Statements[2] is StatementNode.IfNode);
    var ifNode = mainMethod.Body.Statements[2] as StatementNode.IfNode;
    Assert.IsNotNull(ifNode);
    return Verify(ifNode.TrueBranch.Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestRedefinitionNested() {
    var program = SemanticAnalysis(@"
      module Program {
        int a;
        void Main() {
          int a;
          if (true) {
            int a;
          }
        }
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Modules);
    var programModule = program.Modules[0];
    Assert.IsNotNull(programModule);
    Assert.AreEqual("Program", programModule.Name);
    Assert.HasCount(1, programModule.Fields);
    Assert.HasCount(1, programModule.Methods);
    var mainMethod = programModule.Methods[0];
    Assert.IsNotNull(mainMethod);
    Assert.AreEqual("Main", mainMethod.Name);
    Assert.HasCount(1, mainMethod.Body.Statements);
    Assert.IsTrue(mainMethod.Body.Statements[0] is StatementNode.IfNode);
    var IfNode = mainMethod.Body.Statements[0] as StatementNode.IfNode;
    return Verify(IfNode.TrueBranch.Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public void TestDuplicateModule() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {}
        }
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
          int a;
          int a;
          void Main() {}
        }
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateVariable2() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {
            int a;
            int a;
          }
        }
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateVariable3() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {
            int a, a, b;
          }
        }
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateMethod() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {}
          void Foo() {}
          void Foo() {}
        }
      ");
    });
  }
  [TestMethod]
  public void TestUndefined1() {
    Assert.Throws<DeclarationNotDefinedException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {
            x = 1;
          }
        }
      ");
    });
  }
  [TestMethod]
  public void TestUndefined2() {
    Assert.Throws<DeclarationNotDefinedException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {
            int x;
          }
          void foo() {
            x = 1;
          }
        }
      ");
    });
  }
  [TestMethod]
  public void TestUndefined3() {
    Assert.Throws<DeclarationNotDefinedException>(() => {
      SemanticAnalysis(@"
        module Program {
          void Main() {
            Second.main();
          }
        }
      ");
    });
  }
  [TestMethod]
  public void TestMutation1() {
    try {
      SemanticAnalysis(@"
        module Program {
          int x;
          void Main() {
            Program.x = 1;
          }
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
          void Main() {
          }
          void add(int x) {
            x = 1;
          }
        }
      ");
    });
  }
  #endregion
  #region SemanticTests
  // Program.Main Checks
  [TestMethod]
  public void TestValidSemanticProgramMain() {
    try {
      SemanticAnalysis(@"
      module Program {
        void Main() {}
      }
    ");
    }
    catch {
      Assert.Fail("Semantic analysis threw an exception on a valid program.");
    }
  }
  [TestMethod]
  public void TestInValidSemanticNoProgram() {
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {}
    ");
    });
  }
  [TestMethod]
  public void TestInValidSemanticNoMain() {
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Program {}
    ");
    });
  }
  [TestMethod]
  public void TestInValidSemanticMainNoVoid() {
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Program {
        int Main() {}
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidSemanticMainArgs() {
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Program {
        void Main(int args) {}
      }
    ");
    });
  }
  // Loop Checks
  [TestMethod]
  public void TestValidSemanticLoops() {
    try {
      SemanticAnalysis(@"
      module Program {
        void Main() {
          while (true) {
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
  public void TestValidSemanticNestedLoops() {
    try {
      SemanticAnalysis(@"
      module Program {
        void Main() {
          while (true) {
            if (true) {
              continue;
              break;
            }
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
        void Main() {
          continue;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidSemanticBreak() {
    Assert.Throws<SemanticException>(() => {
      SemanticAnalysis(@"
      module Main {
        void Main() {
          break;
        }
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
        void Main() {
          int x;
          x = 1 / 2;
          x = 0 / 1;
        }
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
        void Main() {
          int x;
          x = 1 / 0;
        }
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
        void Main() {
          int a[];
          int x;
          a = new int[5];
          a = new int[0];
          a = new int[x];
        }
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
        void Main() {
          int a[];
          a = new int[-1];
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidArrayIndex() {
    try {
      SemanticAnalysis(@"
      module Program {
        void Main() {
          int a[];
          a = new int[5];
          a[0] = 1;
        }
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
        void Main() {
          int a[];
          a = new int[-1];
          a[-1] = 1;
        }
      }
    ");
    });
  }
  #endregion
}
