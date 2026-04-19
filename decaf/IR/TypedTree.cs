using System.Collections.Generic;
using System.Text.Json.Serialization;

using Decaf.IR.Operators;
using Decaf.Utils;

namespace Decaf.IR.TypedTree {
  /// <summary>A base typed tree node that all other parse tree nodes inherit from.</summary>
  public abstract record Node {
    /// <summary>The kind of the node.</summary>
    public abstract ParseTree.NodeKind Kind { get; }
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
  /// <summary>The root node of a typed tree, it represents an entire program.</summary>
  public sealed record ProgramNode(Position Position, ModuleNode[] Modules, Scope<Signature.Signature> Scope) : Node(Position) {
    public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ProgramNode;
    public ModuleNode[] Modules { get; } = Modules;
    public Scope<Signature.Signature> Scope { get; } = Scope;
  };
  /// <summary>A module declaration.</summary>
  public sealed record ModuleNode(
    Position Position,
    string Name,
    ImportNode[] Imports,
    StatementNode.BlockNode Body,
    Scope<Signature.Signature> Scope,
    Signature.Signature.ModuleSig Signature
  ) : Node(Position) {
    public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ModuleNode;
    public string Name { get; } = Name;
    public ImportNode[] Imports { get; } = Imports;
    public StatementNode.BlockNode Body { get; } = Body;
    public Scope<Signature.Signature> Scope { get; } = Scope;
    public Signature.Signature.ModuleSig Signature { get; } = Signature;
  }
  /// <summary>An import statement, `import wasm <name:id>: <type> from "<module:string>"`.</summary>
  public sealed record ImportNode(
    Position Position, string Name, Signature.Signature Signature, string Module
  ) : Node(Position) {
    public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ImportNode;
    public string Name { get; } = Name;
    public Signature.Signature Signature { get; } = Signature;
    public string Module { get; } = Module;
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

    /// <summary>A block statement.</summary>
    public sealed record BlockNode(
      Position Position,
      StatementNode[] Statements,
      Scope<Signature.Signature> Scope
    ) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BlockStatementNode;
      public StatementNode[] Statements { get; } = Statements;
      public Scope<Signature.Signature> Scope { get; } = Scope;
    }

    // Variables

    /// <summary>A variable declaration statement.</summary>
    public sealed record VariableDeclNode : StatementNode {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.VariableDeclNode;
      // A single declaration bind.
      public sealed record BindNode(
        Position Position,
        string Name,
        ExpressionNode InitExpr,
        Signature.Signature Signature
      ) : Node(Position) {
        public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BindNode;
        public string Name { get; } = Name;
        public ExpressionNode InitExpr { get; } = InitExpr;
        public Signature.Signature Signature { get; } = Signature;
      }
      public VariableDeclNode(Position Position, IEnumerable<BindNode> Binds) : base(Position) {
        this.Binds = Binds;
      }
      public IEnumerable<BindNode> Binds { get; }
    };
    /// <summary>An assignment statement.</summary>
    public sealed record AssignmentNode(Position Position, LocationNode Location, ExpressionNode Expression) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.AssignmentStatementNode;
      public LocationNode Location { get; } = Location;
      public ExpressionNode Expression { get; } = Expression;
    };

    // Control Flow

    /// <summary>An if statement.</summary>
    public sealed record IfNode(
#nullable enable
      Position Position, ExpressionNode Condition, StatementNode TrueBranch, StatementNode? FalseBranch
#nullable restore
    ) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.IfStatementNode;
      public ExpressionNode Condition { get; } = Condition;
      public StatementNode TrueBranch { get; } = TrueBranch;
#nullable enable
      public StatementNode? FalseBranch { get; } = FalseBranch;
#nullable restore
    };
    /// <summary>A while loop statement.</summary>
    public sealed record WhileNode(Position Position, ExpressionNode Condition, StatementNode Body) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.WhileStatementNode;
      public ExpressionNode Condition { get; } = Condition;
      public StatementNode Body { get; } = Body;
    };

    // Other

    /// <summary>A return statement.</summary>
#nullable enable
    public sealed record ReturnNode(Position Position, ExpressionNode? Value) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ReturnStatementNode;
      public ExpressionNode? Value { get; } = Value;
    };
