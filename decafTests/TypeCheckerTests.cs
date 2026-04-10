using VerifyMSTest;
using VerifyTests;

using Decaf.IR.TypedTree;
using Decaf.Utils.Errors.TypeCheckingErrors;

[TestClass]
public class DecafTypeCheckerTests : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/TypeChecker/");
    return settings;
  }
  private static ProgramNode TypeCheck(string text) {
    var lexer = Compiler.Compiler.LexString(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.Compiler.ParseTokenStream(tokenStream);
    var scopedProgram = Compiler.Compiler.SemanticAnalysis(program);
    var typeCheckedProgram = Compiler.Compiler.TypeChecking(scopedProgram);
    return typeCheckedProgram;
  }
  // Simple Checks
  [TestMethod]
  public void TestSimpleProgram() {
    // Simply testing that we don't throw
    try {
      TypeCheck(@"
      class Program {
        void Main() {}
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on a valid program");
    }
  }
  // Variables
  [TestMethod]
  public void TestMisMatchVar() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int x;
          x = true;
        }
      }
    ");
    });
  }
  // Method Tests
  [TestMethod]
  public void TestValidMethod() {
    // Simply testing that we don't throw
    try {
      TypeCheck(@"
      class Program {
        int add(int a, int b) {
          return a + b;
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on a valid program");
    }
  }
  [TestMethod]
  public void TestLessParams() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        int add(int a, int b) {
          return a + b;
        }
        void Main() {
          int x;
          x = add(1);
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestMisMatchParams() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        int add(int a, boolean b) {
          return a + b;
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestMisMatchReturn() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        boolean add(int a, int b) {
          return a + b;
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestNoReturnExpected() {
    // NOTE: We don't explicitly say that void method can't return a value,
    //   however void isn't a value so it would be impossible to match the type.
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void add(int a, int b) {
          return a + b;
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestNoReturnUnexpected() {
    // NOTE: We don't explicitly say that void method can't return a value,
    //   however void isn't a value so it would be impossible to match the type.
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        int add(int a, int b) {
          return;
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    });
  }
  [TestMethod]
  public void ReturnExpectedButNoReturn() {
    // NOTE: We don't explicitly say that void method can't return a value,
    //   however void isn't a value so it would be impossible to match the type.
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        int add(int a, int b) {
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestProperNoReturns() {
    // NOTE: We don't explicitly say that void method can't return a value,
    //   however void isn't a value so it would be impossible to match the type.
    try {
      TypeCheck(@"
      class Program {
        void add(int a, int b) {
          return;
        }
        void Main() {
          int x;
          add(1, 2);
        }
      }
    ");
    }
    catch {
      Assert.Fail("TypeChecking failed on void value with a return.");
    }
  }
  [TestMethod]
  public void TestCallHasResult() {
    // NOTE: We don't explicitly say if an expr we expect a return other than void, 
    //   however an expr can never be of type void
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void add(int a, int b) {
          return a + b;
        }
        void Main() {
          int x;
          x = add(1, 2);
        }
      }
    ");
    });
  }
  // Arithmetic Tests
  [TestMethod]
  public void TestValidArithmetic() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          int a, b, c, d;
          a = 1 + 1;
          b = 1 - 1;
          c = 1 * 1;
          d = 1 / 1;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid arithmetic program");
    }
  }
  [TestMethod]
  public void TestValidArithmeticCompound() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          int a;
          a = 1 + 1 - 1 * 1 /1;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid arithmetic program");
    }
  }
  [TestMethod]
  public void TestInvalidArithmeticAdd() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int a;
          a = 1 + true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidArithmeticSub() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int a;
          a = 1 - true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidArithmeticMul() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int a;
          a = 1 * true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidArithmeticDiv() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int a;
          a = 1 / true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidCompoundArithmetic() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 + 1 - 1 * 1 / 1;
        }
      }
    ");
    });
  }
  // Relational Tests
  [TestMethod]
  public void TestValidRelational() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a, b, c, d;
          a = 1 < 1;
          b = 1 > 1;
          c = 1 <= 1;
          d = 1 >= 1;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid relational program");
    }
  }
  [TestMethod]
  public void TestInvalidRelationalLt() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 < true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalGt() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 > true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalLe() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 <= true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalGe() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 >= true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalOut() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int a;
          a = 1 < 1;
        }
      }
    ");
    });
  }
  // Equality Tests
  [TestMethod]
  public void TestValidEquality() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 == 1;
          a = 1 != 1;
          a = true == true;
          a = true != true;
          a = 'c' == 'c';
          a = 'c' != 'c';
          a = null == null;
          a = null != null;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid equality program");
    }
  }
  [TestMethod]
  public void TestInValidEquality1() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 == true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidEquality2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 != true;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidEquality3() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 != 'c';
        }
      }
    ");
    });
  }
  // Conditional Operators
  [TestMethod]
  public void TestValidConditional() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = true && true;
          a = true || true;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid conditional program");
    }
  }
  [TestMethod]
  public void TestInValidConditional1() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 && 1;
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidConditional2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = 1 || 1;
        }
      }
    ");
    });
  }
  // Prefix Tests
  [TestMethod]
  public void TestValidPrefix() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = !true;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid prefix program");
    }
  }
  [TestMethod]
  public void TestInValidPrefix() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          boolean a;
          a = !1;
        }
      }
    ");
    });
  }
  // Test If Statements - Condition must be bool
  [TestMethod]
  public void TestValidIf() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          if (true) {
            return;
          }
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid if program");
    }
  }
  [TestMethod]
  public void TestInValidIf() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          if (1) {
            return;
          }
        }
      }
    ");
    });
  }
  // Test While Statements - Condition must be bool
  [TestMethod]
  public void TestValidWhile() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
          while (true) {
            return;
          }
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid while program");
    }
  }
  [TestMethod]
  public void TestInValidWhile() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          while (1) {
            return;
          }
        }
      }
    ");
    });
  }
  // Test Locations
  [TestMethod]
  public void TestValidSimpleLocation() {
    try {
      TypeCheck(@"
      class Program {
        void add() {}
        void Main() {
          add();
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid location program");
    }
  }
  [TestMethod]
  public void TestInvalidSimpleLocation() {
    Assert.Throws<CallOnNonMethod>(() => {
      TypeCheck(@"
      class Program {
        int add;
        void Main() {
          add();
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidComplexLocation() {
    try {
      TypeCheck(@"
      class A {
        int x;
        int add() {
          return 1;
        }
      }
      class Program {
        void Main() {
          int x, y;
          x = A.x;
          y = A.add();
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid location program");
    }
  }
  [TestMethod]
  public void TestInvalidComplexLocation() {
    Assert.Throws<MemberAccessUnknown>(() => {
      TypeCheck(@"
      class A {
      }
      class Program {
        void Main() {
          int x;
          x = A.x;
          A.add();
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidComplexLocation2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class A {
        int add() {
          return 1;
        }
      }
      class Program {
        void Main() {
          boolean y;
          y = A.add();
        }
      }
    ");
    });
  }
  // Array
  [TestMethod]
  public void TestValidArrayLocation() {
    try {
      TypeCheck(@"
      class Program {
        int x[];
        void Main() {
          int y;
          y = x[1];
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid array location program");
    }
  }
  [TestMethod]
  public void TestInvalidArrayLocation() {
    Assert.Throws<ArrayAccessOnNonArray>(() => {
      TypeCheck(@"
      class Program {
        int x;
        void Main() {
          int y;
          y = x[1];
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidArrayAccess() {
    try {
      TypeCheck(@"
      class Program {
        int x[];
        void Main() {
          int y;
          y = x[1];
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid array location program");
    }
  }
  [TestMethod]
  public void TestInvalidArrayAccess() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        int x[];
        void Main() {
          int y;
          y = x[true];
        }
      }
    ");
    });
  }
  // Class Initialization
  [TestMethod]
  public void TestValidClassInitialization() {
    try {
      TypeCheck(@"
      class A {}
      class Program {
        void Main() {
          A y;
          y = new A();
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid class initializations program");
    }
  }
  [TestMethod]
  public void TestInvalidClassInitialization1() {
    Assert.Throws<Decaf.Utils.Errors.ScopeErrors.DeclarationNotDefinedException>(() => {
      TypeCheck(@"
      class A {}
      class Program {
        void Main() {
          B y;
          y = new B();
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidClassInitialization2() {
    Assert.Throws<Decaf.Utils.Errors.ScopeErrors.DeclarationNotDefinedException>(() => {
      TypeCheck(@"
      class A {}
      class Program {
        void Main() {
          B y;
          y = new t();
        }
      }
    ");
    });
  }
  // Test Array Initialization
  [TestMethod]
  public void TestValidArrayInitialization() {
    try {
      TypeCheck(@"
      class A {}
      class Program {
        void Main() {
          A a[];
          boolean b[];
          int c[];
          a = new A[1];
          b = new boolean[1];
          c = new int[1];
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid array initializations program");
    }
  }
  [TestMethod]
  public void TestInvalidArrayInitialization() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
          int a[];
          a = new int[true];
        }
      }
    ");
    });
  }
  // Test Return Statements
  [TestMethod]
  public void TestValidReturnStatement1() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
        }
        int add(int x) {
          if (true) {
            return 1;
          } else {
            return 2;
          }
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid return program");
    }
  }
  [TestMethod]
  public void TestValidReturnStatement2() {
    try {
      TypeCheck(@"
      class Program {
        void Main() {
        }
        int add(int x) {
          if (true) {
            return 1;
          }
          return 2;
        }
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid return program");
    }
  }
  [TestMethod]
  public void TestInvalidReturnStatement1() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
        }
        int add(int x) {
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidReturnStatement2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
        }
        int add(int x) {
          if (true) {
            return 1;
          }
        }
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidReturnStatement3() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      class Program {
        void Main() {
        }
        int add(int x) {
          while (true) {
           return 1;
          }
        }
      }
    ");
    });
  }
}
