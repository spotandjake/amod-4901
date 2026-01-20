using System;
using System.IO;
using Antlr4.Runtime;

public class Program {
  public static void Main(string[] args) {
    // TODO: Make a compile function: https://github.com/commandlineparser/commandline
    // TODO: Make a pretty user facing cli
    // Open Test File
    FileStream fileStream = new FileStream("./test.decaf", FileMode.Open);
    StreamReader reader = new StreamReader(fileStream);
    string fileContent = reader.ReadToEnd();
    reader.Close();
    fileStream.Close();
    // Lex
    AntlrInputStream inputStream = new AntlrInputStream(fileContent);
    DecafLexer lexer = new DecafLexer(inputStream);
    while (true) {
      IToken token = lexer.NextToken();
      if (token.Type == TokenConstants.EOF)
        break;
      Console.WriteLine(
        $"Token Type: {DecafLexer.ruleNames[token.Type - 1]}, Text: '{token.Text}'"
      );
    }
    Console.WriteLine("Hello, World!");
  }
}
