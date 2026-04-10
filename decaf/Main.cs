using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Antlr4.Runtime;
using CommandLine;
using System.Text.Json;

using ParseTree = Decaf.IR.ParseTree;
using TypedTree = Decaf.IR.TypedTree;
using AnfTree = Decaf.IR.AnfTree;
using Decaf.Utils;
using Decaf.Frontend;
using Decaf.MiddleEnd;
using Decaf.MiddleEnd.TypeChecker;
using Decaf.Backend;
using Decaf.Utils.Errors;

namespace Compiler {
  public class Compiler {
    /// <summary>
    /// This method bundles the runtime code into the user program.
    /// 
    /// This is done in a rather naive way we simply take the runtime code, and then append the parsed runtime classes to the 
    /// user-defined classes. 
    /// This is not the most efficient way to do this, but it is simple and works for our purposes.
    /// One benefit of appending the parsed runtime classes over doing a string append is that we get better error reporting and handling
    /// as locations are right and malformed runtime code will be caught while parsing the runtime, 
    /// so user errors are scoped to their source.
    /// </summary>
    /// <param name="program">The program to bundle the runtime code into.</param>
    /// <returns>The bundled program.</returns>
    private static ParseTree.ProgramNode BundleRuntime(ParseTree.ProgramNode program) {
      // Get the embedded runtime resource (This is basically a hack to include the runtime code in the compiled assembly)
      var assembly = Assembly.GetExecutingAssembly();
      using var stream = assembly.GetManifestResourceStream("decaf.Runtime.decaf");
      using var reader = new BinaryReader(stream);
      byte[] data = reader.ReadBytes((int)stream.Length);
      // Lex the runtime
      var runtimeSource = System.Text.Encoding.UTF8.GetString(data);
      var lexer = LexString(runtimeSource, "$internal$/Runtime.decaf");
      var tokenStream = new CommonTokenStream(lexer);
      // Parse the runtime
      var runtimeProgram = ParseTokenStream(tokenStream);
      // Bundle the runtime into the program
      return new ParseTree.ProgramNode(
        program.Position,
        // Put the runtime classes before the user-defined classes to ensure that the runtime classes are 
        // available to the user-defined classes
        [.. runtimeProgram.Classes, .. program.Classes],
        null
      );
    }
#nullable enable
    public static DecafLexer LexString(string source, string? inputFileName) {
#nullable disable
      // Create Input Stream
      AntlrInputStream inputStream = new AntlrInputStream(source) {
        name = inputFileName ?? "<unknown file>"
      };
      // Create Lexer Instance
      DecafLexer lexer = new DecafLexer(inputStream);
      lexer.RemoveErrorListeners();
      lexer.AddErrorListener(LexerErrorListener.Instance);
      return lexer;
    }
    public static ParseTree.ProgramNode ParseTokenStream(CommonTokenStream tokenStream) {
      DecafParser parser = new DecafParser(tokenStream);
      parser.RemoveErrorListeners();
      parser.AddErrorListener(ParserErrorListener.Instance);
      ParseTree.ProgramNode program = ParseTreeMapper.MapProgramContext(parser.program());
      return program;
    }
    public static ParseTree.ProgramNode SemanticAnalysis(ParseTree.ProgramNode program) {
      var scopedTree = ScopeMapper.MapProgramNode(program, new Scope<bool>(null));
      SemanticChecker.CheckProgramNode(scopedTree);
      return scopedTree;
    }
    public static TypedTree.ProgramNode TypeChecking(ParseTree.ProgramNode program) {
      return TypeChecker.TypeProgramNode(program);
    }
    public static AnfTree.ProgramNode AnfMapping(TypedTree.ProgramNode program) {
      return AnfMapper.FromProgramNode(program);
    }
    public static bool Codegen(AnfTree.ProgramNode program) {
      // TODO: This should return a wasm module
      Decaf.Backend.Codegen.CompileProgram(program);
      return true;
    }
#nullable enable
    public static void CompileString(string source, string? inputFileName) {
#nullable disable
      // Lexing
      var lexer = LexString(source, inputFileName);
      var tokenStream = new CommonTokenStream(lexer);
      // NOTE: For debugging lexer token stream
      // while (true) {
      //   IToken token = lexer.NextToken();
      //   if (token.Type == TokenConstants.EOF)
      //     break;
      //   Console.WriteLine(
      //     $"Token Type: {DecafLexer.ruleNames[token.Type - 1]}, Text: '{token.Text}'"
      //   );
      // }
      // Parsing
      var parsedProgram = ParseTokenStream(tokenStream);
      // Include the runtime code
      var bundledProgram = BundleRuntime(parsedProgram);
      // Semantic Analysis
      var scopedProgram = SemanticAnalysis(bundledProgram);
      // TypeChecking
      var TypeCheckingProgram = TypeChecking(scopedProgram);
      // Anf Conversion
      var anfProgram = AnfMapping(TypeCheckingProgram);
      // Code Generation
      var wasmModule = Codegen(anfProgram);
      // TODO: Return the wasm module
      string json = JsonSerializer.Serialize(anfProgram, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
      Console.WriteLine(json);
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
#nullable enable
      public string? output { get; set; }
#nullable disable
      [Option("debug", Required = false, HelpText = "Weather to run in debug mode or not.")]
      public bool Debug { get; set; } = false;
    }

    static void Main(string[] args) {
      CommandLine.Parser.Default.ParseArguments<Options>(args)
        .WithParsed(RunOptions)
        .WithNotParsed(HandleCommandLineErrors);
    }
    static void RunOptions(Options opts) {
      // Get file absolute
      string absPath = Path.GetFullPath(opts.FileName);
      string relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), absPath);
      if (!File.Exists(absPath)) {
        Console.WriteLine($"File not found: {absPath}");
        return;
      }
      // Read file content
      string source = File.ReadAllText(absPath);
      // Compile
      try {
        // TODO: Write output to opts.output if specified
        Compiler.Compiler.CompileString(source, relPath);
      }
      catch (Exception e) {
        bool rethrow = ErrorHandler.HandleError(opts.Debug, e);
        // Rethrow the exception (NOTE: using throw here rethrows the original exception preserving the stack trace)
        if (rethrow) throw;
      }
    }
    static void HandleCommandLineErrors(IEnumerable<Error> errs) {
      // TODO: Handle Command Line Errors
    }
  }
}
