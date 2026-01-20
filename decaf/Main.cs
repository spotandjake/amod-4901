using System;
using Antlr4.Runtime;

public class Program
{
    public static void Main(string[] args)
    {
        String text = "This is a test string for ANTLR input stream.";
        AntlrInputStream inputStream = new AntlrInputStream(text.ToString());
        DecafLexer lexer = new DecafLexer(inputStream);
        while (true)
        {
            IToken token = lexer.NextToken();
            if (token.Type == TokenConstants.EOF)
            {
                break;
            }
            System.Console.WriteLine($"Token Type: {token.Type}, Text: '{token.Text}'");
        }
        System.Console.WriteLine("Hello, World!");
    }
}