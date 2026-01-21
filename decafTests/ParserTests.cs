using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

[TestClass]
public class DecafParserTests :
    VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory(System.IO.Path.Combine("Snapshots", nameof(DecafParserTests)));
    return settings;
  }
  #nullable enable
  private ParseTree.ProgramNode? Parse(string text) {
    var lexer = Compiler.Compiler.LexString(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.Compiler.ParseTokenStream(tokenStream, null);
    return program;
  }
  #region ValidTests
  // Empty Program
  [TestMethod]
  public void TestEmpty() {
    // Assert.
    Assert.Throws<Antlr4.Runtime.Misc.ParseCanceledException>(() => Parse(""));
  }
  // Classes
  [TestMethod]
  public Task TestEmptyBaseClass() {
    var result = Parse("class Main {}");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestEmptyClass() {
    var result = Parse("class Main extends Base {}");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestEmptyMultiClass() {
    var result = Parse("class Main {} class Main2 {}");
    return Verify(result, CreateSettings());
  }
  // Class Variable Declarations
  [TestMethod]
  public Task TestSingleVariableDeclaration() {
    var result = Parse("class Main { int x; }");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestMultiVariableDeclaration() {
    var result = Parse("class Main { int x; int y; }");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestMultiBindsDeclaration() {
    var result = Parse("class Main { int x, y, z; }");
    return Verify(result, CreateSettings());
  }
  // TODO: Array Variable
  // Class Method Declarations
  [TestMethod]
  public Task TestSingleBasicMethodDeclaration() {
    var result = Parse("class Main { void foo() {} }");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestMultiBasicMethodDeclaration() {
    var result = Parse("class Main { void foo() {} void bar() {} }");
    return Verify(result, CreateSettings());
  }
  // TODO: Method Parameters
  // [TestMethod]
  // public Task TestMethodSingleParamDeclaration() {
  //   var result = Parse("class Main { void foo(int x) {} }");
  //   return Verify(result, CreateSettings());
  // }
  // [TestMethod]
  // public Task TestMethodMultiParamDeclaration() {
  //   var result = Parse("class Main { void foo(int x, int y) {} }");
  //   return Verify(result, CreateSettings());
  // }
  // TODO: Array Method Param Type

  // TODO: Block Statements
  // TODO: Statements
  // TODO: Expressions
  // TODO: Types

  // TODO: Full Stress Test Programs
  #endregion
  // TODO: Invalid Tests
}
