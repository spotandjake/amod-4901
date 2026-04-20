using System.Text.Json.Serialization;

using Decaf.IR.Operators;
using Decaf.IR.PrimitiveDefinition;
using Decaf.Utils;

/// <summary>
/// This file contains the definition of the ANF tree, which is the intermediate representation used in
/// the ANF pass. The ANF tree is a simplified version of the typed tree, where all expressions are flattened.
/// What this means in practice is that an expression like:
/// ```
/// return a + b * c;
/// ```
/// would become:
/// ```
/// let t1 = b * c;
/// let t2 = a + t1;
/// return t2;
/// ```
/// The main motivation for using ANF is that it makes code generation easier, since we can
/// guarantee that all expressions are simple and do not contain any nested expressions. This makes it easier to
/// generate code for expressions, since we can just generate code for the simple expressions and then combine them together.
/// Another motivation for using ANF is that it makes optimization easier, since we can easily identify 
/// common subexpressions and eliminate them.
/// 
/// More information on ANF can be found here: https://en.wikipedia.org/wiki/A-normal_form
/// </summary>
namespace Decaf.IR.AnfTree {
  /// <summary>
  /// An enum representing the different kinds of node in the ANF tree.
  /// 
  /// The main purpose of this is so we can see the `NodeKind` in both JSON and snapshots.
  /// </summary>
  public enum NodeKind {
    // --- Code Units ---
    ProgramNode,
    ModuleNode,
    ImportNode,
    FunctionNode,
    // --- Instructions ---
    BlockInstructionNode,
    BindInstructionNode,
    AssignmentInstructionNode,
    IfInstructionNode,
    LoopInstructionNode,
    ReturnInstructionNode,
    ContinueInstructionNode,
    BreakInstructionNode,
    SimpleExprInstructionNode,
    // --- Expressions ---
    PrefixExpressionNode,
    BinopExpressionNode,
    CallExpressionNode,
    PrimCallExpressionNode,
    ArrayInitExpressionNode,
    ImmExpressionNode,
    // --- Immediates ---
    ConstantImmediate,
    LocationImmediate,
    // --- Literals ---
    IntegerLiteralNode,
    BooleanLiteralNode,
    CharacterLiteralNode,
    StringLiteralNode,
    FunctionReferenceLiteralNode,
    // --- Locations ---
    ArrayLocationNode,
    SymbolLocationNode,
    // --- Other ---
    ParameterNode
  }
  /// <summary>A base anf node that all other anf nodes inherit from.</summary>
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
  /// <summary>The root node of a typed tree, it represents an entire program.</summary>
  public sealed record ProgramNode(Position Position, ModuleNode[] Modules) : Node(Position) {
    public override NodeKind Kind => NodeKind.ProgramNode;
    public ModuleNode[] Modules { get; } = Modules;
  };
  /// <summary>A module declaration.</summary>
  public sealed record ModuleNode(
    Position Position,
    Symbol ID,
    ImportNode[] Imports,
    FunctionNode[] Functions,
    InstructionNode.BlockNode Body,
    Signature.Signature.ModuleSig Signature
  ) : Node(Position) {
    public override NodeKind Kind => NodeKind.ModuleNode;
    public Symbol ID { get; } = ID;
    public ImportNode[] Imports { get; } = Imports;
    public FunctionNode[] Functions { get; } = Functions;
    public InstructionNode.BlockNode Body { get; } = Body;
    public Signature.Signature.ModuleSig Signature { get; } = Signature;
  }
  /// <summary>An import declaration.</summary>
  public sealed record ImportNode(
    Position Position, Symbol ID, Signature.Signature Signature, string ExternalName, string ExternalModule
  ) : Node(Position) {
    public override NodeKind Kind => NodeKind.ImportNode;
    public Symbol ID { get; } = ID;
    public Signature.Signature Signature { get; } = Signature;
    public string ExternalName { get; } = ExternalName;
    public string ExternalModule { get; } = ExternalModule;
  }
  /// <summary>A function declaration.</summary>
  public sealed record FunctionNode : Node {
    // NOTE: Functions are hoisted to the top level of the module this is because they are static
    public sealed record ParameterNode(
      Position Position,
      Symbol ID,
      Signature.Signature Signature
    ) : Node(Position) {
      public override NodeKind Kind => NodeKind.ParameterNode;
      public Symbol ID { get; } = ID;
      public Signature.Signature Signature { get; } = Signature;
    }
    public override NodeKind Kind => NodeKind.FunctionNode;
    public Symbol ID { get; }
    public ParameterNode[] Parameters { get; }
    public InstructionNode.BlockNode Body { get; }
    public Signature.Signature.FunctionSig Signature { get; }
    public FunctionNode(
      Position Position,
      Symbol ID,
      ParameterNode[] Parameters,
      InstructionNode.BlockNode Body,
      Signature.Signature.FunctionSig Signature
    ) : base(Position) {
      this.ID = ID;
      this.Parameters = Parameters;
      this.Body = Body;
      this.Signature = Signature;
    }
  }
  #endregion
  // --- Instructions ---
  #region Instructions
  /// <summary>The supertype for all instructions.</summary>
  [JsonDerivedType(typeof(InstructionNode.BlockNode), "BlockInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.BindNode), "BindInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.AssignmentNode), "AssignmentInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.IfNode), "IfInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.LoopNode), "LoopInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.ReturnNode), "ReturnInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.ContinueNode), "ContinueInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.BreakNode), "BreakInstructionNode")]
  [JsonDerivedType(typeof(InstructionNode.SimpleExprInstructionNode), "SimpleExprInstructionNode")]
  public abstract record InstructionNode : Node {
    protected InstructionNode(Position position) : base(position) { }

    /// <summary>A block instruction.</summary>
    public sealed record BlockNode(
      Position Position,
      InstructionNode[] Instructions
    ) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.BlockInstructionNode;
      public InstructionNode[] Instructions { get; } = Instructions;
    }

    // Variables

    /// <summary>A bind instruction.</summary>
    public sealed record BindNode(Position Position, Symbol ID, SimpleExpressionNode SimpleExpression) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.BindInstructionNode;
      public Symbol ID { get; } = ID;
      public SimpleExpressionNode SimpleExpression { get; } = SimpleExpression;
    };

    /// <summary>An assignment instruction.</summary>
    public sealed record AssignmentNode(
      Position Position, LocationNode Location, ImmediateNode Imm
    ) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.AssignmentInstructionNode;
      public LocationNode Location { get; } = Location;
      public ImmediateNode Imm { get; } = Imm;
    };

    // Control Flow

    /// <summary>An if instruction.</summary>
    public sealed record IfNode(
#nullable enable
      Position Position, ImmediateNode Condition, InstructionNode TrueBranch, InstructionNode? FalseBranch
#nullable restore
    ) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.IfInstructionNode;
      public ImmediateNode Condition { get; } = Condition;
      public InstructionNode TrueBranch { get; } = TrueBranch;
