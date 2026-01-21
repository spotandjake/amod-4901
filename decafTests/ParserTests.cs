using System.Text.Json;
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
  private ParseTree.ProgramNode Parse(string text) {
    var lexer = Compiler.Compiler.LexString(text, null);
    var tokenStream = new Antlr4.Runtime.CommonTokenStream(lexer);
    var program = Compiler.Compiler.ParseTokenStream(tokenStream, null);
    return program;
  }
  #region ValidTests
  // Empty Program
  [TestMethod]
  public Task TestEmpty() {
    // Assert.
    var result = Parse("");
    return Verify(result, CreateSettings());
  }
  // Classes
  [TestMethod]
  public Task TestEmptyBaseClass() {
    // Assert.
    var result = Parse("class Main {}");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestEmptyClass() {
    // Assert.
    var result = Parse("class Main extends Base {}");
    return Verify(result, CreateSettings());
  }
  [TestMethod]
  public Task TestEmptyMultiClass() {
    // Assert.
    var result = Parse("class Main {} class Main2 {}");
    return Verify(result, CreateSettings());
  }
  // [TestMethod]
  // public Task TestKitchenSinkParsing() {
  //   // Assert.
  //   var result = Parse("class Main extends Test { int x; }");
  //   return Verify(result);
  // }
  #endregion
}
