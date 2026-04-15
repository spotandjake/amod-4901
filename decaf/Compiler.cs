using System.Reflection;
using System.IO;

using Antlr4.Runtime;

using Decaf.Frontend;
using Decaf.MiddleEnd.TypeChecker;
using Decaf.MiddleEnd.Optimizations;
using Decaf.MiddleEnd;
using Decaf.Backend;

using ParseTree = Decaf.IR.ParseTree;
using TypedTree = Decaf.IR.TypedTree;
using AnfTree = Decaf.IR.AnfTree;
using Wasm = Decaf.WasmBuilder;

namespace Decaf.Compiler {
  public static class Compiler {
    // --- Generic ---

    /// <summary>
    /// This method bundles the runtime code into the user program.
    /// 
    /// This is done in a rather naive way we simply take the runtime code, and then append the parsed runtime modules to the 
    /// user-defined modules. 
    /// This is not the most efficient way to do this, but it is simple and works for our purposes.
    /// One benefit of appending the parsed runtime modules over doing a string append is that we get better error reporting and handling
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
      var runtimeSource = System.Text.Encoding.UTF8.GetString(data);
      // Process with the front end
      var runtimeProgram = FrontEnd(runtimeSource, "$internal$/Runtime.decaf", false);
      // Bundle the runtime into the program
      return new ParseTree.ProgramNode(
        program.Position,
        // Put the runtime modules before the user-defined modules to ensure that the runtime modules are 
        // available to the user-defined modules
        [.. runtimeProgram.Modules, .. program.Modules]
      );
    }

    // --- Entry points ---

    /// <summary>
    /// This is the main entry point for the compiler, it takes the raw source code and runs the 
    /// entire compilation pipeline on it, returning the compiled wasm module.
    /// 
    /// This method runs the entire compilation pipeline on the given source code, this includes:
    /// - Front end (Lexing, Parsing, Semantic Analysis)
    /// - Middle end (Type checking, Lowering to ANF, Optimizations)
    /// - Back end (Lowering to wasm)
    /// </summary>
    /// <param name="source">The raw source code to compile.</param>
    /// <param name="inputFileName">The name of the file that contained the source code.</param>
    /// <returns>The compiled wasm module.</returns>
#nullable enable
    public static Wasm.WasmModule CompileString(string source, string? inputFileName) {
      // Front end
      var frontEndProgram = FrontEnd(source, inputFileName);
      // Middle end
      var middleEndProgram = MiddleEnd(frontEndProgram);
      // Back end
      var wasmModule = Backend(middleEndProgram);
      // Return the compiled wasm module
      return wasmModule;
    }
#nullable restore
    // --- Front end ---

    /// <summary>
    /// This method runs the entire front end pipeline on the given source code, this includes:
    /// - Lexing
    /// - Parsing
    /// - Bundling the runtime code
    /// - Scope checking
    /// - Semantic checking
    /// </summary>
    /// <param name="source">The raw source code to compile.</param>
    /// <param name="inputFileName">The name of the file that contained the source code.</param>
    /// <returns>The program after front end processing.</returns>
#nullable enable
    public static ParseTree.ProgramNode FrontEnd(string source, string? inputFileName, bool bundleRuntime = true) {
      // Lex the program
      var lexer = LexSource(source, inputFileName);
      // Parse the program
      var tokenStream = new CommonTokenStream(lexer);
      var program = ParseSource(tokenStream);
      // Bundle the runtime
      var bundledProgram = bundleRuntime ? BundleRuntime(program) : program;
      // Check semantics - NOTE: we can't do semantic checks before bundling
      var checkedProgram = bundleRuntime ? CheckSemantics(bundledProgram) : program;
      // Return the program after front end processing
      return checkedProgram;
    }
#nullable restore
    /// <summary>
    /// This method runs the lexer on the given source code.
    /// </summary>
    /// <param name="source">The raw source code to lex.</param>
    /// <param name="inputFileName">The name of the file that contained the source code.</param>
    /// <returns>The lexer instance.</returns>
#nullable enable
    public static DecafLexer LexSource(string source, string? inputFileName) {
      // Create Input Stream
      var inputStream = new AntlrInputStream(source) {
        name = inputFileName ?? "<unknown file>"
      };
      // Create Lexer Instance
      var lexer = new DecafLexer(inputStream);
      // Setup our custom error handler for better error reporting
      lexer.RemoveErrorListeners();
      lexer.AddErrorListener(LexerErrorListener.Instance);
      return lexer;
    }
#nullable restore
    /// <summary>This method runs the parser on the given token stream.</summary>
    /// <param name="tokenStream">The token stream to parse.</param>
    /// <returns>The parsed program.</returns>
    public static ParseTree.ProgramNode ParseSource(CommonTokenStream tokenStream) {
      // Initialize the parser
      var parser = new DecafParser(tokenStream);
      // Setup our custom error handler for better error reporting
      parser.RemoveErrorListeners();
      parser.AddErrorListener(ParserErrorListener.Instance);
      // Convert the ANTLR parse tree to our own parse tree representation
      var program = ParseTreeMapper.MapProgramContext(parser.program());
      return program;
    }
    /// <summary>
    /// This method runs the semantic analysis phase on the given program, which includes:
    /// - Scope Validation
    /// - Semantic Validation
    /// </summary>
    /// <param name="program">The parsed program to check.</param>
    /// <returns>The checked program.</returns>
    public static ParseTree.ProgramNode CheckSemantics(ParseTree.ProgramNode program) {
      // Validate program scoping semantics
      ScopeChecker.CheckProgramNode(program);
      // Validate program general semantics
      SemanticChecker.CheckProgramNode(program);
      return program;
    }
    // --- Middle end ---

