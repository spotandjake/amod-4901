using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

// TODO: Consider if there is a cleaner way to structure this file.
public record ParseTree {
  public enum NodeKind {
    ProgramNode,
    ClassDeclNode,
    MethodDeclNode,
    BlockNode,
    VariableDeclarationNode,
    TypeNode,
    // General
    VarBindNode,
    ParameterNode,
    LocationNode,
    // Statements
    AssignmentNode,
    ExpressionStatementNode,
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
  public struct Position {
#nullable enable
    public required string? fileName;
    public required int line;
    public required int column;
    public required int offset;
    public static Position FromContext(Antlr4.Runtime.ParserRuleContext context) {
      return new Position {
        fileName = context.Start.InputStream.SourceName,
        line = context.Start.Line,
        column = context.Start.Column,
        offset = context.Start.StartIndex
      };
    }
  }
  public abstract record Node {
    public abstract NodeKind Kind { get; }
    public Position Position { get; }
    protected Node(Position position) { this.Position = position; }
  };
  public record ProgramNode : Node {
    public override NodeKind Kind => NodeKind.ProgramNode;
    public ClassNode[] Classes { get; }
    public ProgramNode(Position position, ClassNode[] classes) : base(position) {
      this.Classes = classes;
    }
    public static ProgramNode FromContext(DecafParser.ProgramContext context) {
      var position = Position.FromContext(context);
      var classes = context.class_decl().Select(
        classCtx => ClassNode.FromContext(classCtx)
      ).ToArray();
      return new ProgramNode(position, classes);
    }
  };
  public record ClassNode : Node {
    public override NodeKind Kind => NodeKind.ClassDeclNode;
    public string Name { get; }
#nullable enable
    public string? SuperClassName { get; }
    public VariableDeclarationNode[] VariableDeclarations { get; }
    public MethodDeclarationNode[] MethodDeclarations { get; }
    public ClassNode(Position position, string name, string? superClassName, VariableDeclarationNode[] varDecls, MethodDeclarationNode[] methodDecls) : base(position) {
      this.Name = name;
      this.SuperClassName = superClassName;
      this.VariableDeclarations = varDecls;
      this.MethodDeclarations = methodDecls;
    }
    public static ClassNode FromContext(DecafParser.Class_declContext context) {
      var position = Position.FromContext(context);
      var name = context.name.Text;
      var superClassName = context.superClassName?.Text;
      var varDecls = context.var_decl().Select(
        varDeclCtx => VariableDeclarationNode.FromContext(varDeclCtx)
      ).ToArray();
      var methodDecls = context.method_decl().Select(
        methodDeclCtx => MethodDeclarationNode.FromContext(methodDeclCtx)
      ).ToArray();
      return new ClassNode(position, name, superClassName, varDecls, methodDecls);
    }
  }
  public record VariableDeclarationNode : Node {
    public record VarBindNode : Node {
      public override NodeKind Kind => NodeKind.VarBindNode;
      public string Name { get; }
      public bool IsArray { get; }
      public VarBindNode(Position position, string name, bool isArray) : base(position) {
        this.Name = name;
        this.IsArray = isArray;
      }
      public static VarBindNode FromContext(DecafParser.Var_bindContext context) {
        var position = Position.FromContext(context);
        var name = context.name.Text;
        var isArray = context.LBRACK() != null && context.RBRACK() != null;
        return new VarBindNode(position, name, isArray);
      }
    }
    public override NodeKind Kind => NodeKind.VariableDeclarationNode;
    public TypeNode VarType { get; }
    public VarBindNode[] VarBinds { get; }
    public VariableDeclarationNode(Position position, TypeNode varType, VarBindNode[] varBinds) : base(position) {
      this.VarType = varType;
      this.VarBinds = varBinds;
    }
    public static VariableDeclarationNode FromContext(DecafParser.Var_declContext context) {
      var position = Position.FromContext(context);
      var varType = TypeNode.FromContext(context.typ);
      var varBinds = context.binds.var_bind().Select(
        varBindCtx => VarBindNode.FromContext(varBindCtx)
      ).ToArray();
      return new VariableDeclarationNode(position, varType, varBinds);
    }
  }
  public record MethodDeclarationNode : Node {
    public record ParameterNode : Node {
      public override NodeKind Kind => NodeKind.ParameterNode;
      public TypeNode ParamType { get; }
      public string Name { get; }
      public bool IsArray { get; }
      public ParameterNode(Position position, TypeNode paramType, string name, bool isArray) : base(position) {
        this.ParamType = paramType;
        this.Name = name;
        this.IsArray = isArray;
      }
      public static ParameterNode FromContext(DecafParser.Method_decl_paramContext context) {
        var position = Position.FromContext(context);
        var paramType = TypeNode.FromContext(context.typ);
        var name = context.name.Text;
        var isArray = context.LBRACK() != null && context.RBRACK() != null;
        return new ParameterNode(position, paramType, name, isArray);
      }
    }
    public override NodeKind Kind => NodeKind.MethodDeclNode;
    public TypeNode ReturnType { get; }
    public string Name { get; }
    public ParameterNode[] Parameters { get; }
    public BlockNode Body { get; }
    public MethodDeclarationNode(Position position, TypeNode returnType, string name, ParameterNode[] parameters, BlockNode body) : base(position) {
      this.ReturnType = returnType;
      this.Name = name;
      this.Parameters = parameters;
      this.Body = body;
    }
    public static MethodDeclarationNode FromContext(DecafParser.Method_declContext context) {
      var position = Position.FromContext(context);
      var returnType = TypeNode.FromContext(context.returnType);
      var name = context.name.Text;
      var parameters = context.parameters != null ? context.parameters.method_decl_param().Select(
        paramCtx => ParameterNode.FromContext(paramCtx)
      ).ToArray() : [];
      var body = BlockNode.FromContext(context.body);
      return new MethodDeclarationNode(position, returnType, name, parameters, body);
    }
  }
  public record BlockNode : Node {
    public override NodeKind Kind => NodeKind.BlockNode;
    public VariableDeclarationNode[] VariableDeclarations { get; }
    public StatementNode[] Statements { get; }
    public BlockNode(Position position, VariableDeclarationNode[] varDecls, StatementNode[] statements) : base(position) {
      this.VariableDeclarations = varDecls;
      this.Statements = statements;
    }
    public static BlockNode FromContext(DecafParser.BlockContext context) {
      var position = Position.FromContext(context);
      var varDecls = context.var_decl().Select(
        varDeclCtx => VariableDeclarationNode.FromContext(varDeclCtx)
      ).ToArray();
      var statements = context.statement().Select(
        stmtCtx => StatementNode.FromContext(stmtCtx)
      ).ToArray();
      return new BlockNode(position, varDecls, statements);
    }
  }
  public record TypeNode : Node {
    public override NodeKind Kind => NodeKind.TypeNode;
    public string Type { get; }
    public TypeNode(Position position, string type) : base(position) { this.Type = type; }
    public static TypeNode FromContext(DecafParser.TypeContext context) {
      var position = Position.FromContext(context);
      var type = context.GetText();
      return new TypeNode(position, type);
    }
  }
  [JsonDerivedType(typeof(AssignmentNode), "AssignmentStatement")]
  [JsonDerivedType(typeof(ExpressionStatementNode), "ExpressionStatement")] // New generic JSON type
  [JsonDerivedType(typeof(IfNode), "IfStatement")]
  [JsonDerivedType(typeof(WhileNode), "WhileStatement")]
  [JsonDerivedType(typeof(ReturnNode), "ReturnStatement")]
  public abstract record StatementNode : Node {
    protected StatementNode(Position position) : base(position) { }
    public static StatementNode FromContext(DecafParser.StatementContext context) {
      switch (context) {
        case DecafParser.AssignStatementContext assignCtx:
          return AssignmentNode.FromContext(assignCtx.assign_stmt());
        case DecafParser.ExpressionStatementContext exprStmtCtx:
          return ExpressionStatementNode.FromContext(exprStmtCtx.expression_stmt());

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
  public record AssignmentNode : StatementNode {
    public override NodeKind Kind => NodeKind.AssignmentNode;
    public LocationNode Location { get; }
    public ExpressionNode Expression { get; }
    public AssignmentNode(Position position, LocationNode location, ExpressionNode expression) : base(position) {
      this.Location = location;
      this.Expression = expression;
    }
    public static AssignmentNode FromContext(DecafParser.Assign_stmtContext context) {
      var position = Position.FromContext(context);
      var location = LocationNode.FromContext(context.location());
      var expression = ExpressionNode.FromContext(context.expr());
      return new AssignmentNode(position, location, expression);
    }
  };

  public record ExpressionStatementNode : StatementNode {
    public override NodeKind Kind => NodeKind.ExpressionStatementNode;
    public ExpressionNode Content { get; }
    public ExpressionStatementNode(Position position, ExpressionNode content) : base(position) {
      this.Content = content;
    }

    public static ExpressionStatementNode FromContext(DecafParser.Expression_stmtContext context) { // I probably messed up the type here
      var position = Position.FromContext(context);
      var callExpr = context.call_stmt().call_expr();

      ExpressionNode content;
      if (callExpr is DecafParser.MethodCallExprContext m) {
        content = CallNode.FromContext(m.method_call());
      }
      else if (callExpr is DecafParser.PrimCalloutExprContext p) {
        content = PrimitiveCallNode.FromContext(p.prim_callout());
      }
      else {
        throw new InvalidProgramException("Unknown call expression type");
      }
      return new ExpressionStatementNode(position, content);
    }
  }

  public record CallNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.CallNode;
    public LocationNode MethodPath { get; }
    public ExpressionNode[] Arguments { get; }
    public CallNode(Position position, LocationNode methodPath, ExpressionNode[] args) : base(position) {
      this.MethodPath = methodPath;
      this.Arguments = args;
    }
    public static CallNode FromContext(DecafParser.Method_callContext context) {
      var position = Position.FromContext(context);
      var methodPath = LocationNode.FromContext(context.methodPath);
      var args = context.args.expr().Select(
        exprCtx => ExpressionNode.FromContext(exprCtx)
      ).ToArray();
      return new CallNode(position, methodPath, args);
    }
  };
  public record PrimitiveCallNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.PrimitiveCallNode;
    public string PrimitiveId { get; }
    public ExpressionNode[] Arguments { get; }
    public PrimitiveCallNode(Position position, string primId, ExpressionNode[] args) : base(position) {
      this.PrimitiveId = primId;
      this.Arguments = args;
    }
    public static PrimitiveCallNode FromContext(DecafParser.Prim_calloutContext context) {
      var position = Position.FromContext(context);
      var primId = context.primId.Text;
      // TODO: Handle mixed expression and stringlit args
      // var args = context.args.expr().Select(
      //   exprCtx => ExpressionNode.FromContext(exprCtx)
      // ).ToArray();
      return new PrimitiveCallNode(position, primId, []);
    }
  };

