using Decaf.IR.AnfTree.LiteralNodes;
using Decaf.IR.ParseTree;
using Decaf.IR.TypedTree;
using Decaf.Utils;
using System.Collections.Generic;
using System.Text.Json.Serialization;

// This namespace contains the definition for our typed tree.
// The typed tree is pretty much the exact same as the parse tree except it attaches extra type information to each node.
// This tree is generated from our typechecker and the typing information can be used later during codegen and analysis.
namespace Decaf.IR.AnfTree {
  // A primitive type represents the basic types in our language

  // ? RM ME:I think I should be able to remove this because it's already defined in typedtree. 
/*   public enum PrimitiveType {
    Int,
    Character,
    String,
    Boolean,
    Void,
    Null
  } */
  /// <summary>
  /// A base ANF Tree node that all other ANF tree nodes inherit from.
  /// </summary>
  public abstract record Node {
    public abstract ParseTree.NodeKind Kind { get; }
    public Position Position { get; }
    protected Node(Position position) { this.Position = position; }
  };
  
  public record ProgramNode(Position Position, DeclarationNode.ClassNode[] Classes, Scope<Signature> Scope) : Node(Position) {
    public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ProgramNode;
    public DeclarationNode.ClassNode[] Classes { get; } = Classes;
    public Scope<Signature> Scope { get; } = Scope;
  };
  public record BlockNode(
    Position Position,
    DeclarationNode.VariableNode[] Declarations,
    InstructionNode[] Instructions, // * statements are now refered as instructions?
    Scope<Signature> Scope
  ) : Node(Position) {
    public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BlockNode;
    public DeclarationNode.VariableNode[] Declarations { get; } = Declarations;
    public InstructionNode[] Instructions { get; } = Instructions;
    public Scope<Signature> Scope { get; } = Scope;
  }
  /// <summary>
  /// The supertype for all declaration nodes, we use a supertype to ensure strict type checking.
  /// </summary>
  [JsonDerivedType(typeof(ClassNode), "ClassDeclNode")]
  [JsonDerivedType(typeof(VariableNode), "VarDeclNode")]
  [JsonDerivedType(typeof(MethodNode), "MethodDeclNode")]
  public abstract record DeclarationNode : Node {
    protected DeclarationNode(Position position) : base(position) { }
    /// <summary>A class declaration.</summary>
    public record ClassNode : DeclarationNode {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ClassDeclNode;
      public string Name { get; }
#nullable enable
      public string? SuperClassName { get; }
#nullable disable
      public VariableNode[] Fields { get; }
      public MethodNode[] Methods { get; }
      public Scope<Signature> Scope { get; }
      public Signature.ClassSignature Signature { get; }
      public ClassNode(
        Position position,
        string name,
#nullable enable
        string? superClassName,
#nullable disable
        VariableNode[] fields,
        MethodNode[] methods,
        Scope<Signature> scope,
        Signature.ClassSignature signature
      ) : base(position) {
        this.Name = name;
        this.SuperClassName = superClassName;
        this.Fields = fields;
        this.Methods = methods;
        this.Scope = scope;
        this.Signature = signature;
      }
    }
    /// <summary>A variable declaration.</summary>
    public record VariableNode : DeclarationNode {
      public record BindNode(Position Position, string Name, Signature Signature) : Node(Position) {
        public override ParseTree.NodeKind Kind => ParseTree.NodeKind.VarBindNode;
        public string Name { get; } = Name;
        public Signature Signature { get; } = Signature;
      }
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.VariableDeclNode;
      public BindNode[] Binds { get; }
      public VariableNode(Position position, BindNode[] Binds) : base(position) {
        this.Binds = Binds;
      }
    }
    /// <summary>A method declaration.</summary>
    public record MethodNode : DeclarationNode {
      public record ParameterNode : DeclarationNode {
        public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ParameterNode;
        public string Name { get; }
        public Signature Signature { get; }
        public ParameterNode(
          Position position,
          string name,
          Signature signature
        ) : base(position) {
          this.Name = name;
          this.Signature = signature;
        }
      }
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.MethodDeclNode;
      public string Name { get; }
      public ParameterNode[] Parameters { get; }
      public BlockNode Body { get; }
      public Scope<Signature> Scope { get; }
      public Signature.MethodSignature Signature { get; }
      public MethodNode(
        Position position,
        string name,
        ParameterNode[] parameters,
        BlockNode body,
        Scope<Signature> scope,
        Signature.MethodSignature signature
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
  /// The supertype for all statement nodes, we use a supertype to ensure strict type checking.
  /// </summary>
  [JsonDerivedType(typeof(AssignmentNode), "AssignmentInstruction")]
  [JsonDerivedType(typeof(ExprNode), "ExpressionInstruction")]
  [JsonDerivedType(typeof(IfNode), "IfInstruction")]
  [JsonDerivedType(typeof(WhileNode), "WhileInstruction")]
  [JsonDerivedType(typeof(ContinueNode), "ContinueInstruction")]
  [JsonDerivedType(typeof(BreakNode), "BreakInstruction")]
  [JsonDerivedType(typeof(ReturnNode), "ReturnInstruction")]
  [JsonDerivedType(typeof(BindNode), "BindInstruction")]
  public abstract record InstructionNode : Node {

    protected InstructionNode(Position position) : base(position) { }
    /// <summary>An assignment statement.</summary>
    public record AssignmentNode(
      Position Position,
      ExpressionNode.LocationNode Location,
      ExpressionNode Expression
    ) : InstructionNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.AssignmentStatement;
      public ExpressionNode.LocationNode Location { get; } = Location;
      public ExpressionNode Expression { get; } = Expression;
    };
    /// <summary>An expression statement.</summary>
    public record ExprNode(Position Position, ExpressionNode Content) : InstructionNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ExprStatement;
      public ExpressionNode Content { get; } = Content;
    };
    /// <summary>An if statement.</summary>
    public record IfNode(
      Position Position,
      ExpressionNode Condition,
      BlockNode TrueBranch,
#nullable enable
      BlockNode? FalseBranch
#nullable disable
    ) : InstructionNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.IfStatement;
      public ExpressionNode Condition { get; } = Condition;
      public BlockNode TrueBranch { get; } = TrueBranch;
#nullable enable
      public BlockNode? FalseBranch { get; } = FalseBranch;
#nullable disable
    };
    /// <summary>A while loop statement.</summary>
    public record WhileNode(Position Position, ExpressionNode Condition, BlockNode Body) : InstructionNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.WhileStatement;
      public ExpressionNode Condition { get; } = Condition;
      public BlockNode Body { get; } = Body;
    };
    /// <summary>A continue statement.</summary>
    public record ContinueNode(Position Position) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.ContinueStatement;
    };
    /// <summary>A break statement.</summary>
    public record BreakNode(Position Position) : InstructionNode(Position) {
      public override NodeKind Kind => NodeKind.BreakStatement;
    };

    public record BindNode(Position Position, ExpressionNode Value, Signature Signature, String Name) : InstructionNode(Position){

      public String Name { get; } = Name;
      public ExpressionNode Value { get; } = Value;
      public Signature Signature { get; } = Signature;
    };
    /// <summary>A return statement.</summary>
