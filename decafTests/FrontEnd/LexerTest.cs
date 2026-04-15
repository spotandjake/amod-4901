using System.Threading.Tasks;
using Antlr4.Runtime;
using VerifyMSTest;
using VerifyTests;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace decafTests.FrontEnd;

// NOTE: This file exists todo basic testing on the lexer,
//       we don't need to do a ton of testing here, we just validate that keywords, operators and literals match correctly
//       we do more extensive testing of the lexer during parser testing as we will be lexing larger programs and can catch more edge 
//       cases there.
[TestClass]
public class LexerTest : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Lexer/");
    return settings;
  }
  private static DecafLexer Lex(string text) => Compiler.Compiler.LexString(text, null);
  [TestMethod]
  public void TestEnsureLexerTestUpdated() {
    // NOTE: This isn't an actual test of the lexer, but if a rule is added or removed 
    //       it ensures that the tests are also looked at / not forgotten about.
    Assert.HasCount(60, DecafLexer.ruleNames);
  }
  // Unit Test Keywords
  [TestMethod]
  public void TestTypeKeywords() {
    var lexer = Lex("int boolean char string void");
    Assert.AreEqual(DecafLexer.INT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.BOOLEAN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHAR, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.STRING, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.VOID, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestInstructionKeywords() {
    var lexer = Lex("break continue return");
    Assert.AreEqual(DecafLexer.BREAK, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CONTINUE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RETURN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestControlFlowKeywords() {
    var lexer = Lex("if else while");
    Assert.AreEqual(DecafLexer.IF, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.ELSE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.WHILE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestValueKeywords() {
    var lexer = Lex("true false");
    Assert.AreEqual(DecafLexer.TRUE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.FALSE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestOtherKeywords() {
    var lexer = Lex("module let new");
    Assert.AreEqual(DecafLexer.MODULE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.LET, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.NEW, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  // Unit Test Operators
  [TestMethod]
  public void TestPunctuation() {
    var lexer = Lex("() {} [] ; , . : =>");
    Assert.AreEqual(DecafLexer.LPAREN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RPAREN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.LBRACE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RBRACE, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.LBRACK, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.RBRACK, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.SEMI, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.COMMA, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.DOT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.COLON, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.ARROW, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestPrefixOperators() {
    var lexer = Lex("! ~");
    Assert.AreEqual(DecafLexer.NOT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.BNOT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestArithmeticOperators() {
    var lexer = Lex("+ - * /");
    Assert.AreEqual(DecafLexer.PLUS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.MINUS, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.MULT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.DIV, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestRelationalOperators() {
    var lexer = Lex("<= >= < >");
    Assert.AreEqual(DecafLexer.LEQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.GEQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.LT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.GT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestEqualityOperators() {
    var lexer = Lex("== !=");
    Assert.AreEqual(DecafLexer.EQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.NEQ, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestConditionalOperators() {
    var lexer = Lex("&& ||");
    Assert.AreEqual(DecafLexer.AND, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.OR, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestBitwiseOperators() {
    var lexer = Lex("& | << >>");
    Assert.AreEqual(DecafLexer.BAND, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.BOR, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.BLSHIFT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.BRSHIFT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestAssignmentOperator() {
    var lexer = Lex("=");
    Assert.AreEqual(DecafLexer.ASSIGN, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  // Unit Test Literals
  [TestMethod]
  public void TestInt() {
    // INTLIT testing
    string decTestString = "0 -1 10 -56 256 -100";
    DecafLexer lexer = Lex(decTestString);

    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);

    string hexTestString = "0x0 0x1 0xa 0x1a2b 0xbob 0x1z";
    lexer = Lex(hexTestString);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
    Assert.AreNotEqual(DecafLexer.INTLIT, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestChar() {
    string charTestString = "'\\n' '\\t' '\\' '\\\\' '\\'' '\\\"' 'a' 'Z' ' ' '!' '#' '$' '%' '&' '(' ')' '~'";
    DecafLexer lexer = Lex(charTestString);

    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.CHARLIT, lexer.NextToken().Type);
  }

  [TestMethod]
  public void TestString() {
    string strlitTestString = "\"~($tRinG liter@l!)#%& \" \"\\n \\t \\ \\\\ \\' \\\" \"";
    DecafLexer lexer = Lex(strlitTestString);

    Assert.AreEqual(DecafLexer.STRINGLIT, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.STRINGLIT, lexer.NextToken().Type);
  }

  [TestMethod]
  public void TestInvalidMultiChar() {
    string testString = "'aa'";
    DecafLexer lexer = Lex(testString);
    try {
      IToken token = lexer.NextToken();
      Assert.Fail("Expected a SyntaxErrorException to be thrown.");
    }
    catch (SyntaxErrorException e) {
      Assert.Contains("'aa", e.Message);
    }
  }

  [TestMethod]
  public void TestIdentifiers() {
    string idTestString = "a A _foo_bar_ ab12cd 1a";
    DecafLexer lexer = Lex(idTestString);

    Assert.AreEqual(DecafLexer.ID, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.ID, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.ID, lexer.NextToken().Type);
    Assert.AreEqual(DecafLexer.ID, lexer.NextToken().Type);
    Assert.AreNotEqual(DecafLexer.ID, lexer.NextToken().Type);
  }

  [TestMethod]
  public void TestKeywordIdentifier() {
    string TestString = "whiletrue";
    DecafLexer lexer = Lex(TestString);

    Assert.AreEqual(DecafLexer.ID, lexer.NextToken().Type);
  }

  [TestMethod]
  public void TestAttributes() {
    // Test Attributes, Comments, Whitespace, Newlines (\n, \r\n)
    // NOTE: These are all skipped by the lexer so we just need to 
    //       ensure they don't interfere with tokenization.
    string testString = "// this is a comment\n  \n\n\r\n";
    DecafLexer lexer = Lex(testString);
    Assert.AreEqual(DecafLexer.Eof, lexer.NextToken().Type);
  }
  [TestMethod]
  public void TestInvalidToken() {
    // NOTE: We only have a single test for invalid tokens as the 
    //       parser will catch more cases, it's easier to test there. 
    //       We just intend to test the error handling works.
    string testString = "$";
    DecafLexer lexer = Lex(testString);
    try {
      IToken token = lexer.NextToken();
      Assert.Fail("Expected a SyntaxErrorException to be thrown.");
    }
    catch (SyntaxErrorException e) {
      // NOTE: We don't care about the exact message, just that it includes the invalid token.
      Assert.Contains("$", e.Message);
    }
  }
  // Snapshot Testing
  [TestMethod]
  public Task TestProgram() {
    // The purpose of this test is to lex a full program and verify the token stream.
    // A snapshot test is used here as we don't care to much about individual tokens but we would like to know
    // if something changes in the token stream as a whole or if something breaks.
    var lexer = Lex(@"
      module Main {
        let x: int = 1, y: boolean = true, z: int[] = new int[10];

        let foo = (a: int, b: boolean, c: int[]): void => {
          if (a < 10 && b) {
            x = x + 1;
          } else {
            x = x - 1;
          }
        };

        let bar = (): int => {
          return x;
        };
      }");
    // Collect tokens
    IList<IToken> tokens = lexer.GetAllTokens();
    return Verify(tokens.Select(token => DecafLexer.ruleNames[token.Type - 1]).ToArray(), CreateSettings());
  }
}