#nullable enable
      public InstructionNode? FalseBranch { get; } = FalseBranch;
#nullable restore
    };
    /// <summary>
    /// A loop instruction.
    /// 
    /// NOTE: We lower to a generic loop instead of a while loop because it is easier to generate code
    ///       and means any optimization applied to loops can be applied to any loop.
    /// </summary>
    public sealed record LoopNode(Position Position, InstructionNode Body) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.LoopInstructionNode;
      public InstructionNode Body { get; } = Body;
    };

    // Other

    /// <summary>A return instruction.</summary>
#nullable enable
    public sealed record ReturnNode(Position Position, ImmediateNode? Value) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.ReturnInstructionNode;
      public ImmediateNode? Value { get; } = Value;
    };
#nullable restore
    /// <summary>A continue instruction.</summary>
    public sealed record ContinueNode(Position Position) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.ContinueInstructionNode;
    };
    /// <summary>A break instruction.</summary>
    public sealed record BreakNode(Position Position) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.BreakInstructionNode;
    };
    /// <summary>A simple expression instruction.</summary>
    public sealed record SimpleExprInstructionNode(Position Position, SimpleExpressionNode Expr) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.SimpleExprInstructionNode;
      public SimpleExpressionNode Expr { get; } = Expr;
    };
  }
  #endregion

  // --- Simple Expressions ---
  #region SimpleExpressionNode
  /// <summary>
  /// The supertype for all simple expression nodes, we use a supertype to ensure strict type checking.
  /// 
  /// In ANF an expression is very similar to what it is in the typedTree the main difference,
  /// is that they are no longer compound, instead they are what we consider to be a simple expression.
  /// Expressions can normally be nested for example `a + b + c` would be represented as `(a + b) + c` in the typed tree.
  /// In anf we have a restriction that the arguments to an expression must be immediates, i.e a variable reference or
  /// a constant / literal, so we would represent `a + b + c` as `t1 = a + b; t1 + c` where `t1` is a temporary generated
  /// variable by the anf pass.
  /// </summary>
  [JsonDerivedType(typeof(SimpleExpressionNode.PrefixNode), "PrefixSimpleExpression")]
  [JsonDerivedType(typeof(SimpleExpressionNode.BinopNode), "BinopSimpleExpression")]
  [JsonDerivedType(typeof(SimpleExpressionNode.CallNode), "CallSimpleExpression")]
  [JsonDerivedType(typeof(SimpleExpressionNode.PrimCallNode), "PrimCallExpression")]
  [JsonDerivedType(typeof(SimpleExpressionNode.ArrayInitNode), "ArrayInitSimpleExpression")]
  [JsonDerivedType(typeof(SimpleExpressionNode.ImmediateExpressionNode), "ImmediateSimpleExpression")]
  public abstract record SimpleExpressionNode : Node {
    public Signature.Signature ExpressionType { get; }
    protected SimpleExpressionNode(Position position, Signature.Signature expressionType) : base(position) {
      this.ExpressionType = expressionType;
    }

    /// <summary>A prefix expression.</summary>
    public sealed record PrefixNode(
      Position Position,
      PrefixOperator Operator,
      ImmediateNode Operand,
      Signature.Signature ExpressionType
    ) : SimpleExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.PrefixExpressionNode;
      public PrefixOperator Operator { get; } = Operator;
      public ImmediateNode Operand { get; } = Operand;
    };
    /// <summary>A binary operation expression.</summary>
    public sealed record BinopNode(
      Position Position,
      ImmediateNode Lhs,
      BinaryOperator Operator,
      ImmediateNode Rhs,
      Signature.Signature ExpressionType
    ) : SimpleExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.BinopExpressionNode;
      public ImmediateNode Lhs { get; } = Lhs;
      public BinaryOperator Operator { get; } = Operator;
      public ImmediateNode Rhs { get; } = Rhs;
    };
    /// <summary>A call expression.</summary>
    public sealed record CallNode(
      Position Position,
      LocationNode Callee,
      ImmediateNode[] Arguments,
      Signature.Signature ExpressionType
    ) : SimpleExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.CallExpressionNode;
      public LocationNode Callee { get; } = Callee;
      public ImmediateNode[] Arguments { get; } = Arguments;
    }
    /// <summary>A primitive call expression.</summary>
    public sealed record PrimCallNode(
      Position Position,
      PrimDefinition Callee,
      ImmediateNode[] Arguments,
      Signature.Signature ExpressionType
    ) : SimpleExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.PrimCallExpressionNode;
      public PrimDefinition Callee { get; } = Callee;
      public ImmediateNode[] Arguments { get; } = Arguments;
    }
    /// <summary>An array initialization expression.</summary>
    public sealed record ArrayInitNode(
      Position Position,
      ImmediateNode SizeImm,
      Signature.Signature ExpressionType
    ) : SimpleExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.ArrayInitExpressionNode;
      public ImmediateNode SizeImm { get; } = SizeImm;
    }
    /// <summary>
    /// An immediate expression, this is used to represent expressions that are already 
    /// in the form of an immediate, for example a variable reference or a constant.
    /// 
    /// NOTE: Usually these are generated during the anf transformation with the goal to optimize 
    ///       them away during optimizations. While there is no problem with having an immediate expression in the tree
    ///       it does indicate that there is some indirection as all expressions should take an immediate.
    /// </summary>
    public sealed record ImmediateExpressionNode(
      Position Position,
      ImmediateNode Imm,
      Signature.Signature ExpressionType
    ) : SimpleExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.ImmExpressionNode;
      public ImmediateNode Imm { get; } = Imm;
    }
  }
  #endregion
  // --- Immediates ---
  #region Immediates
  /// <summary>The supertype for all immediate nodes.</summary>
  [JsonDerivedType(typeof(ImmediateNode.ConstantNode), "ConstantImmediate")]
  [JsonDerivedType(typeof(ImmediateNode.LocationImmNode), "LocationImmediate")]
  public abstract record ImmediateNode : Node {
    public Signature.Signature Signature { get; }
    protected ImmediateNode(Position position, Signature.Signature signature) : base(position) {
      this.Signature = signature;
    }
    /// <summary>
    /// A constant node, this is used to represent literals and other constant values in the IR.
    /// </summary>
    public record ConstantNode(
      Position Position, LiteralNode Value, Signature.Signature Signature
    ) : ImmediateNode(Position, Signature) {
      public override NodeKind Kind => NodeKind.ConstantImmediate;
      public LiteralNode Value { get; } = Value;
    };
    /// <summary>
    /// A variable access node, this is used to represent variable accesses.
    /// </summary>
    // TODO: Get rid of locations convert them to basic binds at this level
    public record LocationImmNode(
      Position Position, LocationNode Location, Signature.Signature Signature
    ) : ImmediateNode(Position, Signature) {
      public override NodeKind Kind => NodeKind.LocationImmediate;
      public LocationNode Location { get; } = Location;
    };
  };
  #endregion
  // --- Literals ---
  #region Literals
  // <summary>The supertype for all literal nodes.</summary>
  [JsonDerivedType(typeof(LiteralNode.IntegerNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(LiteralNode.BooleanNode), "BooleanLiteral")]
  [JsonDerivedType(typeof(LiteralNode.CharacterNode), "CharacterLiteral")]
  [JsonDerivedType(typeof(LiteralNode.StringNode), "StringLiteral")]
  [JsonDerivedType(typeof(LiteralNode.FunctionReferenceNode), "FunctionReferenceLiteral")]

  public abstract record LiteralNode : Node {
    public Signature.Signature LiteralType { get; }
    protected LiteralNode(Position position, Signature.Signature literalType) : base(position) {
      this.LiteralType = literalType;
    }

    /// <summary>An integer literal.</summary>
    public sealed record IntegerNode(
      Position Position, int Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override NodeKind Kind => NodeKind.IntegerLiteralNode;
      public int Value { get; } = Value;
    };
    /// <summary>A boolean literal.</summary>
    public sealed record BooleanNode(
      Position Position, bool Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override NodeKind Kind => NodeKind.BooleanLiteralNode;
      public bool Value { get; } = Value;
    };
    /// <summary>A character literal.</summary>
    public sealed record CharacterNode(
      Position Position, char Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override NodeKind Kind => NodeKind.CharacterLiteralNode;
      public char Value { get; } = Value;
    };
    /// <summary>A string literal.</summary>
    public sealed record StringNode(
      Position Position, string Value, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override NodeKind Kind => NodeKind.StringLiteralNode;
      public string Value { get; } = Value;
    };
    /// <summary>A function reference literal.</summary>
    public sealed record FunctionReferenceNode(
      Position Position, Symbol ID, Signature.Signature LiteralType
    ) : LiteralNode(Position, LiteralType) {
      public override NodeKind Kind => NodeKind.FunctionReferenceLiteralNode;
      public Symbol ID { get; } = ID;
    };
  }
  #endregion
  // --- Locations ---
  #region Locations
  // TODO: Consider resolving locations to simple instructions at this level
  /// <summary>The supertype for all location nodes.</summary>
  [JsonDerivedType(typeof(LocationNode.ArrayNode), "ArrayLocationNode")]
  [JsonDerivedType(typeof(LocationNode.SymbolLocation), "SymbolLocationNode")]
  public abstract record LocationNode : Node {
    public Signature.Signature LocationType { get; }
    protected LocationNode(Position position, Signature.Signature locationType) : base(position) {
      this.LocationType = locationType;
    }
    /// <summary>An array location.</summary>
    public sealed record ArrayNode(
      Position Position,
      LocationNode Root,
      ImmediateNode IndexImm,
      Signature.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override NodeKind Kind => NodeKind.ArrayLocationNode;
      public LocationNode Root { get; } = Root;
      public ImmediateNode IndexImm { get; } = IndexImm;
    };
    /// <summary>A symbolic location.</summary>
    public sealed record SymbolLocation(
      Position Position,
      Symbol ID,
      Signature.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override NodeKind Kind => NodeKind.SymbolLocationNode;
      public Symbol ID { get; } = ID;
    };
  }
  #endregion
}
