namespace decafTests.FrontEnd;

using VerifyMSTest;
using VerifyTests;
using System.Linq;
using System.Threading.Tasks;

using Decaf.Compiler;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.IR.Signature;
using Decaf.Utils;
using Decaf.Utils.Errors.ParsingErrors;

// NOTE: This file exists todo basic testing on the parser, with more in depth testing being performed in our end to end tests
//      The main goal here is just to be able to quickly test specific parsing scenarios without having to write a full end to end test,
//      if we start seeing failures in our end to end tests and these are passing we can be reasonably confident that the issue is in
//      a later stage of the compiler as the individuals nodes parse just fine. Our end to end tests do stress test parsing however.
//      If a regression is found in the end to end tests and we want to quickly test a specific parsing scenario to confirm the 
//      regression is in parsing we can add a test here.
[TestClass]
public class ParserTest : VerifyBase {
  private VerifySettings CreateSettings() {
    var settings = new VerifySettings();
    settings.UseDirectory("Snapshots/Parser/");
    return settings;
  }
  private static ParseTree.ProgramNode Test(string input, string fileName = null) {
    var tokenStream = Compiler.LexSource(input, fileName);
    var commonTokenStream = new Antlr4.Runtime.CommonTokenStream(tokenStream);
    return Compiler.ParseSource(commonTokenStream);
  }
  // --- Code Units ---
  #region CodeUnits
  // Program Structure
  // NOTE: The only goal here is to test programs in isolation, more extensive testing is done in other tests and stress tests
  [TestMethod]
  public void TestEmptyProgram() => Assert.Throws<UnrecognizedTokenException>(() => Test(""));
  [TestMethod]
  public void TestCommentOnlyProgram() => Assert.Throws<UnrecognizedTokenException>(() => Test("// this is a comment"));
  [TestMethod]
  public void TestBasicProgram() {
    try {
      Test("module Program {}");
    }
    catch {
      Assert.Fail($"Basic program should parse without errors");
    }
  }
  // Module Structure
  // NOTE: The only goal here is to test modules in isolation, more extensive testing is done in other tests and stress tests
  [TestMethod]
  public void TestBasicModule() {
    var program = Test("module Program {}");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    // NOTE: This is the most important check here (that we have no content)
    Assert.HasCount(0, module.Body.Statements);
  }
  [TestMethod]
  public void TestTwoModules() {
    var program = Test("module Program1 {} module Program2 {}");
    Assert.IsNotNull(program);
    Assert.HasCount(2, program.Modules);
    // NOTE: This is the most important check here (that we have both modules)
    var module1 = program.Modules[0];
    Assert.IsNotNull(module1);
    Assert.AreEqual("Program1", module1.Name.Name);
    var module2 = program.Modules[1];
    Assert.IsNotNull(module2);
    Assert.AreEqual("Program2", module2.Name.Name);
  }
  // Import Statements
  [TestMethod]
  public void TestImportStatement() {
    var program = Test("""
      module Program {
        import wasm fd_write: (int, int, int, int) => int from "wasi_snapshot_preview1";
      }
    """);
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(0, module.Body.Statements);
    // NOTE: This is the most important check here (that we have the correct import)
    Assert.HasCount(1, module.Imports);
    var import = module.Imports[0];
    Assert.IsNotNull(import);
    Assert.AreEqual("wasi_snapshot_preview1", import.Module);
    Assert.AreEqual("fd_write", import.Name.Name);
  }
  #endregion
  // --- Statements ---
  #region Statements
  // Block Statement
  [TestMethod]
  public void TestEmptyBlockStatement() {
    var program = Test("module Program { {} }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var block = module.Body.Statements[0];
    Assert.IsNotNull(block);
    Assert.IsInstanceOfType(block, typeof(ParseTree.StatementNode.BlockNode));
    var blockNode = block as ParseTree.StatementNode.BlockNode;
    Assert.IsNotNull(blockNode);
    // NOTE: This is the most important check here (that we have no content)
    Assert.HasCount(0, blockNode.Statements);
  }
  [TestMethod]
  public void TestBasicBlockStatement() {
    var program = Test("module Program { { let x: int = 1; } }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var block = module.Body.Statements[0];
    Assert.IsNotNull(block);
    Assert.IsInstanceOfType(block, typeof(ParseTree.StatementNode.BlockNode));
    var blockNode = block as ParseTree.StatementNode.BlockNode;
    Assert.IsNotNull(blockNode);
    // NOTE: This is the most important check here (that we have content)
    Assert.HasCount(1, blockNode.Statements);
  }
  // Variable Declaration Statement
  [TestMethod]
  public void TestVariableDeclarationStatement() {
    var program = Test("module Program { let x: int = 1; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var variableDeclaration = module.Body.Statements[0];
    Assert.IsNotNull(variableDeclaration);
    Assert.IsInstanceOfType(variableDeclaration, typeof(ParseTree.StatementNode.VariableDeclNode));
    var variableDeclarationNode = variableDeclaration as ParseTree.StatementNode.VariableDeclNode;
    Assert.IsNotNull(variableDeclarationNode);
    // NOTE: This is the most important check here (that we have the correct variable declaration)
    Assert.HasCount(1, variableDeclarationNode.Binds);
    var bind = variableDeclarationNode.Binds.ToArray()[0];
    Assert.IsNotNull(bind);
    Assert.AreEqual("x", bind.Name.Name);
  }
  [TestMethod]
  public void TestMultiVariableDeclaration() {
    var program = Test("module Program { let x: int = 1, y: int = 2; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var variableDeclaration = module.Body.Statements[0];
    Assert.IsNotNull(variableDeclaration);
    Assert.IsInstanceOfType(variableDeclaration, typeof(ParseTree.StatementNode.VariableDeclNode));
    var variableDeclarationNode = variableDeclaration as ParseTree.StatementNode.VariableDeclNode;
    Assert.IsNotNull(variableDeclarationNode);
    // NOTE: This is the most important check here (that we have the correct variable declaration)
    Assert.HasCount(2, variableDeclarationNode.Binds);
    var bind = variableDeclarationNode.Binds.ToArray()[0];
    Assert.IsNotNull(bind);
    Assert.AreEqual("x", bind.Name.Name);
    var bind2 = variableDeclarationNode.Binds.ToArray()[1];
    Assert.IsNotNull(bind2);
    Assert.AreEqual("y", bind2.Name.Name);
  }
  // Assignment Statement
  [TestMethod]
  public void TestAssignmentStatement() {
    var program = Test("module Program { x = 2; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is the most important check here (that we have the correct assignment statement)
    var assignmentStatement = module.Body.Statements[0];
    Assert.IsNotNull(assignmentStatement);
    Assert.IsInstanceOfType(assignmentStatement, typeof(ParseTree.StatementNode.AssignmentNode));
    var assignmentStatementNode = assignmentStatement as ParseTree.StatementNode.AssignmentNode;
    Assert.IsNotNull(assignmentStatementNode);
  }
  // Control Flow
  [TestMethod]
  public void TestIfStatement1() {
    // NOTE: This test aims to test a basic if(true) {} statement with no else 
    var program = Test("module Program { if (true) {} }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var ifStatement = module.Body.Statements[0];
    Assert.IsNotNull(ifStatement);
    Assert.IsInstanceOfType(ifStatement, typeof(ParseTree.StatementNode.IfNode));
  }
  [TestMethod]
  public void TestIfStatement2() {
    // NOTE: This test aims to test a basic if(true) {} statement with no else 
    var program = Test("module Program { if (true) {} else {} }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var ifStatement = module.Body.Statements[0];
    Assert.IsNotNull(ifStatement);
    Assert.IsInstanceOfType(ifStatement, typeof(ParseTree.StatementNode.IfNode));
  }
  [TestMethod]
  public Task TestIfStatement3() {
    // NOTE: We do a snapshot of all the types of if statement to be thorough
    var program = Test(@"module Program {
      // Non block 
      if (true) let x: int = 1;
      else let y: int = 2;
      //  Block
      if (true) { let x: int = 1; }
      else { let y: int = 2; }
      // Mixed
      if (true) let x: int = 1;
      else { let y: int = 2; }
      if (true) { let x: int = 1; }
      else let y: int = 2;
    }");
    return Verify(program, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  // While Statement
  [TestMethod]
  public void TestWhileStatement() {
    var program = Test("module Program { while (true) {} }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var whileStatement = module.Body.Statements[0];
    Assert.IsNotNull(whileStatement);
    Assert.IsInstanceOfType(whileStatement, typeof(ParseTree.StatementNode.WhileNode));
  }
  // Return Statement
  [TestMethod]
  public void TestReturnStatement1() {
    // NOTE: While this isn't semantically correct it proves we can parse the return statement correctly
    var program = Test("module Program { return; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var returnStatement = module.Body.Statements[0];
    Assert.IsNotNull(returnStatement);
    Assert.IsInstanceOfType(returnStatement, typeof(ParseTree.StatementNode.ReturnNode));
  }
  [TestMethod]
  public void TestReturnStatement2() {
    // NOTE: While this isn't semantically correct it proves we can parse the return statement correctly
    var program = Test("module Program { return 1; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var returnStatement = module.Body.Statements[0];
    Assert.IsNotNull(returnStatement);
    Assert.IsInstanceOfType(returnStatement, typeof(ParseTree.StatementNode.ReturnNode));
    Assert.IsNotNull((returnStatement as ParseTree.StatementNode.ReturnNode).Value);
  }
  // Continue Statement
  [TestMethod]
  public void TestContinueStatement() {
    // NOTE: While this isn't semantically correct it proves we can parse the continue statement correctly
    var program = Test("module Program { continue; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var continueStatement = module.Body.Statements[0];
    Assert.IsNotNull(continueStatement);
    Assert.IsInstanceOfType(continueStatement, typeof(ParseTree.StatementNode.ContinueNode));
  }
  // Break Statement
  [TestMethod]
  public void TestBreakStatement() {
    // NOTE: While this isn't semantically correct it proves we can parse the break statement correctly
    var program = Test("module Program { break; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var breakStatement = module.Body.Statements[0];
    Assert.IsNotNull(breakStatement);
    Assert.IsInstanceOfType(breakStatement, typeof(ParseTree.StatementNode.BreakNode));
  }
  // Simple Expression Statement Test
  [TestMethod]
  public void TestSimpleExpressionStatement() {
    // NOTE: While this doesn't test every expression statement it proves we can parse a simple expression statement correctly
    //       The stress tests and end to end tests will test more complex expression statements.
    var program = Test("module Program { 1 + 2; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    // NOTE: This is what we care about here
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
  }
  #endregion
  // --- Expressions ---
  #region Expressions
  // Binop Expressions
  [TestMethod]
  public void TestBinopExpression() {
    // NOTE: While this doesn't test every binop it proves we can parse a binop expression correctly
    //       The stress tests and end to end tests will test more complex binops.

    // NOTE: We do not test precedence in the parser as it's really difficult to capture well, instead we test it in
    //       our end to end tests by checking if the output matches what we would expect given the precedence rules, if we have a 
    //       regression in precedence we will see it there and can add more specific tests here if needed.
    var program = Test("module Program { 1 + 2; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.BinopNode));
  }
  // Prefix Expressions
  [TestMethod]
  public void TestPrefixExpression() {
    // NOTE: While this doesn't test every prefix it proves we can parse a prefix expression correctly
    //       The stress tests and end to end tests will test more complex prefix.
    var program = Test("module Program { !true; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.PrefixNode));
  }
  // Call Expressions
  [TestMethod]
  public void TestCallExpression() {
    var program = Test("module Program { foo(); }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.CallNode));
  }
  // Primitive Call - This is just a special case of call with a prim loc
  [TestMethod]
  public void TestPrimitiveCallExpression() {
    var program = Test("module Program { @wasm.memory.size(); }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));

    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.CallNode));
    // NOTE: This is what we care about here
    var callNode = exprStatementNode.Expression as ParseTree.ExpressionNode.CallNode;
    Assert.IsNotNull(callNode);
    Assert.IsTrue(callNode.Callee.IsPrimitive);
  }
  // Array Initialization
  [TestMethod]
  public void TestArrayInitializationExpression() {
    var program = Test("module Program { new int[5]; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.ArrayInitNode));
  }
  // NOTE: We do not test location expressions as they are covered by locations
  // NOTE: We do not test literal expressions as they are covered by literals
  // Parenthesis Expressions
  [TestMethod]
  public void TestParenExpression() {
    var program = Test("module Program { (1 + 2); }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    // NOTE: Parenthesis expressions are syntactic and only exist for precedence, hence we expect the inner expression to be a binop expression, not a paren expression
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.BinopNode));
  }
  #endregion
  // --- Literals ---
  #region Literals
  // Integer Test
  [TestMethod]
  public void TestIntLiteral() {
    var program = Test("module Program { 1; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LiteralExprNode));
    var literalExprNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LiteralExprNode;
    Assert.IsInstanceOfType(literalExprNode.Literal, typeof(ParseTree.LiteralNode.IntegerNode));
    Assert.AreEqual(1, (literalExprNode.Literal as ParseTree.LiteralNode.IntegerNode).Value);
  }
  // Boolean Test
  [TestMethod]
  public void TestBoolLiteral() {
    var program = Test("module Program { true; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LiteralExprNode));
    var literalExprNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LiteralExprNode;
    Assert.IsInstanceOfType(literalExprNode.Literal, typeof(ParseTree.LiteralNode.BooleanNode));
    Assert.IsTrue((literalExprNode.Literal as ParseTree.LiteralNode.BooleanNode).Value);
  }
  // Character Literal
  [TestMethod]
  public void TestCharLiteral() {
    var program = Test("module Program { 'a'; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LiteralExprNode));
    var literalExprNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LiteralExprNode;
    Assert.IsInstanceOfType(literalExprNode.Literal, typeof(ParseTree.LiteralNode.CharacterNode));
    Assert.AreEqual('a', (literalExprNode.Literal as ParseTree.LiteralNode.CharacterNode).Value);
  }
  // String
  [TestMethod]
  public void TestStringLiteral() {
    var program = Test("module Program { \"Hello World!\\n\"; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    // NOTE: This is what we care about here
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LiteralExprNode));
    var literalExprNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LiteralExprNode;
    Assert.IsInstanceOfType(literalExprNode.Literal, typeof(ParseTree.LiteralNode.StringNode));
    Assert.AreEqual("Hello World!\n", (literalExprNode.Literal as ParseTree.LiteralNode.StringNode).Value);
  }
  // Function Literals
  [TestMethod]
  public Task TestFunctionLiteral1() {
    // NOTE: We use snapshots here because of the verbosity of property based testing
    // Basic Test
    var program = Test("module Program { let x = (): void => {}; }");
    return Verify(program, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestFunctionLiteral2() {
    // NOTE: We use snapshots here because of the verbosity of property based testing
    // With A Parameter Test
    var program = Test("module Program { let x = (x: int): void => {}; }");
    return Verify(program, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestFunctionLiteral3() {
    // NOTE: We use snapshots here because of the verbosity of property based testing
    // With Multiple Parameters Test
    var program = Test("module Program { let x = (x: int, y: boolean): void => {}; }");
    return Verify(program, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public Task TestFunctionLiteral4() {
    // NOTE: We use snapshots here because of the verbosity of property based testing
    // With a body Test
    var program = Test("module Program { let x = (): int => { return 1; }; }");
    return Verify(program, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  [TestMethod]
  public void TestFunctionLiteral5() {
    // Functions need to know their name, so while parsing we ensure that they are the rhs of a bind, this test ensures that we throw an error if this is not the case
    Assert.Throws<Decaf.Utils.Errors.SemanticErrors.FunctionLiteralMustBeDirectRhsOfVarDecl>(
      () => Test("module Program { { (): void => {}; } }")
    );
  }
  #endregion
  // -- Types ---
  #region Types
  // Array Type
  [TestMethod]
  public void TestArrayType() {
    // NOTE: While this program isn't semantically correct it proves we can parse the `int[]` type correctly
    var program = Test("module Program { let x: int[] = 1; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var variableDeclaration = module.Body.Statements[0];
    Assert.IsNotNull(variableDeclaration);
    Assert.IsInstanceOfType(variableDeclaration, typeof(ParseTree.StatementNode.VariableDeclNode));
    var variableDeclarationNode = variableDeclaration as ParseTree.StatementNode.VariableDeclNode;
    Assert.IsNotNull(variableDeclarationNode);
    // NOTE: This is the most important check here (that we have the correct variable declaration)
    Assert.HasCount(1, variableDeclarationNode.Binds);
    var bind = variableDeclarationNode.Binds.ToArray()[0];
    Assert.IsNotNull(bind);
    Assert.AreEqual("x", bind.Name.Name);
    Assert.IsNotNull(bind.Signature);
    Assert.IsInstanceOfType(bind.Signature, typeof(Signature.ArraySig));
    var arraySig = bind.Signature as Signature.ArraySig;
    Assert.IsNotNull(arraySig);
    Assert.IsNotNull(arraySig.Typ);
    Assert.IsInstanceOfType(arraySig.Typ, typeof(Signature.PrimitiveSig));
  }
  // Simple Types
  [TestMethod]
  public void TestSimpleTypes() {
    var program = Test(@"
      module Program {
        let x: int = 1,
            y: boolean = true,
            z: char = 'a',
            s: string = ""hello"";
      }"
    );
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var variableDeclaration = module.Body.Statements[0];
    Assert.IsNotNull(variableDeclaration);
    Assert.IsInstanceOfType(variableDeclaration, typeof(ParseTree.StatementNode.VariableDeclNode));
    var variableDeclarationNode = variableDeclaration as ParseTree.StatementNode.VariableDeclNode;
    Assert.IsNotNull(variableDeclarationNode);
    // NOTE: This is the most important check here (that we have the correct variable types)
    Assert.HasCount(4, variableDeclarationNode.Binds);
    var bind = variableDeclarationNode.Binds.ToArray()[0];
    Assert.IsNotNull(bind);
    Assert.AreEqual("x", bind.Name.Name);
    Assert.IsNotNull(bind.Signature);
    Assert.IsInstanceOfType(bind.Signature, typeof(Signature.PrimitiveSig));
    Assert.AreEqual(PrimitiveType.Int, (bind.Signature as Signature.PrimitiveSig).Type);
    var bind2 = variableDeclarationNode.Binds.ToArray()[1];
    Assert.IsNotNull(bind2);
    Assert.AreEqual("y", bind2.Name.Name);
    Assert.IsNotNull(bind2.Signature);
    Assert.IsInstanceOfType(bind2.Signature, typeof(Signature.PrimitiveSig));
    Assert.AreEqual(PrimitiveType.Boolean, (bind2.Signature as Signature.PrimitiveSig).Type);
    var bind3 = variableDeclarationNode.Binds.ToArray()[2];
    Assert.IsNotNull(bind3);
    Assert.AreEqual("z", bind3.Name.Name);
    Assert.IsNotNull(bind3.Signature);
    Assert.IsInstanceOfType(bind3.Signature, typeof(Signature.PrimitiveSig));
    Assert.AreEqual(PrimitiveType.Character, (bind3.Signature as Signature.PrimitiveSig).Type);
    var bind4 = variableDeclarationNode.Binds.ToArray()[3];
    Assert.IsNotNull(bind4);
    Assert.AreEqual("s", bind4.Name.Name);
    Assert.IsNotNull(bind4.Signature);
    Assert.IsInstanceOfType(bind4.Signature, typeof(Signature.PrimitiveSig));
    Assert.AreEqual(PrimitiveType.String, (bind4.Signature as Signature.PrimitiveSig).Type);
  }
  // Function Types
  [TestMethod]
  public Task TestFuncTypes() {
    // NOTE: We use snapshots here because of the verbosity of property based testing
    // NOTE: While this isn't semantically correct it parses the function type which is what we care about
    var program = Test("module Program { let x: (int, int) => int = 1; }");
    return Verify(program, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  #endregion
  // --- Locations ---
  #region Locations
  // Array Location
  [TestMethod]
  public void TestArrayLocation() {
    var program = Test("module Program { x[0]; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    // NOTE: This is what we care about here
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LocationExprNode));
    var arrayAccessNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LocationExprNode;
    Assert.IsNotNull(arrayAccessNode);
    Assert.IsInstanceOfType(arrayAccessNode.Location, typeof(ParseTree.LocationNode.ArrayNode));
    var arrayLocationNode = arrayAccessNode.Location as ParseTree.LocationNode.ArrayNode;
    Assert.IsNotNull(arrayLocationNode);
    Assert.IsFalse(arrayLocationNode.IsPrimitive);
  }
  // Member Location
  [TestMethod]
  public void TestMemberLocation() {
    var program = Test("module Program { Runtime.print; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    // NOTE: This is what we care about here
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LocationExprNode));
    var memberAccessNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LocationExprNode;
    Assert.IsNotNull(memberAccessNode);
    Assert.IsInstanceOfType(memberAccessNode.Location, typeof(ParseTree.LocationNode.MemberNode));
    var memberLocationNode = memberAccessNode.Location as ParseTree.LocationNode.MemberNode;
    Assert.IsNotNull(memberLocationNode);
    Assert.IsFalse(memberLocationNode.IsPrimitive);
  }
  // Basic Location
  [TestMethod]
  public void TestBasicLocation() {
    var program = Test("module Program { x; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    // NOTE: This is what we care about here
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LocationExprNode));
    var accessNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LocationExprNode;
    Assert.IsNotNull(accessNode);
    Assert.IsInstanceOfType(accessNode.Location, typeof(ParseTree.LocationNode.IdentifierNode));
    var locationNode = accessNode.Location as ParseTree.LocationNode.IdentifierNode;
    Assert.IsNotNull(locationNode);
    Assert.IsFalse(locationNode.IsPrimitive);
  }
  // Primitive Location
  [TestMethod]
  public void TestPrimitiveLocation() {
    // NOTE: While you can't really use prims like this, it parses correctly so we can test it, the `@` signifies its a primitive
    var program = Test("module Program { @wasm.i32.store; }");
    Assert.IsNotNull(program);
    Assert.HasCount(1, program.Modules);
    var module = program.Modules[0];
    Assert.IsNotNull(module);
    Assert.AreEqual("Program", module.Name.Name);
    Assert.HasCount(1, module.Body.Statements);
    var expressionStatement = module.Body.Statements[0];
    Assert.IsNotNull(expressionStatement);
    Assert.IsInstanceOfType(expressionStatement, typeof(ParseTree.StatementNode.ExprStatementNode));
    var exprStatementNode = expressionStatement as ParseTree.StatementNode.ExprStatementNode;
    // NOTE: This is what we care about here
    Assert.IsNotNull(exprStatementNode);
    Assert.IsInstanceOfType(exprStatementNode.Expression, typeof(ParseTree.ExpressionNode.LocationExprNode));
    var accessNode = exprStatementNode.Expression as ParseTree.ExpressionNode.LocationExprNode;
    Assert.IsNotNull(accessNode);
    Assert.IsInstanceOfType(accessNode.Location, typeof(ParseTree.LocationNode.MemberNode));
    var locationNode = accessNode.Location as ParseTree.LocationNode.MemberNode;
    Assert.IsNotNull(locationNode);
    Assert.IsTrue(locationNode.IsPrimitive);
  }
  #endregion
  // --- Invalid Tests ---
  #region InvalidTests
  [TestMethod]
  public void TestInvalidStmtNoSemi() {
    Assert.Throws<UnrecognizedTokenException>(() => Test("module Main { let x = 1 }"));
  }
  [TestMethod]
  public void TestInvalidParenExpression() {
    Assert.Throws<UnrecognizedTokenException>(() => Test("module Main { let x = (); }"));
  }
  #endregion
  // --- Stress Tests ---
  #region StressTests
  // Full Stress Test Programs
  [TestMethod]
  public Task TestFullProgram() {
    // NOTE: The purpose of this test is just to ensure a practical program fully parses
    var result = Test(@"
    module Main {
      let x: int = 0, y: int = 0, z: int[] = new int[5];

      let foo = (a: int, b: boolean, c: int[]): void => {
        if (a < 10 && b) {
          x = x + 1;
        } else {
          x = x - 1;
        }
      };

      let bar = (): int => {
        return x;
      };
    }

    module Base {
      let calloutMethod = (): void => {
      };
    }
    ");
    return Verify(result, CreateSettings()).IgnoreMembersWithType<Position>();
  }
  #endregion
}
