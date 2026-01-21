using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using CommandLine;
using System.Text.Json;

namespace Compiler {
  public class Compiler {
#nullable enable
    public static DecafLexer LexString(string source, string? inputFileName) {
      // Create Input Stream
      AntlrInputStream inputStream = new AntlrInputStream(source);
      // Create Lexer Instance
      DecafLexer lexer = new DecafLexer(inputStream);
      return lexer;
    }
    #nullable enable
    public static ParseTree.ProgramNode ParseTokenStream(CommonTokenStream tokenStream, string? inputFileName) {
      DecafParser parser = new DecafParser(tokenStream);
      ParseTree.ProgramNode program = ParseTree.ProgramNode.FromContext(parser.program());
      return program;
    }
#nullable enable
    public static void CompileString(string source, string? inputFileName) {
      // Lexing
      DecafLexer lexer = LexString(source, inputFileName);
      CommonTokenStream tokenStream = new CommonTokenStream(lexer);
      // while (true) {
      //   IToken token = lexer.NextToken();
      //   if (token.Type == TokenConstants.EOF)
      //     break;
      //   Console.WriteLine(
      //     $"Token Type: {DecafLexer.ruleNames[token.Type - 1]}, Text: '{token.Text}'"
      //   );
      // }
      // Parsing
      ParseTree.ProgramNode program = ParseTokenStream(tokenStream, inputFileName);
      string json = JsonSerializer.Serialize(program, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
      Console.WriteLine(json);
      // TODO: Figure out ast error checking
      // TODO: Semantic Analysis
      // TODO: TypeChecking
      // TODO: Code Generation
    }
  }
}

namespace CLI {
  public class Program {
    class Options {
      [Value(0, MetaName = "input file",
            HelpText = "Input file to be processed.",
            Required = true)]
      public required string FileName { get; set; }
      [Option('o', "output", Required = false, HelpText = "Output filename.")]
      public string? output { get; set; }
    }

    static void Main(string[] args) {
      CommandLine.Parser.Default.ParseArguments<Options>(args)
        .WithParsed(RunOptions)
        .WithNotParsed(HandleParseError);
    }
    static void RunOptions(Options opts) {
      // Get file absolute
      string absPath = System.IO.Path.GetFullPath(opts.FileName);
      if (!System.IO.File.Exists(absPath)) {
        Console.WriteLine($"File not found: {absPath}");
        return;
      }
      // Read file content
      string source = System.IO.File.ReadAllText(absPath);
      // Compile
      Compiler.Compiler.CompileString(source, absPath);
      // TODO: Write output to opts.output if specified
    }
    static void HandleParseError(IEnumerable<Error> errs) {
      //handle errors
    }
  }
}
