using Decaf.IR.PrimitiveDefinition;
using Decaf.IR.TypedTree;
using Decaf.Utils;
using System.Text.Json.Serialization;

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
    // General
    ProgramNode,
    BlockNode,
    TypeNode,
    ParameterNode,
    // Declarations
    ModuleDeclNode,
    MethodDeclNode,
    GlobalDeclNode,
    // Instructions
    BindNode,
    AssignmentInstruction,
    ExprInstruction,
    IfInstruction,
    LoopInstruction,
    ContinueInstruction,
    BreakInstruction,
    ReturnInstruction,
    // Expressions
    CallExpression,
    PrimitiveExpression,
    BinopExpression,
    PrefixExpression,
    AllocateArrayExpression,
    // Immediates
    ConstantImmediate,
    LocationAccessImmediate,
    // Locations
    IdentifierLocation,
    MemberLocation,
    ArrayLocation
  }
  /// <summary>
  /// A base anf node that all other anf nodes inherit from.
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
  public record ProgramNode(Position Position, DeclarationNode.ModuleNode[] Modules, Scope<TypedTree.Signature> Scope) : Node(Position) {
    public override NodeKind Kind => NodeKind.ProgramNode;
    public DeclarationNode.ModuleNode[] Modules { get; } = Modules;
    public Scope<TypedTree.Signature> Scope { get; } = Scope;
  };
  public record BlockNode(
    Position Position,
    InstructionNode[] Instructions,
    Scope<TypedTree.Signature> Scope
  ) : Node(Position) {
    public override NodeKind Kind => NodeKind.BlockNode;
    public InstructionNode[] Instructions { get; } = Instructions;
    public Scope<TypedTree.Signature> Scope { get; } = Scope;
  }
  /// <summary>
  /// The supertype for all declaration nodes, we use a supertype to ensure strict type checking.
  /// </summary>
  [JsonDerivedType(typeof(ModuleNode), "ModuleDeclNode")]
  [JsonDerivedType(typeof(GlobalNode), "GlobalDeclNode")]
  [JsonDerivedType(typeof(MethodNode), "MethodDeclNode")]
  public abstract record DeclarationNode : Node {
    protected DeclarationNode(Position position) : base(position) { }
    /// <summary>A class declaration.</summary>
    public record ModuleNode(
      Position Position,
      string Name,
      // TODO: Get rid of the distinction between globals and methods, they are both just code
      GlobalNode[] Globals,
      MethodNode[] Methods,
      Scope<TypedTree.Signature> Scope,
      TypedTree.Signature.ClassSignature Signature
    ) : DeclarationNode(Position) {
      public override NodeKind Kind => NodeKind.ModuleDeclNode;
      public string Name { get; } = Name;
      public GlobalNode[] Globals { get; } = Globals;
      public MethodNode[] Methods { get; } = Methods;
      public Scope<TypedTree.Signature> Scope { get; } = Scope;
      public TypedTree.Signature.ClassSignature Signature { get; } = Signature;
    }
    /// <summary>A global declaration.</summary>
    public record GlobalNode(Position Position, string Name, TypedTree.Signature Signature) : DeclarationNode(Position) {
      public override NodeKind Kind => NodeKind.GlobalDeclNode;
      public string Name { get; } = Name;
      public TypedTree.Signature Signature { get; } = Signature;
    }
    /// <summary>A method declaration.</summary>
    public record MethodNode : DeclarationNode {
      public record ParameterNode(Position Position, string Name, TypedTree.Signature Signature) : DeclarationNode(Position) {
        public override NodeKind Kind => NodeKind.ParameterNode;
        public string Name { get; } = Name;
        public TypedTree.Signature Signature { get; } = Signature;
      }
      public override NodeKind Kind => NodeKind.MethodDeclNode;
      public string Name { get; }
      public ParameterNode[] Parameters { get; }
      public BlockNode Body { get; }
      public Scope<TypedTree.Signature> Scope { get; }
      public TypedTree.Signature.MethodSignature Signature { get; }
      public MethodNode(
        Position position,
        string name,
        ParameterNode[] parameters,
        BlockNode body,
        Scope<TypedTree.Signature> scope,
        TypedTree.Signature.MethodSignature signature
      ) : base(position) {
        this.Name = name;
        this.Parameters = parameters;
        this.Body = body;
        this.Scope = scope;
        this.Signature = signature;
      }
    }
  };
  /// <summary>
  /// The supertype for all instruction nodes, we use a supertype to ensure strict type checking.
  /// 
  /// In ANF an instruction is pretty much just a regular statement, the main difference is that 
  /// instead of consuming expressions they consume immediates. With the exception of a bind instruction which is used to bind
  /// the result of a simple expression to a variable.
  /// </summary>
  [JsonDerivedType(typeof(BindNode), "BindInstruction")]
  [JsonDerivedType(typeof(AssignmentNode), "AssignmentInstruction")]
  [JsonDerivedType(typeof(ExprNode), "ExpressionInstruction")]
  [JsonDerivedType(typeof(IfNode), "IfInstruction")]
  [JsonDerivedType(typeof(LoopNode), "LoopInstruction")]
  [JsonDerivedType(typeof(ContinueNode), "ContinueInstruction")]
  [JsonDerivedType(typeof(BreakNode), "BreakInstruction")]
  [JsonDerivedType(typeof(ReturnNode), "ReturnInstruction")]
  public abstract record InstructionNode : Node {
    protected InstructionNode(Position position) : base(position) { }
    /// <summary>A bind instruction, this is used to represent `int a = 1`;</summary>
    /// <param name="Position"></param>
    /// <param name="Location"></param>
    /// <param name="Expression"></param>
    public record BindNode(Position Position, LocationNode Location, ExpressionNode Expression) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.BindNode;
      public LocationNode Location { get; } = Location;
      public ExpressionNode Expression { get; } = Expression;
    };
    /// <summary>An assignment instruction.</summary>
    public record AssignmentNode(
      Position Position,
      LocationNode Location,
      ImmediateNode Expression
    ) : InstructionNode(Position) {
      // TODO: Merge AssignmentNode with BindNode
      public override NodeKind Kind => NodeKind.AssignmentInstruction;
      public LocationNode Location { get; } = Location;
      public ImmediateNode Expression { get; } = Expression;
    };
    /// <summary>An expression instruction.</summary>
    public record ExprNode(Position Position, ImmediateNode Content) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.ExprInstruction;
      public ImmediateNode Content { get; } = Content;
    };
    /// <summary>An if instruction.</summary>
    public record IfNode(
      Position Position,
      ImmediateNode Condition,
      BlockNode TrueBranch,
#nullable enable
      BlockNode? FalseBranch
#nullable disable
    ) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.IfInstruction;
      public ImmediateNode Condition { get; } = Condition;
      public BlockNode TrueBranch { get; } = TrueBranch;
