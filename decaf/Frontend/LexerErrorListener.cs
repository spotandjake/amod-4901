using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Data;

public sealed class LexerErrorListener : IAntlrErrorListener<int> {
  public static readonly LexerErrorListener Instance = new();
  private LexerErrorListener() { }

  private string buildErrorSourceMessage(string fileName, int line, int column) {
    return $"\u001b[1mFile \"{fileName}\":{line}:{column}\u001b[0m:";
  }
  private string buildLexerErrorMessage(DecafLexer lexer) {
    var text = (ICharStream)lexer.InputStream;
    string invalidText = text.GetText(Interval.Of(lexer.TokenStartCharIndex, text.Index));
    return $"\u001b[31mError\u001b[0m: syntax error unrecognized token `{invalidText}`";
  }

  public void SyntaxError(
      System.IO.TextWriter output,
      IRecognizer recognizer,
      int offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e) {
    if (recognizer is DecafLexer) {
      DecafLexer lexer = (DecafLexer)recognizer;
      string errSrcMsg = buildErrorSourceMessage(lexer.SourceName, line, charPositionInLine);
      string errMsg = buildLexerErrorMessage(lexer);
      throw new SyntaxErrorException($"{errSrcMsg}\n{errMsg}");
    }
  }
}
