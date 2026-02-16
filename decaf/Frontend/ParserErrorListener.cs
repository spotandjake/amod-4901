using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Data;

public sealed class ParserErrorListener : IAntlrErrorListener<IToken> {
  public static readonly ParserErrorListener Instance = new();
  private ParserErrorListener() { }

  private string buildErrorSourceMessage(string fileName, int line, int column) {
    return $"\u001b[1mFile \"{fileName}\":{line}:{column}\u001b[0m:";
  }
  private string buildParserErrorMessage(string msg) {
    return $"\u001b[31mError\u001b[0m: syntax error unrecognized token `{msg}`";
  }

  public void SyntaxError(
      System.IO.TextWriter output,
      IRecognizer recognizer,
      IToken offendingSymbol,
      int line,
      int charPositionInLine,
      string msg,
      RecognitionException e) {
    if (recognizer is DecafParser) {
      var parser = (DecafParser)recognizer;
      string errSrcMsg = buildErrorSourceMessage(parser.SourceName, line, charPositionInLine);
      string errMsg = buildParserErrorMessage(msg);
      throw new SyntaxErrorException($"{errSrcMsg}\n{errMsg}");
    }
  }
}
