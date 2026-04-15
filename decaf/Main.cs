using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Cli;

using Decaf.Utils.Errors;

/// <summary>
/// This is the main entry point for the Decaf Command Line Interface (CLI).
/// 
/// It handles the user interactions and calls the appropriate functions from the compiler and error handling modules.
/// </summary>
namespace Decaf.CLI {
  using Decaf.Compiler;
  // The settings for our CLI application.
  public class Settings : CommandSettings {
    [CommandArgument(0, "<input file>")]
    [Description("The input file to be processed.")]
    public required string InputFilePath { get; init; }

    [CommandOption("-o|--output", isRequired: false)]
    [Description("The output file to write the results of compilation to.")]
    [DefaultValue(null)]
    public string OutputFilePath { get; init; }

    [CommandOption("-d|--debug", isRequired: false)]
    [Description("Weather we want to debug the compiler while compiling.")]
    [DefaultValue(false)]
    public bool Debug { get; init; }
  }
  // Our default command for the CLI application.
  public class ProgramCommand : Command<Settings> {
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation) {
      // Get the absolute path of the input file
      string absPath = Path.GetFullPath(settings.InputFilePath);
      string relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), absPath);
      // The file does not exist
      if (!File.Exists(absPath)) {
        AnsiConsole.MarkupLine($"[red]Error:[/] File not found: [yellow]{relPath}[/]");
        return -1;
      }
      // Read the file source content
      string source = File.ReadAllText(absPath);
      // Compile the source content
      try {
        var wasmModule = Compiler.CompileString(source, relPath);
        Console.WriteLine(wasmModule.ToWat());
        // TODO: Write the file output if specified in the settings
        // string json = JsonSerializer.Serialize(wasmModule, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        // Console.WriteLine(json);
      }
      catch (Exception e) {
        // Handle any errors that occur during compilation
        bool rethrow = ErrorHandler.HandleError(settings.Debug, e);
        // Rethrow the exception (NOTE: using throw here rethrows the original exception preserving the stack trace)
        if (rethrow) throw;
      }
      return 0;
    }
  }
  // The main entry point to our program.
  class Program {
    static int Main(string[] args) {
      // Use SpectreConsole.CLI for our command line options
      var app = new CommandApp<ProgramCommand>();
      // Enable Backtraces on exceptions
      app.Configure(config => config.PropagateExceptions());
      return app.Run(args);
    }
  }
}
