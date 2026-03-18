using System;
using System.Linq;

using Decaf.IR.ParseTree;
using Decaf.Utils.Errors.SemanticErrors;

namespace MiddleEnd {
  // Performs semantic analysis on the parse tree, ensuring that the program is semantically valid
  public class SemanticChecker {
    private SemanticChecker() { }
    public static void CheckProgramNode(ProgramNode program) {
      // Ensure the program contains a class named "Program"
      if (!program.Classes.Any(m => m.Name == "Program")) {
        throw new SemanticException(program.Position, "A program must contain a class called 'Program'");
      }

      foreach (var _class in program.Classes) {
        CheckClassNode(_class);
      }
    }
    private static void CheckClassNode(DeclarationNode.ClassNode _class) {
      // Ensure the Program class contains a method named Main
      if (_class.Name == "Program") {
        const string mainName = "Main";
        // Find the Main method in the Program class
        var mainMethodCandidates = _class.Methods.Where(m => m.Name == mainName);
        if (mainMethodCandidates.Count() != 1) {
          throw new SemanticException(_class.Position, "A main entry point must exist at `Program.Main()`");
        }
        // We get the first, note that the list is guaranteed to have exactly one element due to the previous check
        var mainMethod = mainMethodCandidates.First(m => m.Name == mainName);
        if (mainMethod.Parameters.Length != 0) {
          throw new SemanticException(mainMethod.Position, "`Program.Main()` should not accept any parameters");
        }
        if (!(mainMethod.ReturnType.Type == TypeNode.PrimitiveType.Void)) {
          throw new SemanticException(mainMethod.ReturnType.Position, "`Program.Main()` should not accept any parameters");
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
