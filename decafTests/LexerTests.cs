using Antlr4.Runtime;

[TestClass]
public class DecafTests {
  private DecafLexer Setup(string text) {
    AntlrInputStream inputStream = new AntlrInputStream(text);
    DecafLexer lexer = new DecafLexer(inputStream);
    return lexer;
  }

  [TestMethod]
  public void TestBasic() {
    DecafLexer lexer = Setup("john says \"hello\" \n michael says \"world\" \n");
    // Lexer.ChatContext context = parser.chat();
    // BasicSpeakVisitor visitor = new BasicSpeakVisitor();
    // visitor.Visit(context);
    // Assert.AreEqual(2, visitor.Lines.Count);
  }
}
