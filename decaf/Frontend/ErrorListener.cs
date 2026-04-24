namespace Decaf.Frontend;

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

using LexerErrors = Decaf.Utils.Errors.LexingErrors;
using ParserErrors = Decaf.Utils.Errors.ParsingErrors;
using Decaf.Utils;

public sealed class LexerErrorListener : IAntlrErrorListener<int> {
  public static readonly LexerErrorListener Instance = new();
  private LexerErrorListener() { }
  public void SyntaxError(
      System.IO.TextWriter output,
      IRecognizer recognizer,
      int offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e) {
    if (recognizer is DecafLexer lexer) {
      var text = (ICharStream)lexer.InputStream;
      string invalidText = text.GetText(Interval.Of(lexer.TokenStartCharIndex, text.Index));
      throw new LexerErrors.UnrecognizedTokenException(
        new Position {
          fileName = lexer.SourceName,
          line = line,
          column = charPositionInLine,
          offset = lexer.CharIndex
        },
        invalidText
      );
    }
  }
}

public sealed class ParserErrorListener : IAntlrErrorListener<IToken> {
  public static readonly ParserErrorListener Instance = new();
  private ParserErrorListener() { }
  public void SyntaxError(
      System.IO.TextWriter output,
      IRecognizer recognizer,
      IToken offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e) {
    if (recognizer is DecafParser parser) {
      throw new ParserErrors.UnrecognizedTokenException(
        new Position {
          fileName = parser.SourceName,
          line = line,
          column = charPositionInLine,
          offset = offendingSymbol.StartIndex
        },
        msg
      );
    }
  }
}
