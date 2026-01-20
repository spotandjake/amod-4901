using System;
using System.Linq;
using System.Text.Json.Serialization;

class ParseTree {
  public enum NodeKind {
    ProgramNode,
    ClassDeclNode,
    MethodDeclNode,
    BlockNode,
    VariableDeclarationNode,
    TypeNode,
    // Statements
    AssignmentNode,
    MethodCallNode,
    IfNode,
    WhileNode,
    ReturnNode,
    // Expressions
    SimpleExpressionNode,
    BinopExpressionNode,
    PrefixExpressionNode,
    // Literals
    IntegerLiteralNode,
    CharLiteralNode,
    BoolLiteralNode,
    NullLiteralNode
  }
  // TODO: Add Positions
  public abstract class Node {
    public abstract NodeKind Kind { get; }
  };
  public class ProgramNode : Node {
    public override NodeKind Kind => NodeKind.ProgramNode;
    public ClassNode[] Classes { get; }
    public ProgramNode(DecafParser.ProgramContext context) {
      this.Classes = context.class_decl().Select(
        classCtx => new ClassNode(classCtx)
      ).ToArray();
    }
  };
  public class ClassNode : Node {
    public override NodeKind Kind => NodeKind.ClassDeclNode;
    public string Name { get; }
#nullable enable
    public string? SuperClassName { get; }
    public VariableDeclarationNode[] VariableDeclarations { get; }
    public MethodDeclarationNode[] MethodDeclarations { get; }
    public ClassNode(DecafParser.Class_declContext context) {
      this.Name = context.ID(0).GetText();
      this.SuperClassName = context.ID().Length > 1 ? context.ID(1).GetText() : null;
      this.VariableDeclarations = context.var_decl().Select(
        varDeclCtx => new VariableDeclarationNode(varDeclCtx)
      ).ToArray();
      this.VariableDeclarations = context.var_decl().Select(
        varDeclCtx => new VariableDeclarationNode(varDeclCtx)
      ).ToArray();
      this.MethodDeclarations = context.method_decl().Select(
        methodDeclCtx => new MethodDeclarationNode(methodDeclCtx)
      ).ToArray();
    }
  }
  public class MethodDeclarationNode : Node {
    public override NodeKind Kind => NodeKind.MethodDeclNode;
    // public TypeNode ReturnType { get; }
    public string Name { get; }
    // TODO: Add Parameters
    public BlockNode Body { get; }
    public MethodDeclarationNode(DecafParser.Method_declContext context) {
      // this.ReturnType = new TypeNode(context.type()); // TODO: How do we want to handle VOID type?
      this.Name = context.ID().GetText();
      // TODO: Parse Parameters
      this.Body = new BlockNode(context.block());
    }
  }
  public class BlockNode : Node {
    public override NodeKind Kind => NodeKind.BlockNode;
    public VariableDeclarationNode[] VariableDeclarations { get; }
    public StatementNode[] Statements { get; }
    public BlockNode(DecafParser.BlockContext context) {
      this.VariableDeclarations = context.var_decl().Select(
        varDeclCtx => new VariableDeclarationNode(varDeclCtx)
      ).ToArray();
      this.Statements = context.statement().Select(
        stmtCtx => StatementNode.FromContext(stmtCtx)
      ).ToArray();
    }
  }
  public class VariableDeclarationNode : Node {
    public override NodeKind Kind => NodeKind.VariableDeclarationNode;
    public TypeNode VarType { get; }
    public string[] VarNames { get; }
    public VariableDeclarationNode(DecafParser.Var_declContext context) {
      this.VarType = new TypeNode(context.type());
      this.VarNames = context.var_bind_list().var_bind().Select(
        varBindCtx => varBindCtx.ID().GetText()
      ).ToArray();
      // TODO: Handle Arrays
    }
  }
  public class TypeNode : Node {
    public override NodeKind Kind => NodeKind.TypeNode;
    public string Type { get; }
    public TypeNode(DecafParser.TypeContext context) {
      this.Type = context.GetText();
    }
  }
  [JsonDerivedType(typeof(AssignmentNode), "AssignmentStatement")]
  [JsonDerivedType(typeof(MethodCallNode), "MethodCallStatement")]
  [JsonDerivedType(typeof(IfNode), "IfStatement")]
  [JsonDerivedType(typeof(WhileNode), "WhileStatement")]
  [JsonDerivedType(typeof(ReturnNode), "ReturnStatement")]
  public abstract class StatementNode : Node {
    public static StatementNode FromContext(DecafParser.StatementContext context) {
      switch (context) {
        case DecafParser.AssignStatementContext assignCtx:
          return new AssignmentNode(assignCtx.assign_stmt());
        case DecafParser.MethodCallStatementContext methodCallCtx:
          return new MethodCallNode(methodCallCtx.method_call());
        case DecafParser.IfStatementContext ifCtx:
          return new IfNode(ifCtx.if_stmt());
        case DecafParser.WhileStatementContext whileCtx:
          return new WhileNode(whileCtx.while_stmt());
        case DecafParser.ReturnStatementContext returnCtx:
          return new ReturnNode(returnCtx.return_stmt());
        default:
          // NOTE: This should be impossible due to grammar restrictions
          throw new InvalidProgramException("Impossible statement at StatementNode.FromContext");
      }
    }
  };
  public class AssignmentNode : StatementNode {
    public override NodeKind Kind => NodeKind.AssignmentNode;
    public string Location { get; }
    public ExpressionNode Expression { get; }
    public AssignmentNode(DecafParser.Assign_stmtContext context) {
      // TODO: Implement Location
      this.Location = context.location().GetText();
      this.Expression = ExpressionNode.FromContext(context.expr());
    }
  };
  // TODO: Implement MethodCallNode
  public class MethodCallNode : StatementNode {
    public override NodeKind Kind => NodeKind.MethodCallNode;
    public MethodCallNode(DecafParser.Method_callContext context) {
    }
  };
  public class IfNode : StatementNode {
    public override NodeKind Kind => NodeKind.IfNode;
    public ExpressionNode Condition { get; }
    public BlockNode TrueBranch { get; }
#nullable enable
    public BlockNode? FalseBranch { get; }
    public IfNode(DecafParser.If_stmtContext context) {
      this.Condition = ExpressionNode.FromContext(context.expr());
      this.TrueBranch = new BlockNode(context.block(0));
      this.FalseBranch = context.block().Length > 1 ? new BlockNode(context.block(1)) : null;
    }
  };
  public class WhileNode : StatementNode {
    public override NodeKind Kind => NodeKind.WhileNode;
    public ExpressionNode Condition { get; }
    public BlockNode Body { get; }
    public WhileNode(DecafParser.While_stmtContext context) {
      this.Condition = ExpressionNode.FromContext(context.expr());
      this.Body = new BlockNode(context.block());
    }
  };
  public class ReturnNode : StatementNode {
    public override NodeKind Kind => NodeKind.ReturnNode;
    public ExpressionNode? Expression { get; }
    public ReturnNode(DecafParser.Return_stmtContext context) {
      if (context.expr() != null) {
        this.Expression = ExpressionNode.FromContext(context.expr());
      }
      else {
        this.Expression = null;
      };
    }
  };
  [JsonDerivedType(typeof(SimpleExpressionNode), "SimpleExpression")]
  // [JsonDerivedType(typeof(MethodCallNode), "MethodCallStatement")]
  // [JsonDerivedType(typeof(IfNode), "IfStatement")]
  [JsonDerivedType(typeof(BinopExpressionNode), "BinopExpression")]
  [JsonDerivedType(typeof(PrefixExpressionNode), "PrefixExpression")]
  // Literal SubTypes
  [JsonDerivedType(typeof(IntegerLiteralNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(CharLiteralNode), "CharLiteral")]
  [JsonDerivedType(typeof(BoolLiteralNode), "BoolLiteral")]
  [JsonDerivedType(typeof(NullLiteralNode), "NullLiteral")]
  public abstract class ExpressionNode : Node {
    public static ExpressionNode FromContext(DecafParser.ExprContext context) {
      switch (context) {
        case DecafParser.SimpleExprContext simpleExprCtx:
          return new SimpleExpressionNode(simpleExprCtx.simple_expr());
        case DecafParser.NewObjectExprContext newObjExprCtx:
          // TODO: Implement NewObjectExpressionNode
          throw new NotImplementedException();
        case DecafParser.NewArrayExprContext newArrExprCtx:
          // TODO: Implement NewArrayExpressionNode
          throw new NotImplementedException();
        case DecafParser.LiteralExprContext literalExprCtx:
          return LiteralNode.FromContext(literalExprCtx.literal());
        case DecafParser.BinaryOpExprContext binopExprCtx:
          return new BinopExpressionNode(binopExprCtx);
        case DecafParser.NotExprContext prefixExprCtx:
          return new PrefixExpressionNode(prefixExprCtx);
        case DecafParser.ParenExprContext parenExprCtx:
          return ExpressionNode.FromContext(parenExprCtx.expr());
        default:
          // NOTE: This should be impossible due to grammar restrictions
          throw new InvalidProgramException("Impossible expression at ExpressionNode.FromContext");
      }
    }
  };
  // TODO: Implement SimpleExpressionNode
  public class SimpleExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.SimpleExpressionNode;
    public SimpleExpressionNode(DecafParser.Simple_exprContext context) {
    }
  };
  public class BinopExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.BinopExpressionNode;
    public ExpressionNode Left { get; }
    public string Operator { get; }
    public ExpressionNode Right { get; }
    public BinopExpressionNode(DecafParser.BinaryOpExprContext context) {
      this.Left = ExpressionNode.FromContext(context.expr(0));
      this.Operator = context.bin_op().GetText();
      this.Right = ExpressionNode.FromContext(context.expr(1));
    }
  };
  public class PrefixExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.PrefixExpressionNode;
    public string Operator { get; }
    public ExpressionNode Operand { get; }
    public PrefixExpressionNode(DecafParser.NotExprContext context) {
      this.Operator = context.NOT().GetText();
      this.Operand = ExpressionNode.FromContext(context.expr());
    }
  };
  [JsonDerivedType(typeof(IntegerLiteralNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(CharLiteralNode), "CharLiteral")]
  [JsonDerivedType(typeof(BoolLiteralNode), "BoolLiteral")]
  [JsonDerivedType(typeof(NullLiteralNode), "NullLiteral")]
  public abstract class LiteralNode : ExpressionNode {
    public static LiteralNode FromContext(DecafParser.LiteralContext context) {
      switch (context) {
        case DecafParser.IntLitContext intLitCtx:
          return new IntegerLiteralNode(intLitCtx);
        case DecafParser.CharLitContext charLitCtx:
          return new CharLiteralNode(charLitCtx);
        case DecafParser.BoolLitContext boolLitCtx:
          return new BoolLiteralNode(boolLitCtx.bool_literal());
        case DecafParser.NullLitContext nullLitCtx:
          return new NullLiteralNode(nullLitCtx);
        default:
          // NOTE: This should be impossible due to grammar restrictions
          throw new InvalidProgramException("Impossible literal at LiteralNode.FromContext");
      }
    }
  };
  public class IntegerLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.IntegerLiteralNode;
    public int Value { get; }
    public IntegerLiteralNode(DecafParser.IntLitContext context) {
      // TODO: Handle Proper Integer Parsing
      this.Value = int.Parse(context.INTLIT().GetText());
    }
  };
  public class CharLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.CharLiteralNode;
    public char Value { get; }
    public CharLiteralNode(DecafParser.CharLitContext context) {
      // TODO: Handle Proper Char Parsing
      this.Value = context.CHARLIT().GetText()[1]; // Skip the quotes
    }
  };
  public class BoolLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.BoolLiteralNode;
    public bool Value { get; }
    public BoolLiteralNode(DecafParser.Bool_literalContext context) {
      this.Value = context.TRUE() != null;
    }
  };
  public class NullLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.NullLiteralNode;
    public NullLiteralNode(DecafParser.NullLitContext context) {
      if (context.NULL() != null) {
        throw new InvalidProgramException("Impossible null in NUllLiteralNode");
      }
    }
  };
}
