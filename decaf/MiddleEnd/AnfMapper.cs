using System;
using System.Linq;
using System.Collections.Generic;

using AnfTree = Decaf.IR.AnfTree;
using TypedTree = Decaf.IR.TypedTree;
using Signature = Decaf.IR.Signature;
using Decaf.Utils;

// The purpose of this file is to map from the typed tree to the ANF tree.
namespace Decaf.MiddleEnd {
  // NOTE: One downside of the recursive approach is theoretically we could blow the stack if we have very nested expressions.
  //       However if this were to ever become an issue we would switch to a work queue based approach.
  public static class AnfMapper {
    private record AnfState(IDGenerator SymbolGenerator, List<AnfTree.FunctionNode> ModuleFunctions) {
      public IDGenerator SymbolGenerator { get; } = SymbolGenerator;
      public List<AnfTree.FunctionNode> ModuleFunctions { get; set; } = ModuleFunctions;
    }
    // A helper to generate temporary variables
    private static (AnfTree.InstructionNode.BindNode, AnfTree.ImmediateNode.LocationImmNode) GenerateTempBind(
      AnfState state, AnfTree.SimpleExpressionNode value, Signature.Signature signature
    ) {
      // Create the binding name
      var tempName = "__anf_temp";
      var tempID = Symbol.Create(state.SymbolGenerator, value.Position, false, tempName);
      // Create the temp location
      var tempLocation = new AnfTree.LocationNode.SymbolLocation(value.Position, tempID, signature);
      // Create the bind
      var bind = new AnfTree.InstructionNode.BindNode(value.Position, tempLocation.ID, value);
      // Create the imm
      var imm = new AnfTree.ImmediateNode.LocationImmNode(value.Position, tempLocation, signature);
      // Return the bind and the imm
      return (bind, imm);
    }
    // --- Code Units
    #region CodeUnits
    public static AnfTree.ProgramNode FromProgramNode(TypedTree.ProgramNode node) {
      // No mapping is required on the program node, since it is just a container
      return new AnfTree.ProgramNode(
        node.Position,
        node.Modules.Select((m) => FromModuleNode(new AnfState(node.SymbolIdGenerator, null), m)).ToArray()
      );
    }
    private static AnfTree.ModuleNode FromModuleNode(AnfState state, TypedTree.ModuleNode node) {
      var moduleState = new AnfState(state.SymbolGenerator, []);
      var instructions = new List<AnfTree.InstructionNode>();
      // Map the imports
      var imports = new List<AnfTree.ImportNode>();
      foreach (var imp in node.Imports) {
        var mappedImp = new AnfTree.ImportNode(imp.Position, imp.ID, imp.Signature, imp.ExternalName, imp.ExternalModule);
        imports.Add(mappedImp);
        // We also need to add a setup instruction for the import to our module body
        var anfLiteral = new AnfTree.LiteralNode.FunctionReferenceNode(
          imp.Position,
          imp.ID,
          imp.Signature
        );
        var imm = new AnfTree.ImmediateNode.ConstantNode(node.Position, anfLiteral, imp.Signature);
        var expr = new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(imp.Position, imm, imp.Signature);
        instructions.Add(new AnfTree.InstructionNode.BindNode(imp.Position, imp.ID, expr));
      }
      // Map the body
      foreach (var stmt in node.Body.Statements) {
        var (binds, instr) = FromStatementNode(moduleState, stmt);
        instructions.AddRange(binds);
        instructions.Add(instr);
      }
      var body = new AnfTree.InstructionNode.BlockNode(node.Body.Position, instructions.ToArray());
      // Produce the new module node
      return new AnfTree.ModuleNode(
        node.Position, node.ID, imports.ToArray(), moduleState.ModuleFunctions.ToArray(), body, node.Signature
      );
    }
    #endregion
    // --- Statements ---
    #region Statements
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode) FromStatementNode(
      AnfState state, TypedTree.StatementNode node
    ) {
      return node switch {
        TypedTree.StatementNode.BlockNode blockNode => ([], FromBlockStatementNode(state, blockNode)),
        TypedTree.StatementNode.VariableDeclNode variableDeclNode => ([], FromVariableDeclStatementNode(state, variableDeclNode)),
        TypedTree.StatementNode.AssignmentNode assignmentNode => FromAssignmentStatementNode(state, assignmentNode),
        TypedTree.StatementNode.IfNode ifNode => FromIfStatementNode(state, ifNode),
        TypedTree.StatementNode.WhileNode whileNode => FromWhileStatementNode(state, whileNode),
        TypedTree.StatementNode.ReturnNode returnNode => FromReturnStatementNode(state, returnNode),
        TypedTree.StatementNode.ContinueNode continueNode => ([], FromContinueStatementNode(state, continueNode)),
        TypedTree.StatementNode.BreakNode breakNode => ([], FromBreakStatementNode(state, breakNode)),
        TypedTree.StatementNode.ExprStatementNode exprStatementNode => FromExprStatementNode(state, exprStatementNode),
        _ => throw new Exception($"Unknown statement node: {node.Kind}")
      };
    }
    private static AnfTree.InstructionNode.BlockNode FromBlockStatementNode(AnfState state, TypedTree.StatementNode.BlockNode node) {
      // Create a new state for the block since it introduces a new scope
      // Map the statements
      var statements = new List<AnfTree.InstructionNode>();
      foreach (var stmt in node.Statements) {
        var (binds, instr) = FromStatementNode(state, stmt);
        statements.AddRange(binds);
        statements.Add(instr);
      }
      // Produce the new block node
      // NOTE: This will never produce any binds since the block itself captures all of its variables
      return new AnfTree.InstructionNode.BlockNode(node.Position, statements.ToArray());
    }
    private static AnfTree.InstructionNode FromVariableDeclStatementNode(
      AnfState state, TypedTree.StatementNode.VariableDeclNode node
    ) {
      // Map the binds
      var instructions = new List<AnfTree.InstructionNode>();
      foreach (var bind in node.Binds) {
        // Map the expression
        var (bindBinds, expr) = FromExpressionNodeAsSimpleExpr(state, bind.InitExpr);
        // Create the bind
        var anfBind = new AnfTree.InstructionNode.BindNode(bind.Position, bind.ID, expr);
        // Add the binds to our list of binds
        instructions.AddRange(bindBinds);
        instructions.Add(anfBind);
      }
      // If there is a single bind we can just return it, otherwise we need to create a block to capture all of the binds
      if (instructions.Count == 1) return instructions[0];
      else return new AnfTree.InstructionNode.BlockNode(node.Position, instructions.ToArray());
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode) FromAssignmentStatementNode(
      AnfState state, TypedTree.StatementNode.AssignmentNode node
    ) {
      var (exprBinds, imm) = FromExpressionNodeAsImm(state, node.Expression);
      var (locationBinds, location) = FromLocationNode(state, node.Location);
      return (
        [.. locationBinds, .. exprBinds],
        new AnfTree.InstructionNode.AssignmentNode(
          node.Position,
          location,
          imm
        )
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode) FromIfStatementNode(
      AnfState state, TypedTree.StatementNode.IfNode node
    ) {
      // Map the condition
      var (binds, imm) = FromExpressionNodeAsImm(state, node.Condition);
      // Map the branches
      var (trueBinds, _trueBranch) = FromStatementNode(state, node.TrueBranch);
      var trueBranch =
        trueBinds.Count > 0 ?
        new AnfTree.InstructionNode.BlockNode(node.TrueBranch.Position, [.. trueBinds, _trueBranch]) :
        _trueBranch;
      var (falseBinds, _falseBranch) = node.FalseBranch != null ? FromStatementNode(state, node.FalseBranch) : ([], null);
      // NOTE: if falseBranch is null then falseBinds should be 0, but we check both just to be safe
      var falseBranch =
        falseBinds.Count > 0 && _falseBranch != null ?
        new AnfTree.InstructionNode.BlockNode(node.FalseBranch.Position, [.. falseBinds, _falseBranch]) :
        _falseBranch;
      // Emit the anf instruction
      return (
        binds,
        new AnfTree.InstructionNode.IfNode(
          node.Position,
          imm,
          trueBranch,
          falseBranch
        )
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode) FromWhileStatementNode(
      AnfState state, TypedTree.StatementNode.WhileNode node
    ) {
      // Map the condition
      var (binds, imm) = FromExpressionNodeAsImm(state, node.Condition);
      // Map the body
      var (bodyBinds, _body) = FromStatementNode(state, node.Body);
      var body =
        bodyBinds.Count > 0 ?
        new AnfTree.InstructionNode.BlockNode(node.Body.Position, [.. bodyBinds, _body]) :
        _body;
      // Build our anf generic loop
      return (
        [],
        new AnfTree.InstructionNode.LoopNode(
          node.Position,
          new AnfTree.InstructionNode.BlockNode(
            node.Body.Position,
            [
              .. binds,
              new AnfTree.InstructionNode.IfNode(
                node.Condition.Position,
                imm,
                body,
                new AnfTree.InstructionNode.BlockNode(
                  node.Body.Position,
                  [
                    new AnfTree.InstructionNode.BreakNode(node.Body.Position)
                  ]
                ))
            ]
          )
        )
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode) FromReturnStatementNode(
      AnfState state, TypedTree.StatementNode.ReturnNode node
    ) {
      if (node.Value != null) {
        var (binds, imm) = FromExpressionNodeAsImm(state, node.Value);
        return (
          binds,
          new AnfTree.InstructionNode.ReturnNode(node.Position, imm)
        );
      }
      else {
        return (
          [],
          new AnfTree.InstructionNode.ReturnNode(node.Position, null)
        );
      }
    }
    private static AnfTree.InstructionNode.ContinueNode FromContinueStatementNode(
      AnfState state, TypedTree.StatementNode.ContinueNode node
    ) {
      return new AnfTree.InstructionNode.ContinueNode(node.Position);
    }
    private static AnfTree.InstructionNode.BreakNode FromBreakStatementNode(
      AnfState state, TypedTree.StatementNode.BreakNode node
    ) {
      return new AnfTree.InstructionNode.BreakNode(node.Position);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.SimpleExprInstructionNode) FromExprStatementNode(
      AnfState state, TypedTree.StatementNode.ExprStatementNode node
    ) {
      var (binds, expr) = FromExpressionNodeAsSimpleExpr(state, node.Expression);
      return (
        binds,
        new AnfTree.InstructionNode.SimpleExprInstructionNode(
          node.Position,
          expr
        )
      );
    }
    #endregion
    // --- Expressions ---
    #region Expressions
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromExpressionNodeAsImm(
      AnfState state,
      TypedTree.ExpressionNode node
    ) {
      switch (node) {
        case TypedTree.ExpressionNode.PrefixNode prefixNode: {
            var (binds, expr) = FromPrefixExpressionNode(state, prefixNode);
            var (tempBind, tempImm) = GenerateTempBind(state, expr, node.ExpressionType);
            return ([.. binds, tempBind], tempImm);
          }
        case TypedTree.ExpressionNode.BinopNode binopNode: {
            var (binds, expr) = FromBinopExpressionNode(state, binopNode);
            var (tempBind, tempImm) = GenerateTempBind(state, expr, node.ExpressionType);
            return ([.. binds, tempBind], tempImm);
          }
        case TypedTree.ExpressionNode.CallNode callNode: {
            var (binds, expr) = FromCallExpressionNode(state, callNode);
            var (tempBind, tempImm) = GenerateTempBind(state, expr, node.ExpressionType);
            return ([.. binds, tempBind], tempImm);
          }
        case TypedTree.ExpressionNode.PrimCallNode primCallNode: {
            var (binds, expr) = FromPrimCallExpressionNode(state, primCallNode);
            var (tempBind, tempImm) = GenerateTempBind(state, expr, node.ExpressionType);
            return ([.. binds, tempBind], tempImm);
          }
        case TypedTree.ExpressionNode.ArrayInitNode arrayInitNode: {
            var (binds, expr) = FromArrayInitExpressionNode(state, arrayInitNode);
            var (tempBind, tempImm) = GenerateTempBind(state, expr, node.ExpressionType);
            return ([.. binds, tempBind], tempImm);
          }
        case TypedTree.ExpressionNode.LocationExprNode locationExprNode:
          return FromLocationExpressionNode(state, locationExprNode);
        case TypedTree.ExpressionNode.LiteralExprNode literalExprNode:
          return FromLiteralExpressionNode(state, literalExprNode);
        // NOTE: The above cases should cover all of the simple expressions, so if we hit the default case then we have an error
        default: throw new Exception($"Unknown expression node: {node.Kind}");
      }
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.SimpleExpressionNode) FromExpressionNodeAsSimpleExpr(
      AnfState state,
      TypedTree.ExpressionNode node
    ) {
      switch (node) {
        case TypedTree.ExpressionNode.PrefixNode prefixNode:
          return FromPrefixExpressionNode(state, prefixNode);
        case TypedTree.ExpressionNode.BinopNode binopNode:
          return FromBinopExpressionNode(state, binopNode);
        case TypedTree.ExpressionNode.CallNode callNode:
          return FromCallExpressionNode(state, callNode);
        case TypedTree.ExpressionNode.PrimCallNode primCallNode:
          return FromPrimCallExpressionNode(state, primCallNode);
        case TypedTree.ExpressionNode.ArrayInitNode arrayInitNode:
          return FromArrayInitExpressionNode(state, arrayInitNode);
        case TypedTree.ExpressionNode.LocationExprNode locationExprNode: {
            var (binds, imm) = FromLocationExpressionNode(state, locationExprNode);
            var expr = new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
              locationExprNode.Position, imm, locationExprNode.ExpressionType
            );
            return (binds, expr);
          }
        case TypedTree.ExpressionNode.LiteralExprNode literalExprNode: {
            var (binds, imm) = FromLiteralExpressionNode(state, literalExprNode);
            var expr = new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
              literalExprNode.Position, imm, literalExprNode.ExpressionType
            );
            return (binds, expr);
          }
        // NOTE: The above cases should cover all of the simple expressions, so if we hit the default case then we have an error
        default: throw new Exception($"Unknown expression node: {node.Kind}");
      }
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.SimpleExpressionNode) FromPrefixExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.PrefixNode node
    ) {
      // Map the Operand
      var (binds, imm) = FromExpressionNodeAsImm(state, node.Operand);
      // Create the mapped anf node
      return (
        [.. binds],
        new AnfTree.SimpleExpressionNode.PrefixNode(node.Position, node.Operator, imm, node.ExpressionType)
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.SimpleExpressionNode) FromBinopExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.BinopNode node
    ) {
      // Map the values
      var (leftBinds, leftImm) = FromExpressionNodeAsImm(state, node.Lhs);
      var (rightBinds, rightImm) = FromExpressionNodeAsImm(state, node.Rhs);
      // Create the mapped anf node
      var anfNode = new AnfTree.SimpleExpressionNode.BinopNode(node.Position, leftImm, node.Operator, rightImm, node.ExpressionType);
      // Return the mapped node and binds
      return ([.. leftBinds, .. rightBinds], anfNode);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.SimpleExpressionNode) FromCallExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.CallNode node
    ) {
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      // Map the location
      var (locationBinds, locationImm) = FromLocationNode(state, node.Callee);
      binds.AddRange(locationBinds);
      // Map the arguments
      var args = new List<AnfTree.ImmediateNode>();
      foreach (var arg in node.Arguments) {
        var (argBinds, argImm) = FromExpressionNodeAsImm(state, arg);
        binds.AddRange(argBinds);
        args.Add(argImm);
      }
      // Create the anf node
      var anfNode = new AnfTree.SimpleExpressionNode.CallNode(
        node.Position, locationImm, args.ToArray(), node.ExpressionType
      );
      // Return the binds and the imm
      return (binds, anfNode);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.SimpleExpressionNode) FromPrimCallExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.PrimCallNode node
    ) {
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      // Map the arguments
      var args = new List<AnfTree.ImmediateNode>();
      foreach (var arg in node.Arguments) {
        var (argBinds, argImm) = FromExpressionNodeAsImm(state, arg);
        binds.AddRange(argBinds);
        args.Add(argImm);
      }
      // Create the anf node
      var anfNode = new AnfTree.SimpleExpressionNode.PrimCallNode(
        node.Position, node.Callee, args.ToArray(), node.ExpressionType
      );
      // Return the binds and the imm
      return (binds, anfNode);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.SimpleExpressionNode) FromArrayInitExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.ArrayInitNode node
    ) {
      // Map the size expression
      var (sizeBinds, sizeImm) = FromExpressionNodeAsImm(state, node.SizeExpr);
      // Crate the mapped node
      var anfNode = new AnfTree.SimpleExpressionNode.ArrayInitNode(node.Position, sizeImm, node.ExpressionType);
      // Return the binds and the imm
      return (sizeBinds, anfNode);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromLocationExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.LocationExprNode node
    ) {
      // NOTE: Mapping locations are a bit weird because they can appear both as a simple expression and as an immediate,
      //       The solution to this is to map them to a special ImmediateExpressionNode and optimize it away during constant propagation.
      var (binds, location) = FromLocationNode(state, node.Location);
      var imm = new AnfTree.ImmediateNode.LocationImmNode(node.Position, location, node.ExpressionType);
      // Return the binds and the imm
      return (binds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromLiteralExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.LiteralExprNode node
    ) {
      // NOTE: Mapping literals is a bit weird because they can appear both as a simple expression and as an immediate,
      //       The solution to this is to map them to a special ImmediateExpressionNode and optimize it away during constant propagation.
      switch (node.Literal) {
        // Most literal transformations are 1 to 1
        case TypedTree.LiteralNode.IntegerNode intNode: {
            var anfLiteral = new AnfTree.LiteralNode.IntegerNode(intNode.Position, intNode.Value, intNode.LiteralType);
            var imm = new AnfTree.ImmediateNode.ConstantNode(node.Position, anfLiteral, node.ExpressionType);
            return ([], imm);
          }
        case TypedTree.LiteralNode.BooleanNode boolNode: {
            var anfLiteral = new AnfTree.LiteralNode.BooleanNode(boolNode.Position, boolNode.Value, boolNode.LiteralType);
            var imm = new AnfTree.ImmediateNode.ConstantNode(node.Position, anfLiteral, node.ExpressionType);
            return ([], imm);
          }
        case TypedTree.LiteralNode.CharacterNode characterNode: {
            var anfLiteral = new AnfTree.LiteralNode.CharacterNode(characterNode.Position, characterNode.Value, characterNode.LiteralType);
            var imm = new AnfTree.ImmediateNode.ConstantNode(node.Position, anfLiteral, node.ExpressionType);
            return ([], imm);
          }
        case TypedTree.LiteralNode.StringNode stringNode: {
            var anfLiteral = new AnfTree.LiteralNode.StringNode(stringNode.Position, stringNode.Value, stringNode.LiteralType);
            var imm = new AnfTree.ImmediateNode.ConstantNode(node.Position, anfLiteral, node.ExpressionType);
            return ([], imm);
          }
        // Functions are a slightly more complex special case
        case TypedTree.LiteralNode.FunctionNode functionNode: {
            // Create a new state for the function
            var newState = new AnfState(state.SymbolGenerator, []);
            // Convert the parameters to the ANF tree variants
            var parameters = new List<AnfTree.FunctionNode.ParameterNode>();
            foreach (var param in functionNode.Parameters) {
              // Parameters map 1 to 1
              parameters.Add(new AnfTree.FunctionNode.ParameterNode(param.Position, param.ID, param.Signature));
            }
            // Convert the body to the ANF tree variant
            var body = FromBlockStatementNode(newState, functionNode.Body);
            // Create the ANF function literal
            var anfNode = new AnfTree.FunctionNode(
              functionNode.Position,
              functionNode.ID,
              parameters.ToArray(),
              body,
              // NOTE: This cast is safe because we refine the input on a function literal itself
              functionNode.LiteralType as Signature.Signature.FunctionSig
            );
            // Add the function to the module context
            state.ModuleFunctions.Add(anfNode);
            // Return an immediate that references the function
            var anfLiteral = new AnfTree.LiteralNode.FunctionReferenceNode(
              functionNode.Position,
              functionNode.ID,
              functionNode.LiteralType
            );
            var imm = new AnfTree.ImmediateNode.ConstantNode(node.Position, anfLiteral, node.ExpressionType);
            return ([], imm);
          }
        // NOTE: The above cases should cover all of the literal nodes, so if we hit this, then we have an error
        default: throw new Exception($"Unknown literal node: {node.Kind}");
      }
    }
    #endregion
    // --- Locations ---
    #region Locations
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode) FromLocationNode(
      AnfState state,
      TypedTree.LocationNode node
    ) {
      return node switch {
        TypedTree.LocationNode.ArrayNode arrayNode => FromArrayLocationNode(state, arrayNode),
        TypedTree.LocationNode.SymbolLocation symbolNode => FromSymbolLocationNode(state, symbolNode),
        // NOTE: The above cases should cover all of the location nodes, so if we hit this, then we have an error
        _ => throw new Exception($"Unknown location node: {node.Kind}"),
      };
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode.ArrayNode) FromArrayLocationNode(
      AnfState state,
      TypedTree.LocationNode.ArrayNode node
    ) {
      var (binds, root) = FromLocationNode(state, node.Root);
      var (indexBinds, indexImm) = FromExpressionNodeAsImm(state, node.IndexExpr);
      return (
        [
          .. binds,
          .. indexBinds
        ],
        new AnfTree.LocationNode.ArrayNode(node.Position, root, indexImm, node.LocationType)
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode.SymbolLocation) FromSymbolLocationNode(
      AnfState _,
      TypedTree.LocationNode.SymbolLocation node
    ) {
      return ([], new AnfTree.LocationNode.SymbolLocation(node.Position, node.ID, node.LocationType));
    }
    #endregion
  }
}
