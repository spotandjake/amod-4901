using System;
using System.Linq;
using System.Text.Json.Serialization;

public class ParseTree {
  public enum NodeKind {
    ProgramNode,
    ClassDeclNode,
    MethodDeclNode,
    BlockNode,
    VariableDeclarationNode,
    TypeNode,
    // Statements
    AssignmentNode,
    CallNode,
    PrimitiveCallNode,
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
    public ProgramNode(ClassNode[] classes) { this.Classes = classes; }
    public static ProgramNode FromContext(DecafParser.ProgramContext context) {
      var classes = context.class_decl().Select(
        classCtx => ClassNode.FromContext(classCtx)
      ).ToArray();
      return new ProgramNode(classes);
    }
  };
  public class ClassNode : Node {
    public override NodeKind Kind => NodeKind.ClassDeclNode;
    public string Name { get; }
#nullable enable
    public string? SuperClassName { get; }
    public VariableDeclarationNode[] VariableDeclarations { get; }
    public MethodDeclarationNode[] MethodDeclarations { get; }
    public ClassNode(string name, string? superClassName, VariableDeclarationNode[] varDecls, MethodDeclarationNode[] methodDecls) {
      this.Name = name;
      this.SuperClassName = superClassName;
      this.VariableDeclarations = varDecls;
      this.MethodDeclarations = methodDecls;
    }
    public static ClassNode FromContext(DecafParser.Class_declContext context) {
      var name = context.name.Text;
      var superClassName = context.superClassName?.Text;
      var varDecls = context.var_decl().Select(
        varDeclCtx => VariableDeclarationNode.FromContext(varDeclCtx)
      ).ToArray();
      var methodDecls = context.method_decl().Select(
        methodDeclCtx => MethodDeclarationNode.FromContext(methodDeclCtx)
      ).ToArray();
      return new ClassNode(name, superClassName, varDecls, methodDecls);
    }
  }
  public class VariableDeclarationNode : Node {
    public override NodeKind Kind => NodeKind.VariableDeclarationNode;
    public TypeNode VarType { get; }
    public string[] VarBinds { get; }
    public VariableDeclarationNode(TypeNode varType, string[] varBinds) {
      this.VarType = varType;
      this.VarBinds = varBinds;
    }
    public static VariableDeclarationNode FromContext(DecafParser.Var_declContext context) {
      var varType = TypeNode.FromContext(context.typ);
      var varBinds = context.binds.var_bind().Select(
        // TODO: Handle Array Variables
        varBindCtx => varBindCtx.name.Text
      ).ToArray();
      return new VariableDeclarationNode(varType, varBinds);
    }
  }
  public class MethodDeclarationNode : Node {
    public override NodeKind Kind => NodeKind.MethodDeclNode;
    public TypeNode ReturnType { get; }
    public string Name { get; }
    // public ParameterNode[] Parameters { get; }
    public BlockNode Body { get; }
    public MethodDeclarationNode(TypeNode returnType, string name, BlockNode body) {
      this.ReturnType = returnType;
      this.Name = name;
      this.Body = body;
    }
    public static MethodDeclarationNode FromContext(DecafParser.Method_declContext context) {
      var returnType = TypeNode.FromContext(context.returnType);
      var name = context.name.Text;
      // TODO: Handle Parameters
      // var parameters = context.parameters
      var body = BlockNode.FromContext(context.body);
      return new MethodDeclarationNode(returnType, name, body);
    }
  }
  public class BlockNode : Node {
    public override NodeKind Kind => NodeKind.BlockNode;
    public VariableDeclarationNode[] VariableDeclarations { get; }
    public StatementNode[] Statements { get; }
    public BlockNode(VariableDeclarationNode[] varDecls, StatementNode[] statements) {
      this.VariableDeclarations = varDecls;
      this.Statements = statements;
    }
    public static BlockNode FromContext(DecafParser.BlockContext context) {
      var varDecls = context.var_decl().Select(
        varDeclCtx => VariableDeclarationNode.FromContext(varDeclCtx)
      ).ToArray();
      var statements = context.statement().Select(
        stmtCtx => StatementNode.FromContext(stmtCtx)
      ).ToArray();
      return new BlockNode(varDecls, statements);
    }
  }
  public class TypeNode : Node {
    public override NodeKind Kind => NodeKind.TypeNode;
    public string Type { get; }
    public TypeNode(string type) { this.Type = type; }
    public static TypeNode FromContext(DecafParser.TypeContext context) {
      var type = context.GetText();
      return new TypeNode(type);
    }
  }
  [JsonDerivedType(typeof(AssignmentNode), "AssignmentStatement")]
  [JsonDerivedType(typeof(CallNode), "CallStatement")]
  [JsonDerivedType(typeof(PrimitiveCallNode), "PrimitiveCallStatement")]
  [JsonDerivedType(typeof(IfNode), "IfStatement")]
  [JsonDerivedType(typeof(WhileNode), "WhileStatement")]
  [JsonDerivedType(typeof(ReturnNode), "ReturnStatement")]
  public abstract class StatementNode : Node {
    public static StatementNode FromContext(DecafParser.StatementContext context) {
      switch (context) {
        case DecafParser.AssignStatementContext assignCtx:
          return AssignmentNode.FromContext(assignCtx.assign_stmt());
        case DecafParser.CallStatementContext callCtx:
          switch (callCtx.call_stmt().call_expr()) {
            case DecafParser.MethodCallExprContext methodCallCtx:
              return CallNode.FromContext(methodCallCtx.method_call());
            case DecafParser.PrimCalloutExprContext primCalloutCtx:
              return PrimitiveCallNode.FromContext(primCalloutCtx.prim_callout());
            default:
              throw new InvalidProgramException("Impossible call expression at StatementNode.FromContext");
          }
        case DecafParser.IfStatementContext ifCtx:
          return IfNode.FromContext(ifCtx.if_stmt());
        case DecafParser.WhileStatementContext whileCtx:
          return WhileNode.FromContext(whileCtx.while_stmt());
        case DecafParser.ReturnStatementContext returnCtx:
          return ReturnNode.FromContext(returnCtx.return_stmt());
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
    public AssignmentNode(string location, ExpressionNode expression) {
      this.Location = location;
      this.Expression = expression;
    }
    public static AssignmentNode FromContext(DecafParser.Assign_stmtContext context) {
      // TODO: Implement proper Location
      var location = context.location().GetText();
      var expression = ExpressionNode.FromContext(context.expr());
      return new AssignmentNode(location, expression);
    }
  };
  public class CallNode : StatementNode {
    public override NodeKind Kind => NodeKind.CallNode;
    public string MethodPath { get; }
    public ExpressionNode[] Arguments { get; }
    public CallNode(string methodPath, ExpressionNode[] args) {
      this.MethodPath = methodPath;
      this.Arguments = args;
    }
    public static CallNode FromContext(DecafParser.Method_callContext context) {
      // TODO: Implement proper Location
      var methodPath = context.methodPath.GetText();
      var args = context.args.expr().Select(
        exprCtx => ExpressionNode.FromContext(exprCtx)
      ).ToArray();
      return new CallNode(methodPath, args);
    }
  };
  public class PrimitiveCallNode : StatementNode {
    public override NodeKind Kind => NodeKind.PrimitiveCallNode;
    public string PrimitiveId { get; }
    public ExpressionNode[] Arguments { get; }
    public PrimitiveCallNode(string primId, ExpressionNode[] args) {
      this.PrimitiveId = primId;
      this.Arguments = args;
    }
    public static CallNode FromContext(DecafParser.Prim_calloutContext context) {
      var primId = context.primId.Text;
      // TODO: Handle mixed expression and stringlit args
      // var args = context.args.expr().Select(
      //   exprCtx => ExpressionNode.FromContext(exprCtx)
      // ).ToArray();
      return new CallNode(primId, []);
    }
  };
  public class IfNode : StatementNode {
    public override NodeKind Kind => NodeKind.IfNode;
    public ExpressionNode Condition { get; }
    public BlockNode TrueBranch { get; }
#nullable enable
    public BlockNode? FalseBranch { get; }
    public IfNode(ExpressionNode condition, BlockNode trueBranch, BlockNode? falseBranch) {
      this.Condition = condition;
      this.TrueBranch = trueBranch;
      this.FalseBranch = falseBranch;
    }
    public static IfNode FromContext(DecafParser.If_stmtContext context) {
      var condition = ExpressionNode.FromContext(context.condition);
      var trueBranch = BlockNode.FromContext(context.trueBranch);
      var falseBranch = context.falseBranch != null ? BlockNode.FromContext(context.falseBranch) : null;
      return new IfNode(condition, trueBranch, falseBranch);
    }
  };
  public class WhileNode : StatementNode {
    public override NodeKind Kind => NodeKind.WhileNode;
    public ExpressionNode Condition { get; }
    public BlockNode Body { get; }
    public WhileNode(ExpressionNode condition, BlockNode body) {
      this.Condition = condition;
      this.Body = body;
    }
    public static WhileNode FromContext(DecafParser.While_stmtContext context) {
      var condition = ExpressionNode.FromContext(context.condition);
      var body = BlockNode.FromContext(context.body);
      return new WhileNode(condition, body);
    }
  };
  public class ReturnNode : StatementNode {
    public override NodeKind Kind => NodeKind.ReturnNode;
    public ExpressionNode? Value { get; }
    public ReturnNode(ExpressionNode? value) { this.Value = value; }
    public static ReturnNode FromContext(DecafParser.Return_stmtContext context) {
      var value = context.value != null ? ExpressionNode.FromContext(context.value) : null;
      return new ReturnNode(value);
    }
  };
  // TODO: Figure out a cleaner way of handling the ast subtypes
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
          return SimpleExpressionNode.FromContext(simpleExprCtx.simple_expr());
        case DecafParser.NewObjectExprContext newObjExprCtx:
          // TODO: Implement NewObjectExpressionNode
          throw new NotImplementedException();
        case DecafParser.NewArrayExprContext newArrExprCtx:
          // TODO: Implement NewArrayExpressionNode
          throw new NotImplementedException();
        case DecafParser.LiteralExprContext literalExprCtx:
          return LiteralNode.FromContext(literalExprCtx.literal());
        case DecafParser.BinaryOpExprContext binopExprCtx:
          return BinopExpressionNode.FromContext(binopExprCtx);
        case DecafParser.NotExprContext prefixExprCtx:
          return PrefixExpressionNode.FromContext(prefixExprCtx);
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
    public SimpleExpressionNode() { }
    public static SimpleExpressionNode FromContext(DecafParser.Simple_exprContext context) {
      return new SimpleExpressionNode();
    }
  };
  public class BinopExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.BinopExpressionNode;
    public ExpressionNode Lhs { get; }
    public string Operator { get; }
    public ExpressionNode Rhs { get; }
    public BinopExpressionNode(ExpressionNode lhs, string op, ExpressionNode rhs) {
      this.Lhs = lhs;
      this.Operator = op;
      this.Rhs = rhs;
    }
    public static BinopExpressionNode FromContext(DecafParser.BinaryOpExprContext context) {
      var lhs = ExpressionNode.FromContext(context.lhs);
      var op = context.op.GetText();
      var rhs = ExpressionNode.FromContext(context.rhs);
      return new BinopExpressionNode(lhs, op, rhs);
    }
  };
  public class PrefixExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.PrefixExpressionNode;
    public string Operator { get; }
    public ExpressionNode Operand { get; }
    public PrefixExpressionNode(string op, ExpressionNode operand) {
      this.Operator = op;
      this.Operand = operand;
    }
    public static PrefixExpressionNode FromContext(DecafParser.NotExprContext context) {
      var op = context.op.Text;
      var operand = ExpressionNode.FromContext(context.operand);
      return new PrefixExpressionNode(op, operand);
    }
  };
  // TODO: Consider a cleaner way of handling literal subtypes
  [JsonDerivedType(typeof(IntegerLiteralNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(CharLiteralNode), "CharLiteral")]
  [JsonDerivedType(typeof(BoolLiteralNode), "BoolLiteral")]
  [JsonDerivedType(typeof(NullLiteralNode), "NullLiteral")]
  public abstract class LiteralNode : ExpressionNode {
    public static LiteralNode FromContext(DecafParser.LiteralContext context) {
      switch (context) {
        case DecafParser.IntLitContext intLitCtx:
          return IntegerLiteralNode.FromContext(intLitCtx);
        case DecafParser.CharLitContext charLitCtx:
          return CharLiteralNode.FromContext(charLitCtx);
        case DecafParser.BoolLitContext boolLitCtx:
          return BoolLiteralNode.FromContext(boolLitCtx.bool_literal());
        case DecafParser.NullLitContext nullLitCtx:
          return NullLiteralNode.FromContext(nullLitCtx);
        default:
          // NOTE: This should be impossible due to grammar restrictions
          throw new InvalidProgramException("Impossible literal at LiteralNode.FromContext");
      }
    }
  };
  public class IntegerLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.IntegerLiteralNode;
    public int Value { get; }
    public IntegerLiteralNode(int value) {
      this.Value = value;
    }
    public static IntegerLiteralNode FromContext(DecafParser.IntLitContext context) {
      // TODO: Handle Proper Integer Conversion
      var value = int.Parse(context.INTLIT().GetText());
      return new IntegerLiteralNode(value);
    }
  };
  public class CharLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.CharLiteralNode;
    public char Value { get; }
    public CharLiteralNode(char value) {
      this.Value = value;
    }
    public static CharLiteralNode FromContext(DecafParser.CharLitContext context) {
      // TODO: Handle Proper Char Parsing
      var value = context.CHARLIT().GetText()[1]; // Skip the quotes
      return new CharLiteralNode(value);
    }
  };
  public class BoolLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.BoolLiteralNode;
    public bool Value { get; }
    public BoolLiteralNode(bool value) { this.Value = value; }
    public static BoolLiteralNode FromContext(DecafParser.Bool_literalContext context) {
      var value = context.TRUE() != null;
      return new BoolLiteralNode(value);
    }
  };
  public class NullLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.NullLiteralNode;
    public NullLiteralNode() { }
    public static NullLiteralNode FromContext(DecafParser.NullLitContext context) {
      return new NullLiteralNode();
    }
  };
}
