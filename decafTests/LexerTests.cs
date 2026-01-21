[TestClass]
public class DecafLexerTests {
  private DecafLexer Lex(string text) {
    return Compiler.Compiler.LexString(text, null); ;
  }
  // NOTE: There isn't a ton of testing on the lexer as it is rather basic in operation.
  //   Most of the stress testing of the lexer will happen during parser testing, as it 
  //   will handle larger programs.

  // Unit Testing
  [TestMethod]
  public void TestKeywords() {
    DecafLexer lexer = Lex("boolean callout class else extends false if int new null return this true void while");
    Assert.AreEqual(DecafLexer.BOOLEAN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CALLOUT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CLASS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.ELSE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.EXTENDS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.FALSE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.IF, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.NEW, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.NULL, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RETURN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.THIS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.TRUE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.VOID, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.WHILE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestOperators() {
    string testString = "() {} [] ; , . ! + - * / <= >= < > == != && || =";
    DecafLexer lexer = Lex(testString);
    Assert.AreEqual(DecafLexer.LPAREN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RPAREN, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.LBRACE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RBRACE, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.LBRACK, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RBRACK, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.SEMI, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.COMMA, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.DOT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.NOT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.PLUS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.MINUS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.MULT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.DIV, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.LEQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.GEQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.LT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.GT, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.EQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.NEQ, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.AND, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.OR, lexer.NextToken().Type);

    Assert.AreEqual(DecafLexer.ASSIGN, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestLiterals() { }
  [TestMethod]
  public void TestIdentifiers() { }
  [TestMethod]
  public void TestAttributes() { }
  // Snapshot Testing
  [TestMethod]
  public void TestProgram() { }
}
