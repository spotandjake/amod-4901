using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

using Decaf.IR.ParseTree;
using Decaf.Utils.Errors.ScopeErrors;

[TestClass]
public class DecafSemanticTests : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory(System.IO.Path.Combine("Snapshots", nameof(DecafParserTests)));
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
      class Program {
        void Main() {}
      }
      class Second {
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
  public Task TestBasicClass() {
    var program = SemanticAnalysis(@"
      class Program {
        int a;
        int b;
        void Main() {}
        void foo() {}
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Classes);
    Assert.IsNotNull(program.Classes[0]);
    Assert.AreEqual("Program", program.Classes[0].Name);
    return Verify(program.Classes[0].Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestMultipleBinds() {
    var program = SemanticAnalysis(@"
      class Program {
        int a, b, c, d;
        void Main() {}
      }
    ");
    Assert.IsNotNull(program);
    Assert.IsNotNull(program.Scope);
    Assert.IsTrue(program.Scope.HasDeclaration("Program"));
    Assert.HasCount(1, program.Classes);
    Assert.IsNotNull(program.Classes[0]);
    Assert.AreEqual("Program", program.Classes[0].Name);
    return Verify(program.Classes[0].Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestManyFunctions() {
    var program = SemanticAnalysis(@"
      class Program {
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
    Assert.HasCount(1, program.Classes);
    Assert.IsNotNull(program.Classes[0]);
    Assert.AreEqual("Program", program.Classes[0].Name);
    return Verify(program.Classes[0].Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public Task TestNestedScopes() {
    var program = SemanticAnalysis(@"
      class Program {
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
    Assert.HasCount(1, program.Classes);
    var programClass = program.Classes[0];
    Assert.IsNotNull(programClass);
    Assert.AreEqual("Program", programClass.Name);
    Assert.HasCount(1, programClass.Fields);
    Assert.HasCount(1, programClass.Methods);
    var mainMethod = programClass.Methods[0];
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
      class Program {
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
    Assert.HasCount(1, program.Classes);
    var programClass = program.Classes[0];
    Assert.IsNotNull(programClass);
    Assert.AreEqual("Program", programClass.Name);
    Assert.HasCount(1, programClass.Fields);
    Assert.HasCount(1, programClass.Methods);
    var mainMethod = programClass.Methods[0];
    Assert.IsNotNull(mainMethod);
    Assert.AreEqual("Main", mainMethod.Name);
    Assert.HasCount(1, mainMethod.Body.Statements);
    Assert.IsTrue(mainMethod.Body.Statements[0] is StatementNode.IfNode);
    var IfNode = mainMethod.Body.Statements[0] as StatementNode.IfNode;
    return Verify(IfNode.TrueBranch.Scope.ToString(), CreateSettings());
  }
  [TestMethod]
  public void TestDuplicateClass() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        class Program {
          void Main() {}
        }
        class Duplicate {}
        class Duplicate {}
      ");
    });
  }
  [TestMethod]
  public void TestDuplicateVariable1() {
    Assert.Throws<DuplicateDeclarationException>(() => {
      SemanticAnalysis(@"
        class Program {
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
        class Program {
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
        class Program {
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
        class Program {
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
        class Program {
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
        class Program {
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
        class Program {
          void Main() {
            Second.main();
          }
        }
      ");
    });
  }
  #endregion
  #region SemanticTests
  // TODO: Add semantic analysis tests
  #endregion
}