#nullable enable
      public BlockNode? FalseBranch { get; } = FalseBranch;
#nullable disable
    };
    /// <summary>A loop instruction.</summary>
    public record LoopNode(Position Position, BlockNode Body) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.LoopInstruction;
      public BlockNode Body { get; } = Body;
    };
    /// <summary>A continue instruction.</summary>
    public record ContinueNode(Position Position) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.ContinueInstruction;
    };
    /// <summary>A break instruction.</summary>
    public record BreakNode(Position Position) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.BreakInstruction;
    };
    /// <summary>A return instruction.</summary>
#nullable enable
    public record ReturnNode(Position Position, ImmediateNode? Value) : InstructionNode(Position) {
#nullable disable
      public override NodeKind Kind => NodeKind.ReturnInstruction;
#nullable enable
      public ImmediateNode? Value { get; } = Value;
#nullable disable
    };
  };
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
  [JsonDerivedType(typeof(CallNode), "CallSimpleExpression")]
  [JsonDerivedType(typeof(PrimitiveNode), "PrimitiveSimpleExpression")]
  [JsonDerivedType(typeof(BinopNode), "BinopSimpleExpression")]
  [JsonDerivedType(typeof(PrefixNode), "PrefixSimpleExpression")]
  [JsonDerivedType(typeof(AllocateArrayNode), "AllocateArraySimpleExpression")]
  public abstract record ExpressionNode : Node {
    public TypedTree.Signature ExpressionType { get; }
    protected ExpressionNode(Position position, TypedTree.Signature expressionType) : base(position) {
      this.ExpressionType = expressionType;
    }
    /// <summary>A call expression.</summary>
    public record CallNode(
      Position Position,
      LocationNode Path,
      ImmediateNode[] Arguments,
      TypedTree.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.CallExpression;
      public LocationNode Path { get; } = Path;
      public ImmediateNode[] Arguments { get; } = Arguments;
    };
    /// <summary>A primitive expression.</summary>
    public record PrimitiveNode(
      Position Position,
      PrimDefinition Primitive,
      ImmediateNode[] Arguments,
      TypedTree.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.PrimitiveExpression;
      public PrimDefinition Primitive { get; } = Primitive;
      public ImmediateNode[] Arguments { get; } = Arguments;
    };
    /// <summary>A binop expression.</summary>
    public record BinopNode(
      Position Position,
      ImmediateNode Lhs,
      string Operator,
      ImmediateNode Rhs,
      TypedTree.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.BinopExpression;
      public ImmediateNode Lhs { get; } = Lhs;
      public string Operator { get; } = Operator;
      public ImmediateNode Rhs { get; } = Rhs;
    };
    /// <summary>A prefix expression.</summary>
    public record PrefixNode(
      Position Position,
      string Operator,
      ImmediateNode Operand,
      TypedTree.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.PrefixExpression;
      public string Operator { get; } = Operator;
      public ImmediateNode Operand { get; } = Operand;
    };
    /// <summary>A new array expression.</summary>
    public record AllocateArrayNode(
      Position Position,
      ImmediateNode SizeImm,
      TypedTree.Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override NodeKind Kind => NodeKind.AllocateArrayExpression;
      public ImmediateNode SizeImm { get; } = SizeImm;
    };
  }
  /// <summary>
  /// The supertype for all immediate nodes, we use a supertype to ensure strict type checking.
  /// </summary>
  public abstract record ImmediateNode : Node {
    public Signature Signature { get; }
    protected ImmediateNode(Position position, Signature signature) : base(position) {
      this.Signature = signature;
    }
    /// <summary>
    /// A constant node, this is used to represent literals and other constant values in the IR.
    /// </summary>
    public record ConstantNode(
      Position Position, TypedTree.LiteralNode Value, Signature Signature
    ) : ImmediateNode(Position, Signature) {
      public override NodeKind Kind => NodeKind.ConstantImmediate;
      public TypedTree.LiteralNode Value { get; } = Value;
    };
    /// <summary>
    /// A variable access node, this is used to represent variable accesses.
    /// </summary>
    public record LocationAccessNode(
      Position Position, LocationNode Location, Signature Signature
    ) : ImmediateNode(Position, Signature) {
      public override NodeKind Kind => NodeKind.LocationAccessImmediate;
      public LocationNode Location { get; } = Location;
    };
  };
  // TODO: Get rid of locations convert them to basic binds at this level
  /// <summary>
  /// The supertype for all location nodes, we use a supertype to ensure strict type checking. 
  /// A location node represents any access to a variable, field or array.
  /// </summary>
  [JsonDerivedType(typeof(IdentifierAccessNode), "IdentifierLocation")]
  [JsonDerivedType(typeof(MemberAccessNode), "MemberLocation")]
  [JsonDerivedType(typeof(ArrayAccessNode), "ArrayLocation")]
  public abstract record LocationNode : Node {
    public TypedTree.Signature LocationType { get; }
    protected LocationNode(Position position, TypedTree.Signature locationType) : base(position) { this.LocationType = locationType; }
    /// <summary>
    /// An identifier node, is used to represent a simple variable access of the form `x`.
    /// </summary>
    public record IdentifierAccessNode(
      Position Position,
      string Name,
      TypedTree.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override NodeKind Kind => NodeKind.IdentifierLocation;
      public string Name { get; } = Name;
    };
    /// <summary>
    /// A member access node, is used to represent a member access of the form `x.y` where `x` is the root and `y` is the member.
    /// </summary>
    public record MemberAccessNode(
      Position Position,
      LocationNode.IdentifierAccessNode Root,
      string Member,
      TypedTree.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override NodeKind Kind => NodeKind.MemberLocation;
      public LocationNode.IdentifierAccessNode Root { get; } = Root;
      public string Member { get; } = Member;
    };
    /// <summary>
    /// An array access node, is used to represent an array access of the form `x[y]` where `x` is the root and `y` is the index.
    /// </summary>
    public record ArrayAccessNode(
      Position Position,
      LocationNode Root,
      ImmediateNode IndexImm,
      TypedTree.Signature LocationType
    ) : LocationNode(Position, LocationType) {
      public override NodeKind Kind => NodeKind.ArrayLocation;
      public LocationNode Root { get; } = Root;
      public ImmediateNode IndexImm { get; } = IndexImm;
    };
  }
}
