using Decaf.IR.Operators;
using Decaf.Utils;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Decaf.IR.ParseTree {
  /// <summary>
  /// An enumeration of every kind of parse tree node we have in our IR.
  /// This mainly exists so our snapshots have a more readable name for each node kind, and each node has a unique kind 
  /// that can be used to identify it, even after converting to JSON.
  /// </summary>
  public enum NodeKind {
    // --- Code Units ---
    ProgramNode,
    ModuleNode,
    // --- Statements ---
    BlockStatementNode,
    VariableDeclNode,
    AssignmentStatementNode,
    IfStatementNode,
    WhileStatementNode,
    ReturnStatementNode,
    ContinueStatementNode,
    BreakStatementNode,
    ExprStatementNode,
    // --- Expressions ---
    PrefixExpressionNode,
    BinopExpressionNode,
    CallExpressionNode,
    LocationExpressionNode,
    ArrayInitExpressionNode,
    LiteralExpressionNode,
    // --- Literals ---
    IntegerLiteralNode,
    BooleanLiteralNode,
    CharacterLiteralNode,
    StringLiteralNode,
    FunctionLiteralNode,
    // --- Types ---
    TypeNode,
    // --- Locations ---
    ArrayLocationNode,
    MemberLocationNode,
    IdentifierLocationNode,
    PrimitiveLocationNode,
    // --- Other ---
    ParameterNode,
    BindNode
  }
  /// <summary>
  /// A base parse tree node that all other parse tree nodes inherit from.
  /// </summary>
  public abstract record Node {
    /// <summary>The kind of the node.</summary>
    public abstract NodeKind Kind { get; }
    /// <summary>The source position of the node.</summary>
    public Position Position { get; }
    /// <summary>
    /// The constructor for the base node, it takes in a position which is used for error reporting and debugging later on.
    /// </summary>
    /// <param name="position">The source position of the node.</param>
    protected Node(Position position) { this.Position = position; }
  };

  // --- Code Units ---
  #region CodeUnits
  /// <summary>The root node of a parse tree, it represents an entire program.</summary>
  public sealed record ProgramNode(Position Position, ModuleNode[] Modules) : Node(Position) {
    public override NodeKind Kind => NodeKind.ProgramNode;
    public ModuleNode[] Modules { get; } = Modules;
  };
  /// <summary>A module declaration, `module <name:id> <body:block_stmt>`.</summary>
  public sealed record ModuleNode(
    Position Position, LocationNode.IdentifierNode Name, StatementNode.BlockNode Body
  ) : Node(Position) {
    public override NodeKind Kind => NodeKind.ModuleNode;
    public LocationNode.IdentifierNode Name { get; } = Name;
    public StatementNode.BlockNode Body { get; } = Body;
  }
  #endregion

  // --- Statements ---
  #region Statements
  /// <summary>The supertype for all statements.</summary>
  [JsonDerivedType(typeof(StatementNode.BlockNode), "BlockStatementNode")]
  [JsonDerivedType(typeof(StatementNode.VariableDeclNode), "VariableDeclNode")]
  [JsonDerivedType(typeof(StatementNode.AssignmentNode), "AssignmentStatementNode")]
  [JsonDerivedType(typeof(StatementNode.IfNode), "IfStatementNode")]
  [JsonDerivedType(typeof(StatementNode.WhileNode), "WhileStatementNode")]
  [JsonDerivedType(typeof(StatementNode.ReturnNode), "ReturnStatementNode")]
  [JsonDerivedType(typeof(StatementNode.ContinueNode), "ContinueStatementNode")]
  [JsonDerivedType(typeof(StatementNode.BreakNode), "BreakStatementNode")]
  [JsonDerivedType(typeof(StatementNode.ExprStatementNode), "ExprStatementNode")]
  public abstract record StatementNode : Node {
    protected StatementNode(Position position) : base(position) { }

    /// <summary>A block statement, `{ <statement>* }`.</summary>
    public sealed record BlockNode(Position Position, StatementNode[] Statements) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.BlockStatementNode;
      public StatementNode[] Statements { get; } = Statements;
    }

    // Variables

    /// <summary>
    /// A variable declaration statement, `let <binds>` where `<binds>` is a comma separated list of:
    /// `<name:id>: <type> = <expr>`, for example `let x: int = 1, y: int[] = 2;`.
    /// </summary>
    public sealed record VariableDeclNode : StatementNode {
      public override NodeKind Kind => NodeKind.VariableDeclNode;
      // A single declaration bind.
#nullable enable
      public sealed record BindNode(
        Position Position, TypeNode? Type, LocationNode.IdentifierNode Name, ExpressionNode InitExpr
      ) : Node(Position) {
        public override NodeKind Kind => NodeKind.BindNode;
        public TypeNode? Type { get; } = Type;
        public LocationNode.IdentifierNode Name { get; } = Name;
        public ExpressionNode InitExpr { get; } = InitExpr;
      }
#nullable restore
      public VariableDeclNode(Position Position, IEnumerable<BindNode> Binds) : base(Position) {
        this.Binds = Binds;
      }
      public IEnumerable<BindNode> Binds { get; }
    };
    /// <summary>An assignment statement, `<location> = <expr>;`.</summary>
    public sealed record AssignmentNode(Position Position, LocationNode Location, ExpressionNode Expression) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.AssignmentStatementNode;
      public LocationNode Location { get; } = Location;
      public ExpressionNode Expression { get; } = Expression;
    };

    // Control Flow

    /// <summary>An if statement, `if (<condition>) <trueBranch> else <falseBranch>`.</summary>
    public sealed record IfNode(
#nullable enable
      Position Position, ExpressionNode Condition, StatementNode TrueBranch, StatementNode? FalseBranch
#nullable restore
    ) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.IfStatementNode;
      public ExpressionNode Condition { get; } = Condition;
      public StatementNode TrueBranch { get; } = TrueBranch;
