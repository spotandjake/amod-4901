using ParseTree;
using System;
using System.Linq;

namespace MiddleEnd {
  // Possible exceptions that can be thrown during semantic analysis
  // TODO: Add position information to these exceptions for better error reporting
  public class SemanticException(string message) : Exception(message) { }
  // Performs semantic analysis on the parse tree, ensuring that the program is semantically valid
  public class SemanticChecker {
    private SemanticChecker() { }
    public static void CheckProgramNode(ProgramNode program) {
      // Ensure the program contains a class named "Program"
      if (!program.Classes.Any(m => m.Name == "Program")) {
        throw new SemanticException("A program must contain a class called 'Program'");
      }

      foreach (var _class in program.Classes) {
        CheckClassNode(_class);
      }
    }
    private static void CheckClassNode(ClassNode _class) {
      // Ensure the Program class contains a method named Main
      if (_class.Name == "Program") {
        const string mainName = "Main";
        // Find the Main method in the Program class
        var mainMethodCandidates = _class.MethodDeclarations.Where(m => m.Name == mainName);
        if (mainMethodCandidates.Count() != 1) {
          throw new SemanticException("A main entry point must exist at `Program.Main()`");
        }
        // We get the first, note that the list is guaranteed to have exactly one element due to the previous check
        var mainMethod = mainMethodCandidates.First(m => m.Name == mainName);
        if (mainMethod.Parameters.Length != 0) {
          throw new SemanticException("`Program.Main()` should not accept any parameters");
        }
        if (!(mainMethod.ReturnType.Type is TypeNode.DecafType.Void)) {
          throw new SemanticException("`Program.Main()` should not accept any parameters");
        }
      }
      // TODO:- throw new SemanticException("A main entry point must exist in `Program.Main()`");
      // TODO:- throw new SemanticException("`Program.Main()` must return void");
    }
  }
}


/*    if (_class.Name == "Program") {
        if (!_class.MethodDeclarations.Any(m => m.Name == "Main")) {
          throw new SemanticException("A main entry point must exist in `Program.Main()`");
        }
        var mainMethod = _class.MethodDeclarations.First(m => m.Name == "Main");
        if (mainMethod.ReturnType != "void") {
          throw new SemanticException("`Program.Main()` must return void");
        }
        if (mainMethod.Parameters.Length != 0) {
          throw new SemanticException("`Program.Main()` must accept zero parameters");
        }
      } */
