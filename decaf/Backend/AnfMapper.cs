using AnfTree = Decaf.IR.AnfTree;
using TypedTree = Decaf.IR.TypedTree;

using System;
using System.Linq;
using System.Collections.Generic;
using Decaf.Utils;
using Decaf.IR.TypedTree;

// The purpose of this file is to map from the typed tree to the ANF tree.
namespace Decaf.Backend {
  public static class AnfMapper {
#nullable enable
    private record AnfState(Scope<TypedTree.Signature> CurrentScope, string? CurrentModule = null) {
      public string? CurrentModule { get; } = CurrentModule;
#nullable disable
      public Scope<TypedTree.Signature> CurrentScope { get; } = CurrentScope;
      public int TempCounter { get; set; } = 0;
    }
    private static (AnfTree.InstructionNode.BindNode, AnfTree.ImmediateNode.LocationAccessNode) GenerateTempBind(
      AnfState state, AnfTree.ExpressionNode value, Signature signature
    ) {
      // Create the binding name
      // TODO: Should we be adding this to the current scope?
      // TODO: Produce a better name (Maybe make it context aware???)
      var tempName = "__anf_temp_" + state.TempCounter;
      state.TempCounter += 1;
      var tempLocation = new AnfTree.LocationNode.IdentifierAccessNode(value.Position, tempName, value.ExpressionType);
      // Create the bind
      var bind = new AnfTree.InstructionNode.BindNode(
        value.Position,
        tempLocation,
        value
      );
      // Create the imm
      var imm = new AnfTree.ImmediateNode.LocationAccessNode(value.Position, tempLocation, signature);
      // Return the bind and the imm
      return (bind, imm);
    }
    public static AnfTree.ProgramNode FromProgramNode(TypedTree.ProgramNode node) {
      // No mapping is required on the program node, since it is just a container
      return new AnfTree.ProgramNode(
        node.Position,
        node.Modules.Select((c) => FromModuleNode(new AnfState(node.Scope, null), c)).ToArray()
      );
    }
    // Declarations
    private static AnfTree.DeclarationNode.ModuleNode FromModuleNode(
      AnfState state, TypedTree.DeclarationNode.ModuleNode node
    ) {
      // Map the fields to property nodes (We do this inline because variable declarations are not a part of the anf tree)
      var fields = new List<AnfTree.DeclarationNode.GlobalNode>();
      foreach (var field in node.Fields) {
        foreach (var bind in field.Binds) {
          fields.Add(new AnfTree.DeclarationNode.GlobalNode(bind.Position, bind.Name, bind.Signature));
        }
      }
      var moduleState = new AnfState(node.Scope, node.Name);
      // Produce the new module node
      return new AnfTree.DeclarationNode.ModuleNode(
        node.Position,
        node.Name,
        fields.ToArray(),
        node.Methods.Select((m) => FromMethodNode(moduleState, m)).ToArray(),
        node.Signature
      );
    }
    private static AnfTree.DeclarationNode.MethodNode FromMethodNode(AnfState state, TypedTree.DeclarationNode.MethodNode node) {
      var newState = new AnfState(node.Scope, state.CurrentModule);
      return new AnfTree.DeclarationNode.MethodNode(
        node.Position,
        node.Name,
        node.Parameters.Select(
          param => new AnfTree.DeclarationNode.MethodNode.ParameterNode(param.Position, param.Name, param.Signature)
        ).ToArray(),
        // NOTE: We generate a new state here because we want to reset the temp counter as it can be scoped per method
        FromBlockNode(newState, node.Body),
        node.Signature
      );
    }
    // General
    private static AnfTree.InstructionNode.BlockNode FromBlockNode(AnfState state, TypedTree.BlockNode node) {
      var newState = new AnfState(node.Scope, state.CurrentModule);
      // TODO: We probably need to map our declarations into something????
      // Map the statements
      var statements = new List<AnfTree.InstructionNode>();
      foreach (var stmt in node.Statements) {
        var (binds, instr) = FromStatementNode(newState, stmt);
        statements.AddRange(binds);
        statements.Add(instr);
      }
      // Produce the new block node
      return new AnfTree.InstructionNode.BlockNode(node.Position, statements.ToArray());
    }
    // Statements
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode) FromStatementNode(
      AnfState state,
      TypedTree.StatementNode node
    ) {
      // We need to map each statement individually
      switch (node) {
        case TypedTree.StatementNode.AssignmentNode assignNode:
          return FromAssignmentStmtNode(state, assignNode);
        case TypedTree.StatementNode.ExprNode exprNode:
          return FromExprStmtNode(state, exprNode);
        case TypedTree.StatementNode.IfNode ifNode:
          return FromIfStmtNode(state, ifNode);
        case TypedTree.StatementNode.WhileNode whileNode:
          return FromWhileStmtNode(state, whileNode);
        case TypedTree.StatementNode.ContinueNode continueNode:
          return FromContinueStmtNode(state, continueNode);
        case TypedTree.StatementNode.BreakNode breakNode:
          return FromBreakStmtNode(state, breakNode);
        case TypedTree.StatementNode.ReturnNode returnNode:
          return FromReturnStmtNode(state, returnNode);
        default: throw new Exception($"Unknown statement node: {node.Kind}");
      }
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.AssignmentNode) FromAssignmentStmtNode(
      AnfState state,
      TypedTree.StatementNode.AssignmentNode node
    ) {
      var (exprBinds, imm) = FromExpressionNode(state, node.Expression);
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
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.ExprNode) FromExprStmtNode(
      AnfState state,
      TypedTree.StatementNode.ExprNode node
    ) {
      var (binds, imm) = FromExpressionNode(state, node.Content);
      return (
        binds,
        new AnfTree.InstructionNode.ExprNode(
          node.Position,
          imm
        )
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.IfNode) FromIfStmtNode(
      AnfState state,
      TypedTree.StatementNode.IfNode node
    ) {
      var (binds, imm) = FromExpressionNode(state, node.Condition);
      var trueBranch = FromBlockNode(state, node.TrueBranch);
      var falseBranch = node.FalseBranch != null ? FromBlockNode(state, node.FalseBranch) : null;
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
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.LoopNode) FromWhileStmtNode(
      AnfState state,
      TypedTree.StatementNode.WhileNode node
    ) {
      var (binds, imm) = FromExpressionNode(state, node.Condition);
      var body = FromBlockNode(state, node.Body);
      return (
        [],
        // TODO: Consider cleaning up this decomposition
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
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.ContinueNode) FromContinueStmtNode(
      AnfState state,
      TypedTree.StatementNode.ContinueNode node
    ) {
      return (
        [],
        new AnfTree.InstructionNode.ContinueNode(node.Position)
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.BreakNode) FromBreakStmtNode(
      AnfState state,
      TypedTree.StatementNode.BreakNode node
    ) {
      return (
        [],
        new AnfTree.InstructionNode.BreakNode(node.Position)
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.InstructionNode.ReturnNode) FromReturnStmtNode(
      AnfState state,
      TypedTree.StatementNode.ReturnNode node
    ) {
      var (binds, imm) = node.Value switch {
        null => ([], null),
        _ => FromExpressionNode(state, node.Value)
      };
      return (
        binds,
        new AnfTree.InstructionNode.ReturnNode(node.Position, imm)
      );
    }
    // Expressions
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode node
    ) {
      return node switch {
        TypedTree.ExpressionNode.CallNode callNode => FromCallExpressionNode(state, callNode),
        TypedTree.ExpressionNode.PrimitiveNode primitiveNode => FromPrimitiveExpressionNode(state, primitiveNode),
        TypedTree.ExpressionNode.BinopNode binopNode => FromBinopExpressionNode(state, binopNode),
        TypedTree.ExpressionNode.PrefixNode prefixNode => FromPrefixExpressionNode(state, prefixNode),
        TypedTree.ExpressionNode.NewArrayNode newArrayNode => FromNewArrayExpressionNode(state, newArrayNode),
        TypedTree.ExpressionNode.LocationAccessNode locationAccessNode =>
          FromLocationAccessExpressionNode(state, locationAccessNode, node.ExpressionType),
        TypedTree.ExpressionNode.LiteralNode literalNode => FromLiteralExpressionNode(state, literalNode, node.ExpressionType),
        _ => throw new Exception($"Unknown expression node: {node.Kind}"),
      };
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromCallExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.CallNode node
    ) {
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      // Map the location
      var (locationBinds, locationImm) = FromLocationNode(state, node.Path);
      binds.AddRange(locationBinds);
      // Map the arguments
      var args = new List<AnfTree.ImmediateNode>();
      foreach (var arg in node.Arguments) {
        var (argBinds, argImm) = FromExpressionNode(state, arg);
        binds.AddRange(argBinds);
        args.Add(argImm);
      }
      // Create the anf node
      var anfNode = new AnfTree.ExpressionNode.CallNode(
        node.Position, locationImm, args.ToArray(), node.ExpressionType
      );
      // Generate an imm for the result
      var (setup, imm) = GenerateTempBind(state, anfNode, node.ExpressionType);
      binds.Add(setup);
      // Return the binds and the imm
      return (binds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromPrimitiveExpressionNode(
     AnfState state,
     TypedTree.ExpressionNode.PrimitiveNode node
   ) {
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      // Map the arguments
      var args = new List<AnfTree.ImmediateNode>();
      foreach (var arg in node.Arguments) {
        var (argBinds, argImm) = FromExpressionNode(state, arg);
        binds.AddRange(argBinds);
        args.Add(argImm);
      }
      // Create the anf node
      var anfNode = new AnfTree.ExpressionNode.PrimitiveNode(
        node.Position, node.Primitive, args.ToArray(), node.ExpressionType
      );
      // Generate an imm for the result
      var (setup, imm) = GenerateTempBind(state, anfNode, node.ExpressionType);
      binds.Add(setup);
      // Return the binds and the imm
      return (binds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromBinopExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.BinopNode node
    ) {
      // Map the values
      var (leftBinds, leftImm) = FromExpressionNode(state, node.Lhs);
      var (rightBinds, rightImm) = FromExpressionNode(state, node.Rhs);
      // Crate the mapped node
      var anfNode = new AnfTree.ExpressionNode.BinopNode(node.Position, leftImm, node.Operator, rightImm, node.ExpressionType);
      // Generate an imm for the result
      var (setup, imm) = GenerateTempBind(state, anfNode, node.ExpressionType);
      // Construct our binds
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      binds.AddRange(leftBinds);
      binds.AddRange(rightBinds);
      binds.Add(setup);
      // Return the binds and the imm
      return (binds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromPrefixExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.PrefixNode node
    ) {
      // Map the Operand
      var (operandBinds, operandImm) = FromExpressionNode(state, node.Operand);
      // Crate the mapped node
      var anfNode = new AnfTree.ExpressionNode.PrefixNode(node.Position, node.Operator, operandImm, node.ExpressionType);
      // Generate an imm for the result
      var (setup, imm) = GenerateTempBind(state, anfNode, node.ExpressionType);
      // Construct our binds
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      binds.AddRange(operandBinds);
      binds.Add(setup);
      // Return the binds and the imm
      return (binds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromNewArrayExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.NewArrayNode node
    ) {
      // Map the size expression
      var (sizeBinds, sizeImm) = FromExpressionNode(state, node.SizeExpr);
      // Crate the mapped node
      var anfNode = new AnfTree.ExpressionNode.AllocateArrayNode(node.Position, sizeImm, node.ExpressionType);
      // Generate an imm for the result
      var (setup, imm) = GenerateTempBind(state, anfNode, node.ExpressionType);
      // Construct our binds
      var binds = new List<AnfTree.InstructionNode.BindNode>();
      binds.AddRange(sizeBinds);
      binds.Add(setup);
      // Return the binds and the imm
      return (binds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromLocationAccessExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.LocationAccessNode node,
      Signature signature
    ) {
      var (locationBinds, location) = FromLocationNode(state, node.Content);
      var imm = new AnfTree.ImmediateNode.LocationAccessNode(node.Position, location, signature);
      // Return the binds and the imm
      return (locationBinds, imm);
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.ImmediateNode) FromLiteralExpressionNode(
      AnfState state,
      TypedTree.ExpressionNode.LiteralNode node,
      Signature signature
    ) {
      return ([], new AnfTree.ImmediateNode.ConstantNode(node.Position, node.Content, signature));
    }
    // Locations
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode) FromLocationNode(
      AnfState state,
      TypedTree.LocationNode node
    ) {
      switch (node) {
        // TODO: Resolve `This` nodes in the type checker
        case TypedTree.LocationNode.ThisNode _:
          // We handle the resolution here because it is easier to resolve it
          return ([], new AnfTree.LocationNode.IdentifierAccessNode(
            node.Position,
            state.CurrentModule,
            state.CurrentScope.GetDeclaration(node.Position, state.CurrentModule)
          ));
        case TypedTree.LocationNode.IdentifierAccessNode identNode:
          return FromIdentifierAccessLocationNode(state, identNode);
        case TypedTree.LocationNode.MemberAccessNode memberNode:
          return FromMemberAccessLocationNode(state, memberNode);
        case TypedTree.LocationNode.ArrayAccessNode arrayNode:
          return FromArrayAcessLocationNode(state, arrayNode);
        default: throw new Exception($"Unknown location node: {node.Kind}");
      }
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode.IdentifierAccessNode) FromIdentifierAccessLocationNode(
      AnfState state,
      TypedTree.LocationNode.IdentifierAccessNode node
    ) {
      return ([], new AnfTree.LocationNode.IdentifierAccessNode(node.Position, node.Name, node.LocationType));
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode.MemberAccessNode) FromMemberAccessLocationNode(
      AnfState state,
      TypedTree.LocationNode.MemberAccessNode node
    ) {
      var (binds, root) = FromLocationNode(state, node.Root);
      // TODO: I would prefer if we move this resolution up
      // TODO: This is why we should consider lowering directly to `global.get`, `local.set`, `array.get`, `array.set` at the anf level
      if (root is not AnfTree.LocationNode.IdentifierAccessNode) {
        // NOTE: This should be impossible given type checking restrictions
        throw new Exception("Member access root must be an identifier access");
      }
      return (
        binds,
        new AnfTree.LocationNode.MemberAccessNode(
          node.Position,
          (AnfTree.LocationNode.IdentifierAccessNode)root,
          node.Member,
          node.LocationType
        )
      );
    }
    private static (List<AnfTree.InstructionNode.BindNode>, AnfTree.LocationNode.ArrayAccessNode) FromArrayAcessLocationNode(
      AnfState state,
      TypedTree.LocationNode.ArrayAccessNode node
    ) {
      var (binds, root) = FromLocationNode(state, node.Root);
      var (indexBinds, indexImm) = FromExpressionNode(state, node.IndexExpr);
      return (
        [
          .. binds,
          .. indexBinds
        ],
        new AnfTree.LocationNode.ArrayAccessNode(node.Position, root, indexImm, node.LocationType)
      );
    }
  }
}