    /// <summary>
    /// This method runs the entire middle end pipeline on the given program, this includes:
    /// - Type checking
    /// - Lowering to ANF
    /// - Optimizations
    /// 
    /// NOTE: This method expects the input program to have already been processed by the front end,
    ///       if scoping or semantics have not been validated then this may not work correctly and may throw 
    ///       unexpected exceptions, or produce malformed outputs.
    /// </summary>
    /// <param name="program">The parsed program to process.</param>
    /// <returns>The processed program.</returns>
    public static AnfTree.ProgramNode MiddleEnd(ParseTree.ProgramNode program) {
      // Type check the program
      var typedProgram = TypeChecker.TypeProgramNode(program);
      // Lower to ANF
      var anfProgram = AnfMapper.FromProgramNode(typedProgram);
      // Run optimizations
      var optimizedProgram = Optimizer.Optimize(anfProgram);
      // Return the program after middle end processing
      return optimizedProgram;
    }
    /// <summary>
    /// This method runs the type checker on the given program, which includes:
    /// - Validating that all expressions are well-typed
    /// - Annotating the parse tree with type information
    /// </summary>
    /// <param name="program">The parsed program to check.</param>
    /// <returns>The type-checked program.</returns>
    public static TypedTree.ProgramNode TypeCheck(ParseTree.ProgramNode program) {
      // Type check the program
      return TypeChecker.TypeProgramNode(program);
    }
    /// <summary>
    /// This method lowers the given program to ANF, which includes:
    /// - Converting all expressions to ANF form
    /// </summary>
    /// <param name="program">The type-checked program to lower.</param>
    /// <returns>The program in ANF form.</returns>
    public static AnfTree.ProgramNode LowerToAnf(TypedTree.ProgramNode program) {
      // Map the typed tree to the anf tree
      return AnfMapper.FromProgramNode(program);
    }
    /// <summary>
    /// This method runs the optimizer on the given program, which includes:
    /// - Running various optimization passes on the program to improve performance and reduce code size.
    /// </summary>
    /// <param name="program">The anf program to optimize.</param>
    /// <returns>The optimized program.</returns>
    public static AnfTree.ProgramNode OptimizeAnf(AnfTree.ProgramNode program) {
      // Run optimizations
      return Optimizer.Optimize(program);
    }
    // --- Back end ---

    /// <summary>
    /// This method runs the entire back end pipeline on the given program, this includes:
    /// - Lowering to wasm
    /// </summary>
    /// <param name="program">The anf program to compile.</param>
    /// <returns>The compiled wasm module.</returns>
    public static Wasm.WasmModule Backend(AnfTree.ProgramNode program) {
      // Lower the program to wasm
      var wasmModule = Codegen.CompileProgram(program);
      // Return the program after back end processing
      return wasmModule;
    }
    /// <summary>
    /// This method lowers the given program to wasm, which includes:
    /// - Converting all ANF instructions to their corresponding wasm instructions
    /// </summary>
    /// <param name="program">The anf program to lower.</param>
    /// <returns>The compiled wasm module.</returns>
    public static Wasm.WasmModule LowerToWasm(AnfTree.ProgramNode program) {
      // Lower the program to wasm
      return Codegen.CompileProgram(program);
    }
  }

}