  public record IfNode : StatementNode {
    public override NodeKind Kind => NodeKind.IfNode;
    public ExpressionNode Condition { get; }
    public BlockNode TrueBranch { get; }
#nullable enable
    public BlockNode? FalseBranch { get; }
    public IfNode(Position position, ExpressionNode condition, BlockNode trueBranch, BlockNode? falseBranch) : base(position) {
      this.Condition = condition;
      this.TrueBranch = trueBranch;
      this.FalseBranch = falseBranch;
    }
    public static IfNode FromContext(DecafParser.If_stmtContext context) {
      var position = Position.FromContext(context);
      var condition = ExpressionNode.FromContext(context.condition);
      var trueBranch = BlockNode.FromContext(context.trueBranch);
      var falseBranch = context.falseBranch != null ? BlockNode.FromContext(context.falseBranch) : null;
      return new IfNode(position, condition, trueBranch, falseBranch);
    }
  };
  public record WhileNode : StatementNode {
    public override NodeKind Kind => NodeKind.WhileNode;
    public ExpressionNode Condition { get; }
    public BlockNode Body { get; }
    public WhileNode(Position position, ExpressionNode condition, BlockNode body) : base(position) {
      this.Condition = condition;
      this.Body = body;
    }
    public static WhileNode FromContext(DecafParser.While_stmtContext context) {
      var position = Position.FromContext(context);
      var condition = ExpressionNode.FromContext(context.condition);
      var body = BlockNode.FromContext(context.body);
      return new WhileNode(position, condition, body);
    }
  };
  public record ReturnNode : StatementNode {
    public override NodeKind Kind => NodeKind.ReturnNode;
    public ExpressionNode? Value { get; }
    public ReturnNode(Position position, ExpressionNode? value) : base(position) { this.Value = value; }
    public static ReturnNode FromContext(DecafParser.Return_stmtContext context) {
      var position = Position.FromContext(context);
      var value = context.value != null ? ExpressionNode.FromContext(context.value) : null;
      return new ReturnNode(position, value);
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
  public abstract record ExpressionNode : Node {
    protected ExpressionNode(Position position) : base(position) { }
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
  public record SimpleExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.SimpleExpressionNode;
    public SimpleExpressionNode(Position position) : base(position) { }
    public static SimpleExpressionNode FromContext(DecafParser.Simple_exprContext context) {
      var position = Position.FromContext(context);
      return new SimpleExpressionNode(position);
    }
  };
  public record BinopExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.BinopExpressionNode;
    public ExpressionNode Lhs { get; }
    public string Operator { get; }
    public ExpressionNode Rhs { get; }
    public BinopExpressionNode(Position position, ExpressionNode lhs, string op, ExpressionNode rhs) : base(position) {
      this.Lhs = lhs;
      this.Operator = op;
      this.Rhs = rhs;
    }
    public static BinopExpressionNode FromContext(DecafParser.BinaryOpExprContext context) {
      var position = Position.FromContext(context);
      var lhs = ExpressionNode.FromContext(context.lhs);
      var op = context.op.GetText();
      var rhs = ExpressionNode.FromContext(context.rhs);
      return new BinopExpressionNode(position, lhs, op, rhs);
    }
  };
  public record PrefixExpressionNode : ExpressionNode {
    public override NodeKind Kind => NodeKind.PrefixExpressionNode;
    public string Operator { get; }
    public ExpressionNode Operand { get; }
    public PrefixExpressionNode(Position position, string op, ExpressionNode operand) : base(position) {
      this.Operator = op;
      this.Operand = operand;
    }
    public static PrefixExpressionNode FromContext(DecafParser.NotExprContext context) {
      var position = Position.FromContext(context);
      var op = context.op.Text;
      var operand = ExpressionNode.FromContext(context.operand);
      return new PrefixExpressionNode(position, op, operand);
    }
  };
  public record LocationNode : Node {
    public override NodeKind Kind => NodeKind.LocationNode;
    public string Root { get; }
    // NOTE: Parsing restricts so we can't have nested arrays.
    public string? Path { get; }
    public ExpressionNode? IndexExpr { get; }
    public LocationNode(Position position, string root, string? path, ExpressionNode? indexExpr) : base(position) {
      this.Root = root;
      this.Path = path;
      this.IndexExpr = indexExpr;
    }
    public static LocationNode FromContext(DecafParser.LocationContext context) {
      var position = Position.FromContext(context);
      var root = context.root.Text;
      var path = context.path?.ID().GetText();
      var indexExpr = context.indexExpr != null ? ExpressionNode.FromContext(context.indexExpr.expr()) : null;
      return new LocationNode(position, root, path, indexExpr);
    }
  };
  [JsonDerivedType(typeof(IntegerLiteralNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(CharLiteralNode), "CharLiteral")]
  [JsonDerivedType(typeof(BoolLiteralNode), "BoolLiteral")]
  [JsonDerivedType(typeof(NullLiteralNode), "NullLiteral")]
  public abstract record LiteralNode : ExpressionNode {
    protected LiteralNode(Position position) : base(position) { }
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
  public record IntegerLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.IntegerLiteralNode;
    public int Value { get; }
    public IntegerLiteralNode(Position position, int value) : base(position) {
      this.Value = value;
    }
    public static IntegerLiteralNode FromContext(DecafParser.IntLitContext context) {
      var position = Position.FromContext(context);
      // TODO: Handle Proper Integer Conversion
      var value = int.Parse(context.INTLIT().GetText());
      return new IntegerLiteralNode(position, value);
    }
  };
  public record CharLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.CharLiteralNode;
    public char Value { get; }
    public CharLiteralNode(Position position, char value) : base(position) {
      this.Value = value;
    }
    public static CharLiteralNode FromContext(DecafParser.CharLitContext context) {
      var position = Position.FromContext(context);
      // TODO: Handle Proper Char Parsing
      var value = context.CHARLIT().GetText()[1]; // Skip the quotes
      return new CharLiteralNode(position, value);
    }
  };
  public record BoolLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.BoolLiteralNode;
    public bool Value { get; }
    public BoolLiteralNode(Position position, bool value) : base(position) {
      this.Value = value;
    }
    public static BoolLiteralNode FromContext(DecafParser.Bool_literalContext context) {
      var position = Position.FromContext(context);
      var value = context.TRUE() != null;
      return new BoolLiteralNode(position, value);
    }
  };
  public record NullLiteralNode : LiteralNode {
    public override NodeKind Kind => NodeKind.NullLiteralNode;
    public NullLiteralNode(Position position) : base(position) { }
    public static NullLiteralNode FromContext(DecafParser.NullLitContext context) {
      var position = Position.FromContext(context);
      return new NullLiteralNode(position);
    }
  };
}