#nullable restore
    /// <summary>A continue statement.</summary>
    public sealed record ContinueNode(Position Position) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ContinueStatementNode;
    };
    /// <summary>A break statement.</summary>
    public sealed record BreakNode(Position Position) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BreakStatementNode;
    };
    /// <summary>An expression statement.</summary>
    public sealed record ExprStatementNode(Position Position, ExpressionNode Expression) : StatementNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ExprStatementNode;
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
  [JsonDerivedType(typeof(ExpressionNode.PrimCallNode), "PrimCallExpression")]
  [JsonDerivedType(typeof(ExpressionNode.ArrayInitNode), "ArrayInitExpression")]
  [JsonDerivedType(typeof(ExpressionNode.LocationExprNode), "LocationExpression")]
  [JsonDerivedType(typeof(ExpressionNode.LiteralExprNode), "LiteralExpression")]
  public abstract record ExpressionNode : Node {
    public Signature.Signature ExpressionType { get; }
    protected ExpressionNode(Position position, Signature.Signature expressionType) : base(position) {
      this.ExpressionType = expressionType;
    }

    /// <summary>A prefix expression.</summary>
    public sealed record PrefixNode(
      Position Position,
      PrefixOperator Operator,
      ExpressionNode Operand,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.PrefixExpressionNode;
      public PrefixOperator Operator { get; } = Operator;
      public ExpressionNode Operand { get; } = Operand;
    };
    /// <summary>A binary operation expression.</summary>
    public sealed record BinopNode(
      Position Position,
      ExpressionNode Lhs,
      BinaryOperator Operator,
      ExpressionNode Rhs,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BinopExpressionNode;
      public ExpressionNode Lhs { get; } = Lhs;
      public BinaryOperator Operator { get; } = Operator;
      public ExpressionNode Rhs { get; } = Rhs;
    };
    /// <summary>A call expression.</summary>
    public sealed record CallNode(
      Position Position,
      LocationNode Callee,
      ExpressionNode[] Arguments,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.CallExpressionNode;
      public LocationNode Callee { get; } = Callee;
      public ExpressionNode[] Arguments { get; } = Arguments;
    }
    /// <summary>A primitive call expression.</summary>
    public sealed record PrimCallNode(
      Position Position,
      PrimitiveDefinition.PrimDefinition Callee,
      ExpressionNode[] Arguments,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.CallExpressionNode;
      public PrimitiveDefinition.PrimDefinition Callee { get; } = Callee;
      public ExpressionNode[] Arguments { get; } = Arguments;
    }
    /// <summary>An array initialization expression.</summary>
    public sealed record ArrayInitNode(
      Position Position,
      ExpressionNode SizeExpr,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ArrayInitExpressionNode;
      public ExpressionNode SizeExpr { get; } = SizeExpr;
    }
    /// <summary>A location expression.</summary>
    public sealed record LocationExprNode(
      Position Position,
      LocationNode Location,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.LocationExpressionNode;
      public LocationNode Location { get; } = Location;
    }
    /// <summary>A literal expression.</summary>
    public sealed record LiteralExprNode(
      Position Position,
      LiteralNode Literal,
      Signature.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.LiteralExpressionNode;
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
    public Signature.Signature LiteralType { get; }
    protected LiteralNode(Position position, Signature.Signature literalType) : base(position) {
      this.LiteralType = literalType;
    }

    /// <summary>An integer literal.</summary>
    public sealed record IntegerNode(
      Position Position, int Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.IntegerLiteralNode;
      public int Value { get; } = Value;
    };
    /// <summary>A boolean literal.</summary>
    public sealed record BooleanNode(
      Position Position, bool Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BooleanLiteralNode;
      public bool Value { get; } = Value;
    };
    /// <summary>A character literal.</summary>
    public sealed record CharacterNode(
      Position Position, char Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.CharacterLiteralNode;
      public char Value { get; } = Value;
    };
    /// <summary>A string literal.</summary>
    public sealed record StringNode(
      Position Position, string Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.StringLiteralNode;
      public string Value { get; } = Value;
    };
    /// <summary>A function literal.</summary>
    public sealed record FunctionNode : LiteralNode {
      // A parameter node
      public sealed record ParameterNode(
        Position Position,
        string Name,
        Signature.Signature Signature
      ) : Node(Position) {
        public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ParameterNode;
        public string Name { get; } = Name;
        public Signature.Signature Signature { get; } = Signature;
      }
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.FunctionLiteralNode;
      public string Name { get; }
      public ParameterNode[] Parameters { get; }
      public StatementNode.BlockNode Body { get; }
      public Scope<Signature.Signature> Scope { get; }
      public FunctionNode(
        Position Position,
        string Name,
        ParameterNode[] Parameters,
        StatementNode.BlockNode Body,
        Scope<Signature.Signature> Scope,
        Signature.Signature LiteralType
      ) : base(Position, LiteralType) {
        this.Name = Name;
        this.Parameters = Parameters;
        this.Body = Body;
        this.Scope = Scope;
      }
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
    public Signature.Signature LocationType { get; }
    protected LocationNode(Position position, Signature.Signature locationType) : base(position) {
      this.LocationType = locationType;
    }
    /// <summary>An array location.</summary>
    public sealed record ArrayNode(
      Position Position,
      LocationNode Root,
      ExpressionNode IndexExpr,
      Signature.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ArrayLocationNode;
      public LocationNode Root { get; } = Root;
      public ExpressionNode IndexExpr { get; } = IndexExpr;
    };
    /// <summary>A member location.</summary>
    public sealed record MemberNode(
      Position Position,
      LocationNode Root,
      string Member,
      Signature.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.MemberLocationNode;
      public LocationNode Root { get; } = Root;
      public string Member { get; } = Member;
    };
    /// <summary>An identifier location.</summary>
    public sealed record IdentifierNode(
      Position Position,
      string Name,
      Signature.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.IdentifierLocationNode;
      public string Name { get; } = Name;
    };
  }
  #endregion
}