#nullable enable
    public record ReturnNode(Position Position, ExpressionNode? Value) : InstructionNode(Position) {
#nullable disable
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ReturnStatement;
#nullable enable
      public ExpressionNode? Value { get; } = Value;

#nullable disable
    };
  };
  /// <summary>
  /// The supertype for all expression nodes, we use a supertype to ensure strict type checking.
  /// </summary>
  [JsonDerivedType(typeof(CallNode), "CallExpression")]
  [JsonDerivedType(typeof(BinopNode), "BinopExpression")]
  [JsonDerivedType(typeof(PrefixNode), "PrefixExpression")]
  [JsonDerivedType(typeof(LocationNode), "LocationExpression")]
  [JsonDerivedType(typeof(ThisNode), "ThisExpression")]
  [JsonDerivedType(typeof(IdentifierNode), "IdentifierExpression")]
  [JsonDerivedType(typeof(LiteralNode), "LiteralExpression")]
  public abstract record ExpressionNode : Node {
    public Signature ExpressionType { get; }
    protected ExpressionNode(Position position, Signature expressionType) : base(position) {
      this.ExpressionType = expressionType;
    }
    /// <summary>A call expression.</summary>
    public record CallNode(
      Position Position,
      bool IsPrimitive,
      LocationNode Path,
      ExpressionNode[] Arguments,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.CallExpression;
      public bool IsPrimitive { get; } = IsPrimitive;
      public LocationNode Path { get; } = Path;
      public ExpressionNode[] Arguments { get; } = Arguments;
    };
    /// <summary>A binop expression.</summary>
    public record BinopNode(
      Position Position,
      ExpressionNode Lhs,
      string Operator,
      ExpressionNode Rhs,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BinopExpression;
      public ExpressionNode Lhs { get; } = Lhs;
      public string Operator { get; } = Operator;
      public ExpressionNode Rhs { get; } = Rhs;
    };
    /// <summary>A prefix expression.</summary>
    public record PrefixNode(
      Position Position,
      string Operator,
      ExpressionNode Operand,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.PrefixExpression;
      public string Operator { get; } = Operator;
      public ExpressionNode Operand { get; } = Operand;
    };
    /// <summary>A new class expression.</summary>
    public record NewClassNode(
      Position Position,
      LocationNode Path,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.NewArrayExpression;
      public LocationNode Path { get; } = Path;
    };
    /// <summary>A new array expression.</summary>
    public record NewArrayNode(
      Position Position,
      ExpressionNode SizeExpr,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.NewArrayExpression;
      public ExpressionNode SizeExpr { get; } = SizeExpr;
    };
    /// <summary>A location access expression.</summary>
    public record LocationNode(
      Position Position,
      ExpressionNode Root,
#nullable enable
      string? Path,
      ExpressionNode? IndexExpr,
#nullable disable
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.LocationExpression;
      public ExpressionNode Root { get; } = Root;
#nullable enable
      public string? Path { get; } = Path;
      public ExpressionNode? IndexExpr { get; } = IndexExpr;
#nullable disable
    };
    /// <summary>A `this` expression.</summary>
    public record ThisNode(Position Position, Signature ExpressionType) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.ThisExpression;
    };
    /// <summary>An identifier expression.</summary>
    public record IdentifierNode(
      Position Position,
      string Name,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.IdentifierExpression;
      public string Name { get; } = Name;
    };
    /// <summary>A literal expression.</summary>
    public record LiteralNode(
      Position Position,
      AnfTree.LiteralNode Content,
      Signature ExpressionType
    ) : ExpressionNode(Position, ExpressionType) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.LiteralExpression;
      public AnfTree.LiteralNode Content { get; } = Content;
    };
  }
  /// <summary>
  /// The supertype for all literal nodes, we use a supertype to ensure strict type checking.
  /// </summary>
  [JsonDerivedType(typeof(LiteralNodes.IntegerNode), "IntegerLiteral")]
  [JsonDerivedType(typeof(LiteralNodes.CharacterNode), "CharLiteral")]
  [JsonDerivedType(typeof(LiteralNodes.StringNode), "StringLiteral")]
  [JsonDerivedType(typeof(LiteralNodes.BooleanNode), "BoolLiteral")]
  [JsonDerivedType(typeof(LiteralNodes.NullNode), "NullLiteral")]
  public abstract record LiteralNode : Node {
    protected LiteralNode(Position position) : base(position) { }
  };
  namespace LiteralNodes {
    /// <summary>An integer literal node.</summary>
    public record IntegerNode(Position Position, int Value) : LiteralNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.IntegerLiteral;
      public int Value { get; } = Value;
    };
    /// <summary>A character literal node.</summary>
    public record CharacterNode(Position Position, char Value) : LiteralNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.CharacterLiteral;
      public char Value { get; } = Value;
    };
    /// <summary>A string literal node.</summary>
    public record StringNode(Position Position, string Value) : LiteralNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.StringLiteral;
      public string Value { get; } = Value;
    };
    /// <summary>An boolean literal node.</summary>
    public record BooleanNode(Position Position, bool Value) : LiteralNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.BooleanLiteral;
      public bool Value { get; } = Value;
    };
    /// <summary>An null literal node.</summary>
    public record NullNode(Position Position) : LiteralNode(Position) {
      public override ParseTree.NodeKind Kind => ParseTree.NodeKind.NullLiteral;
    };
  }
}