#nullable enable
      public StatementNode? FalseBranch { get; } = FalseBranch;
#nullable restore
    };
    /// <summary>A while loop statement, `while (<condition>) <body:block_stmt>`.</summary>
    public sealed record WhileNode(Position Position, ExpressionNode Condition, StatementNode Body) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.WhileStatementNode;
      public ExpressionNode Condition { get; } = Condition;
      public StatementNode Body { get; } = Body;
    };

    // Other

    /// <summary>A return statement, `return <value:expr>`.</summary>
#nullable enable
    public sealed record ReturnNode(Position Position, ExpressionNode? Value) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.ReturnStatementNode;
      public ExpressionNode? Value { get; } = Value;
    };
#nullable restore
    /// <summary>A continue statement, `continue`.</summary>
    public sealed record ContinueNode(Position Position) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.ContinueStatementNode;
    };
    /// <summary>A break statement, `break`.</summary>
    public sealed record BreakNode(Position Position) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.BreakStatementNode;
    };
    /// <summary>An expression statement, `<expr>;`.</summary>
    public sealed record ExprStatementNode(Position Position, ExpressionNode Expression) : StatementNode(Position) {
      public override NodeKind Kind => NodeKind.ExprStatementNode;
      public ExpressionNode Expression { get; } = Expression;
    };
  }
  #endregion

  // --- Expressions ---
  #region Expressions
  /// <summary>The supertype for all expression nodes.</summary>
  [JsonDerivedType(typeof(ExpressionNode.PrefixNode), "PrefixExpression")]
  [JsonDerivedType(typeof(ExpressionNode.BinopNode), "BinopExpression")]
  [JsonDerivedType(typeof(ExpressionNode.CallNode), "CallExpression")]
  [JsonDerivedType(typeof(ExpressionNode.ArrayInitNode), "ArrayInitExpression")]
  [JsonDerivedType(typeof(ExpressionNode.LocationExprNode), "LocationExpression")]
  [JsonDerivedType(typeof(ExpressionNode.LiteralExprNode), "LiteralExpression")]
  public abstract record ExpressionNode : Node {
    protected ExpressionNode(Position position) : base(position) { }

    /// <summary>A prefix expression, `<operator> <operand>`, for example `!x` or `~y`.</summary>
    public sealed record PrefixNode(Position Position, PrefixOperator Operator, ExpressionNode Operand) : ExpressionNode(Position) {
      public override NodeKind Kind => NodeKind.PrefixExpressionNode;
      public PrefixOperator Operator { get; } = Operator;
      public ExpressionNode Operand { get; } = Operand;
    };
    /// <summary>A binary operation expression, `<lhs> <operator> <rhs>`, for example `x + y` or `a && b`.</summary>
    public sealed record BinopNode(
      Position Position, ExpressionNode Lhs, BinaryOperator Operator, ExpressionNode Rhs
    ) : ExpressionNode(Position) {
      public override NodeKind Kind => NodeKind.BinopExpressionNode;
      public ExpressionNode Lhs { get; } = Lhs;
      public BinaryOperator Operator { get; } = Operator;
      public ExpressionNode Rhs { get; } = Rhs;
    };
    /// <summary>A call expression, `<callee>(<args>)`.</summary>
    public sealed record CallNode(Position Position, LocationNode Callee, ExpressionNode[] Arguments) : ExpressionNode(Position) {
      public override NodeKind Kind => NodeKind.CallExpressionNode;
      public LocationNode Callee { get; } = Callee;
      public ExpressionNode[] Arguments { get; } = Arguments;
    }
    /// <summary>An array initialization expression, `new <type>[<size>]`.</summary>
    public sealed record ArrayInitNode(Position Position, TypeNode Type, ExpressionNode SizeExpr) : ExpressionNode(Position) {
      public override NodeKind Kind => NodeKind.ArrayInitExpressionNode;
      public TypeNode Type { get; } = Type;
      public ExpressionNode SizeExpr { get; } = SizeExpr;
    }
    /// <summary>A location expression, `<location>`, for example `x`, `x.y` or `x[y]`.</summary>
    public sealed record LocationExprNode(Position Position, LocationNode Location) : ExpressionNode(Position) {
      public override NodeKind Kind => NodeKind.LocationExpressionNode;
      public LocationNode Location { get; } = Location;
    }
    /// <summary>A literal expression, `<literal>`, for example `1`, `'a'` or `"hello"`.</summary>
    public sealed record LiteralExprNode(Position Position, LiteralNode Literal) : ExpressionNode(Position) {
      public override NodeKind Kind => NodeKind.LiteralExpressionNode;
      public LiteralNode Literal { get; } = Literal;
    }
  }
  #endregion

  // --- Literals ---
  #region Literals
  /// <summary>The supertype for all literal nodes.</summary>
  [JsonDerivedType(typeof(LiteralNode.IntegerNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(LiteralNode.BooleanNode), "BooleanLiteral")]
  [JsonDerivedType(typeof(LiteralNode.CharacterNode), "CharacterLiteral")]
  [JsonDerivedType(typeof(LiteralNode.StringNode), "StringLiteral")]
  [JsonDerivedType(typeof(LiteralNode.FunctionNode), "FunctionLiteral")]
  public abstract record LiteralNode : Node {
    protected LiteralNode(Position position) : base(position) { }

    /// <summary>An integer literal, for example `1`, `42` or `-5`.</summary>
    public sealed record IntegerNode(Position Position, int Value) : LiteralNode(Position) {
      public override NodeKind Kind => NodeKind.IntegerLiteralNode;
      public int Value { get; } = Value;
    };
    /// <summary>A boolean literal, either `true` or `false`.</summary>
    public sealed record BooleanNode(Position Position, bool Value) : LiteralNode(Position) {
      public override NodeKind Kind => NodeKind.BooleanLiteralNode;
      public bool Value { get; } = Value;
    };
    /// <summary>A character literal, for example `'a'`, `'\n'` or `'\''`.</summary>
    public sealed record CharacterNode(Position Position, char Value) : LiteralNode(Position) {
      public override NodeKind Kind => NodeKind.CharacterLiteralNode;
      public char Value { get; } = Value;
    };
    /// <summary>A string literal, for example `"hello"`, `"world"` or `"decaf"`.</summary>
    public sealed record StringNode(Position Position, string Value) : LiteralNode(Position) {
      public override NodeKind Kind => NodeKind.StringLiteralNode;
      public string Value { get; } = Value;
    };
    /// <summary>
    /// A function literal, `(): <return_type> { <body> }` or `(x: int, y: string): <return_type> { <body> }`.
    /// The name is taken from the binding as that is the only place this literal is allowed to occur.
    /// </summary>
    public sealed record FunctionNode : LiteralNode {
      // A parameter node, `<name:id>: <type>`, for example `x: int` or `y: string[]`.
      public sealed record ParameterNode(
        Position Position,
        TypeNode Type,
        LocationNode.IdentifierNode Name
      ) : Node(Position) {
        public override NodeKind Kind => NodeKind.ParameterNode;
        public TypeNode Type { get; } = Type;
        public LocationNode.IdentifierNode Name { get; } = Name;
      }
      public override NodeKind Kind => NodeKind.FunctionLiteralNode;
      public LocationNode.IdentifierNode Name { get; }
      public TypeNode ReturnType { get; }
      public ParameterNode[] Parameters { get; }
      public StatementNode.BlockNode Body { get; }
      public FunctionNode(
        Position Position,
        LocationNode.IdentifierNode Name,
        TypeNode ReturnType,
        ParameterNode[] Parameters,
        StatementNode.BlockNode Body
      ) : base(Position) {
        this.Name = Name;
        this.ReturnType = ReturnType;
        this.Parameters = Parameters;
        this.Body = Body;
      }
    }
  }
  #endregion

  // --- Types ---
  #region Types
  /// <summary>
  /// A type node, which represents a type in our parse tree. 
  /// This can be a primitive type like `int` or `boolean`, or it can be an array type like `int[]` or `string[]`.
  /// </summary>
  public abstract record TypeNode : Node {
    protected TypeNode(Position position) : base(position) { }
#nullable enable
    public sealed record ArrayNode(Position Position, TypeNode ElementType, int? Size) : TypeNode(Position) {
      public override NodeKind Kind => NodeKind.TypeNode;
      public TypeNode ElementType { get; } = ElementType;
      public int? Size { get; } = Size;
    }
#nullable restore
    public sealed record SimpleNode(Position Position, Signature.PrimitiveType Type) : TypeNode(Position) {
      public override NodeKind Kind => NodeKind.TypeNode;
      public Signature.PrimitiveType Type { get; } = Type;
    }
  }
  #endregion

  // --- Locations ---
  #region Locations
  /// <summary>The supertype for all location nodes.</summary>
  [JsonDerivedType(typeof(LocationNode.ArrayNode), "ArrayLocationNode")]
  [JsonDerivedType(typeof(LocationNode.MemberNode), "MemberLocationNode")]
  [JsonDerivedType(typeof(LocationNode.IdentifierNode), "IdentifierLocationNode")]
  public abstract record LocationNode : Node {
    public abstract bool IsPrimitive { get; }
    protected LocationNode(Position position) : base(position) { }
    /// <summary>An array location, `<root>[<index>]`.</summary>
    public sealed record ArrayNode(Position Position, LocationNode Root, ExpressionNode IndexExpr) : LocationNode(Position) {
      public override NodeKind Kind => NodeKind.ArrayLocationNode;
      public override bool IsPrimitive => Root.IsPrimitive;
      public LocationNode Root { get; } = Root;
      public ExpressionNode IndexExpr { get; } = IndexExpr;
      public override string ToString() => $"{Root}[<indexExpr>]";
    };
    /// <summary>A member location, `<root>.<member>`.</summary>
    public sealed record MemberNode(Position Position, LocationNode Root, string Member) : LocationNode(Position) {
      public override NodeKind Kind => NodeKind.MemberLocationNode;
      public override bool IsPrimitive => Root.IsPrimitive;
      public LocationNode Root { get; } = Root;
      public string Member { get; } = Member;
      public override string ToString() => $"{Root}.{Member}";
    };
    /// <summary>An identifier location, `<name>`.</summary>
    public sealed record IdentifierNode(Position Position, string Name) : LocationNode(Position) {
      public override NodeKind Kind => NodeKind.IdentifierLocationNode;
      public override bool IsPrimitive => this.Name.StartsWith("@");
      public string Name { get; } = Name;
      public override string ToString() => Name;
    };
  }
  #endregion
}
