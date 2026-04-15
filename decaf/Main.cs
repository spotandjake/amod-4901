// TODO: Clean up this file
// TODO: Switch the library we are using for the cli

using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

using Decaf.Compiler;
using Decaf.Utils.Errors;

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
        var wasmModule = Compiler.CompileString(source, relPath);
        Console.WriteLine(wasmModule.ToWat());
        // TODO: Write output to opts.output if specified in the format specified
        // string json = JsonSerializer.Serialize(wasmModule, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        // Console.WriteLine(json);
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
