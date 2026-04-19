using VerifyMSTest;

using Decaf.Compiler;
using Decaf.IR.TypedTree;
using Decaf.Utils;
using Decaf.Utils.Errors.TypeCheckingErrors;

[TestClass]
public class DecafTypeCheckerTests : VerifyBase {
  private static ProgramNode TypeCheck(string text) {
    var frontEndProgram = Compiler.FrontEnd(new CompilationConfig {
      // Global Config
      SkipOptimizationPasses = [],
      UseStartSection = false,
      // Module Config
      BundleRuntime = false
    }, text, null);
    var typeCheckedProgram = Compiler.TypeCheck(frontEndProgram);
    return typeCheckedProgram;
  }
  // Simple Checks
  [TestMethod]
  public void TestSimpleProgram() {
    // Simply testing that we don't throw
    try {
      TypeCheck(@"
      module Program {}
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
      module Program {
        let x: int = true;
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
      module Program {
        let add = (a: int, b: int): int => {
          return a + b;
        };
        let x: int = add(1, 2);
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
      module Program {
        let add = (a: int, b: int): int => {
          return a + b;
        };
        let x: int = add(1);
      }
    ");
    });
  }
  [TestMethod]
  public void TestMisMatchParams() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let add = (a: int, b: boolean): int => {
          return a + b;
        };
        let x: int = add(1, 2);
      }
    ");
    });
  }
  [TestMethod]
  public void TestMisMatchReturn() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let add = (a: int, b: int): boolean => {
          return a + b;
        };
        let x: boolean = add(1, 2);
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
      module Program {
        let add = (a: int, b: int): void => {
          return a + b;
        };
        let x: void = add(1, 2);
      }
    ");
    });
  }
  [TestMethod]
  public void TestNoReturnUnexpected() {
    Assert.Throws<InvalidVoidBind>(() => {
      TypeCheck(@"
      module Program {
        let add = (a: int, b: int): void => {
          return;
        };
        let x: void = add(1, 2);
      }
    ");
    });
  }
  [TestMethod]
  public void ReturnExpectedButNoReturn() {
    // NOTE: We don't explicitly say that void method can't return a value,
    //   however void isn't a value so it would be impossible to match the type.
    Assert.Throws<NoReturnStatement>(() => {
      TypeCheck(@"
      module Program {
        let add = (a: int, b: int): int => {};
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
      module Program {
        let add = (a: int, b: int): void => {
          return;
        };
        add(1, 2);
      }
    ");
    }
    catch {
      Assert.Fail("TypeChecking failed on void value with a return.");
    }
  }
  [TestMethod]
  public void TestCallHasResult() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let add = (a: int, b: int): void => {
          return a + b;
        };
      }
    ");
    });
  }
  // Arithmetic Tests
  [TestMethod]
  public void TestValidArithmetic() {
    try {
      TypeCheck(@"
      module Program {
        let a: int = 1 + 1;
        let b: int = 1 - 1;
        let c: int = 1 * 1;
        let d: int = 1 / 1;
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
      module Program {
        let a: int = 1 + 1 - 1 * 1 /1;
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
      module Program {
        let a: int = 1 + true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidArithmeticSub() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: int = 1 - true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidArithmeticMul() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: int = 1 * true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidArithmeticDiv() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: int = 1 / true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidCompoundArithmetic() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 + 1 - 1 * 1 / 1;
      }
    ");
    });
  }
  // Relational Tests
  [TestMethod]
  public void TestValidRelational() {
    try {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 < 1;
        let b: boolean = 1 > 1;
        let c: boolean = 1 <= 1;
        let d: boolean = 1 >= 1;
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
      module Program {
        let a: boolean = 1 < true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalGt() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 > true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalLe() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 <= true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalGe() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 >= true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidRelationalOut() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: int = 1 < 1;
      }
    ");
    });
  }
  // Equality Tests
  [TestMethod]
  public void TestValidEquality() {
    try {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 == 1;
        let b: boolean = 1 != 1;
        let c: boolean = true == true;
        let d: boolean = true != true;
        let e: boolean = 'c' == 'c';
        let f: boolean = 'c' != 'c';
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
      module Program {
        let a: boolean = 1 == true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidEquality2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 != true;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidEquality3() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 != 'c';
      }
    ");
    });
  }
  // Conditional Operators
  [TestMethod]
  public void TestValidConditional() {
    try {
      TypeCheck(@"
      module Program {
        let a: boolean = true && true;
        let b: boolean = true || true;
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
      module Program {
        let a: boolean = 1 && 1;
      }
    ");
    });
  }
  [TestMethod]
  public void TestInValidConditional2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module Program {
        let a: boolean = 1 || 1;
      }
    ");
    });
  }
  // Prefix Tests
  [TestMethod]
  public void TestValidPrefix() {
    try {
      TypeCheck(@"
      module Program {
        let a: boolean = !true;
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
      module Program {
        let a: boolean = !1;
      }
    ");
    });
  }
  // Test If Statements - Condition must be bool
  [TestMethod]
  public void TestValidIf() {
    try {
      TypeCheck(@"
      module Program {
        let Main = (): void => {
          if (true) {
            return;
          }
        };
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
      module Program {
        let Main = (): void => {
          if (1) {
            return;
          }
        };
      }
    ");
    });
  }
  // Test While Statements - Condition must be bool
  [TestMethod]
  public void TestValidWhile() {
    try {
      TypeCheck(@"
      module Program {
        let Main = (): void => {
          while (true) {
            return;
          }
        };
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
      module Program {
        let Main = (): void => {
          while (1) {
            return;
          }
        };
      }
    ");
    });
  }
  // Test Locations
  [TestMethod]
  public void TestValidSimpleLocation() {
    try {
      TypeCheck(@"
      module Program {
        let add = (): void => {};
        add();
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
      module Program {
        let add: int = 0;
        add();
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidComplexLocation() {
    try {
      TypeCheck(@"
      module A {
        let x: int = 0;
        let add = (): int => {
          return 1;
        };
      }
      module Program {
        let x: int = A.x;
        let y: int = A.add();
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
      module A {}
      module Program {
        let x: int = A.x;
        A.add();
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidComplexLocation2() {
    Assert.Throws<LhsNotRhs>(() => {
      TypeCheck(@"
      module A {
        let add = (): int => {
          return 1;
        };
      }
      module Program {
        let y: boolean = A.add();
      }
    ");
    });
  }
  // Array
  [TestMethod]
  public void TestValidArrayLocation() {
    try {
      TypeCheck(@"
      module Program {
        let x: int[] = new int[1];
        let Main = (): void => {
          let y: int = x[1];
        };
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
      module Program {
        let x: int = 0;
        let y: int = x[1];
      }
    ");
    });
  }
  [TestMethod]
  public void TestValidArrayAccess() {
    try {
      TypeCheck(@"
      module Program {
        let x: int[] = new int[1];
        let Main = (): void => {
          let y: int = x[1];
        };
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
      module Program {
        let x: int[] = new int[1];
        let Main = (): void => {
          let y: int = x[true];
        };
      }
    ");
    });
  }
  // Test Array Initialization
  [TestMethod]
  public void TestValidArrayInitialization() {
    try {
      TypeCheck(@"
      module A {}
      module Program {
        let b: boolean[] = new boolean[1];
        let c: int[] = new int[1];
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
      module Program {
        let a: int[] = new int[true];
      }
    ");
    });
  }
  // Test Return Statements
  [TestMethod]
  public void TestValidReturnStatement1() {
    try {
      TypeCheck(@"
      module Program {
        let add = (x: int): int => {
           if (true) {
             return 1;
           } else {
             return 2;
           }
        };
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
      module Program {
        let add = (x: int): int => {
          if (true) {
            return 1;
          }
          return 2;
        };
      }
    ");
    }
    catch {
      Assert.Fail("Type checking threw an exception on valid return program");
    }
  }
  [TestMethod]
  public void TestInvalidReturnStatement1() {
    Assert.Throws<NoReturnStatement>(() => {
      TypeCheck(@"
      module Program {
        let add = (x: int): int => {};
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidReturnStatement2() {
    Assert.Throws<NoReturnStatement>(() => {
      TypeCheck(@"
      module Program {
        let add = (x: int): int => {
          if (true) {
            return 1;
          }
        };
      }
    ");
    });
  }
  [TestMethod]
  public void TestInvalidReturnStatement3() {
    Assert.Throws<NoReturnStatement>(() => {
      TypeCheck(@"
      module Program {
        let add = (x: int): int => {
          while (true) {
           return 1;
          }
        };
      }
    ");
    });
  }
}
