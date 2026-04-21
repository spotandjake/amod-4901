using System;
using System.IO;
using System.Diagnostics;
using VerifyMSTest;

using Decaf.Compiler;
using Decaf.Utils;

namespace decafTests.EndToEnd;

// NOTE: This file exists todo basic testing on the lexer,
//       we don't need to do a ton of testing here, we just validate that keywords, operators and literals match correctly
//       we do more extensive testing of the lexer during parser testing as we will be lexing larger programs and can catch more edge 
//       cases there.
[TestClass]
public class EndToEndTest : VerifyBase {
  private static string CompileAndRun(string sourceFile) {
    // Read the file
    var path = Path.Combine("EndToEnd", "Inputs", sourceFile);
    if (!File.Exists(path)) Assert.Fail($"File {path} does not exist");
    var source = File.ReadAllText(path);
    // Compile the file
    var config = new CompilationConfig {
      // Global config
      UseStartSection = false,
      // Related to modules
      BundleRuntime = true,
      SkipOptimizationPasses = []
    };
    var wasmTree = Compiler.CompileString(config, source, sourceFile);
    // Write the output file
    var outputPath = Path.Combine("EndToEnd", "Outputs", Path.ChangeExtension(sourceFile, "wat"));
    if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
      Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
    File.WriteAllText(outputPath, wasmTree.ToWat());
    // Run the output file
    try {
      var process = new Process {
        StartInfo = new ProcessStartInfo {
          FileName = "wasmtime",
          Arguments = $"run --wasm reference-types --wasm function-references {outputPath}",
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };
      process.Start();
      string output = process.StandardOutput.ReadToEnd();
      string error = process.StandardError.ReadToEnd();
      process.WaitForExit(2000); // A 2 second timeout to prevent hanging tests
      if (process.ExitCode != 0) Assert.Fail($"Process exited with code {process.ExitCode}");
      return output + error;
    }
    catch (System.ComponentModel.Win32Exception) {
      Assert.Inconclusive($"Running {outputPath} with wasmtime, make sure wasmtime is installed and in your PATH");
      return null; // This will never be reached, but it satisfies the compiler
    }
  }
  [TestMethod]
  public void TestValidCompileAndRun() {
    // Test Basic Arithmetic works (includes precedence and associativity)
    Assert.IsEmpty(CompileAndRun("Arithmetic.decaf"));
  }
}
