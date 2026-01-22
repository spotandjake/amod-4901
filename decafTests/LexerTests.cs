using System.Threading.Tasks;
using Antlr4.Runtime;
using VerifyMSTest;
using VerifyTests;
using System.Collections.Generic;
using System.Linq;

[TestClass]
public class DecafLexerTests : VerifyBase {
  private DecafLexer Lex(string text) {
    return Compiler.Compiler.LexString(text, null); ;
  }
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory(System.IO.Path.Combine("Snapshots", nameof(DecafParserTests)));
    return settings;
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
  public void TestLiterals() {
    // TODO: Test Literal lexing
  }
  [TestMethod]
  public void TestIdentifiers() {
    // TODO: Test Identifier lexing
  }
  [TestMethod]
  public void TestAttributes() {
    // TODO: Test Attribute lexing, Comments, Whitespace, Newlines (\n, \r\n)
  }
  // Snapshot Testing
  [TestMethod]
  public Task TestProgram() {
    // The purpose of this test is to lex a full program and verify the token stream.
    // A snapshot test is used here as we don't care to much about individual tokens but we would like to know
    // if something changes in the token stream as a whole or if something breaks.
    var lexer = Lex(@"
      class Main extends Base {
        int x, y, z[];

        void foo(int a, boolean b, int c[]) {
          if (a < 10 && b) {
            x = x + 1;
          } else {
            x = x - 1;
          }
        }

        int bar() {
          return x;
        }
      }

      class Base {
        void calloutMethod() {
          // Callout to external function
          callout printInt(int x);
        }
      }");
    // Collect tokens
    IList<IToken> tokens = lexer.GetAllTokens();
    return Verify(tokens.Select(token => DecafLexer.ruleNames[token.Type - 1]).ToArray(), CreateSettings());
  }
  // TODO: Implement a few Failing Tests (invalid operators, invalid comment types)
}
