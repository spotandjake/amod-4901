// using Decaf.IR.ParseTree;
// using Decaf.Utils;
// using System.Collections.Generic;
// using System.Text.Json.Serialization;

// #nullable enable
// namespace Decaf.IR.TypedTree {
//   // A signature represents the type information for a given declaration. This is used for type checking and method resolution.
//   public abstract record Signature {
//     private Signature() { }
//     public record ClassSignature(Dictionary<string, Signature> Members) : Signature;
//     public record MethodSignature(Signature ReturnType, Signature[] ParameterTypes) : Signature;
//     public record ArraySignature(Signature Typ) : Signature;
//     // TODO: I want to handle this better
//     public record Simple(ParseTree.TypeNode.PrimitiveType Type) : Signature;
//   }
//   // Typed Program Nodes
//   public record ProgramNode(
//     Position Position,
//     DeclarationNode.ClassNode[] Classes,
//     Scope<Signature>? Scope
//   ) : ParseTree.ProgramNode(Position, Classes, null) {
//     public new Scope<Signature>? Scope { get; } = Scope;
//   }
//   public record BlockNode(
//     Position Position,
//     DeclarationNode.VariableNode[] Declarations,
//     StatementNode[] Statements,
//     Scope<Signature>? Scope
//   ) : ParseTree.BlockNode(Position, Declarations, Statements, null) {
//     public new Scope<Signature>? Scope { get; } = Scope;
//   }
//   // /// <summary>
//   // /// The supertype for all declaration nodes, we use a supertype to ensure strict type checking.
//   // /// </summary>
//   // [JsonDerivedType(typeof(ClassNode), "ClassDeclNode")]
//   // [JsonDerivedType(typeof(VariableNode), "VarDeclNode")]
//   // [JsonDerivedType(typeof(MethodNode), "MethodDeclNode")]
//   // public abstract record DeclarationNode : Node {
//   //   protected DeclarationNode(Position position) : base(position) { }
//   //   /// <summary>A class declaration.</summary>
//   //   public record ClassNode : Node {
//   //     public override NodeKind Kind => NodeKind.ClassDeclNode;
//   //     public string Name { get; }
//   //     public string? SuperClassName { get; }
//   //     public VariableNode[] Fields { get; }
//   //     public MethodNode[] Methods { get; }
//   //     public Scope<bool>? Scope { get; }
//   //     public ClassNode(
//   //       Position position,
//   //       string name,
//   //       string? superClassName,
//   //       VariableNode[] fields,
//   //       MethodNode[] methods,
//   //       Scope<bool>? scope
//   //     ) : base(position) {
//   //       this.Name = name;
//   //       this.SuperClassName = superClassName;
//   //       this.Fields = fields;
//   //       this.Methods = methods;
//   //       this.Scope = scope;
//   //     }
//   //   }
//   //   /// <summary>A variable declaration.</summary>
//   //   public record VariableNode : Node {
//   //     public record BindNode(Position Position, string Name, bool IsArray) : Node(Position) {
//   //       public override NodeKind Kind => NodeKind.VarBindNode;
//   //       public string Name { get; } = Name;
//   //       public bool IsArray { get; } = IsArray;
//   //     }
//   //     public override NodeKind Kind => NodeKind.VariableDeclNode;
//   //     public TypeNode Type { get; }
//   //     public BindNode[] Binds { get; }
//   //     public VariableNode(Position position, TypeNode Type, BindNode[] Binds) : base(position) {
//   //       this.Type = Type;
//   //       this.Binds = Binds;
//   //     }
//   //   }
//   //   /// <summary>A method declaration.</summary>
//   //   public record MethodNode : Node {
//   //     public record ParameterNode : Node {
//   //       public override NodeKind Kind => NodeKind.ParameterNode;
//   //       public TypeNode ParamType { get; }
//   //       public string Name { get; }
//   //       public bool IsArray { get; }
//   //       public ParameterNode(Position position, TypeNode paramType, string name, bool isArray) : base(position) {
//   //         this.ParamType = paramType;
//   //         this.Name = name;
//   //         this.IsArray = isArray;
//   //       }
//   //     }
//   //     public override NodeKind Kind => NodeKind.MethodDeclNode;
//   //     public TypeNode ReturnType { get; }
//   //     public string Name { get; }
//   //     public ParameterNode[] Parameters { get; }
//   //     public BlockNode Body { get; }
//   //     public Scope<bool>? Scope { get; }
//   //     public MethodNode(
//   //       Position position,
//   //       TypeNode returnType,
//   //       string name,
//   //       ParameterNode[] parameters,
//   //       BlockNode body,
//   //       Scope<bool>? scope
//   //     ) : base(position) {
//   //       this.ReturnType = returnType;
//   //       this.Name = name;
//   //       this.Parameters = parameters;
//   //       this.Body = body;
//   //       this.Scope = scope;
//   //     }
//   //   }
//   // };
//   // /// <summary>
//   // /// The supertype for all statement nodes, we use a supertype to ensure strict type checking.
//   // /// </summary>
//   // [JsonDerivedType(typeof(AssignmentNode), "AssignmentStatement")]
//   // [JsonDerivedType(typeof(ExprNode), "ExpressionStatement")]
//   // [JsonDerivedType(typeof(IfNode), "IfStatement")]
//   // [JsonDerivedType(typeof(WhileNode), "WhileStatement")]
//   // [JsonDerivedType(typeof(ReturnNode), "ReturnStatement")]
//   // public abstract record StatementNode : Node {
//   //   protected StatementNode(Position position) : base(position) { }
//   //   /// <summary>An assignment statement.</summary>
//   //   public record AssignmentNode(
//   //     Position Position,
//   //     ExpressionNode.LocationNode Location,
//   //     ExpressionNode Expression
//   //   ) : StatementNode(Position) {
//   //     public override NodeKind Kind => NodeKind.AssignmentStatement;
//   //     public ExpressionNode.LocationNode Location { get; } = Location;
//   //     public ExpressionNode Expression { get; } = Expression;
//   //   };
//   //   /// <summary>An expression statement.</summary>
//   //   public record ExprNode(Position Position, ExpressionNode Content) : StatementNode(Position) {
//   //     public override NodeKind Kind => NodeKind.ExprStatement;
//   //     public ExpressionNode Content { get; } = Content;
//   //   };
//   //   /// <summary>An if statement.</summary>
//   //   public record IfNode(
//   //     Position Position,
//   //     ExpressionNode Condition,
//   //     BlockNode TrueBranch,
//   //     BlockNode? FalseBranch
//   //   ) : StatementNode(Position) {
//   //     public override NodeKind Kind => NodeKind.IfStatement;
//   //     public ExpressionNode Condition { get; } = Condition;
//   //     public BlockNode TrueBranch { get; } = TrueBranch;
//   //     public BlockNode? FalseBranch { get; } = FalseBranch;
//   //   };
//   //   /// <summary>A while loop statement.</summary>
//   //   public record WhileNode(Position Position, ExpressionNode Condition, BlockNode Body) : StatementNode(Position) {
//   //     public override NodeKind Kind => NodeKind.WhileStatement;
//   //     public ExpressionNode Condition { get; } = Condition;
//   //     public BlockNode Body { get; } = Body;
//   //   };
//   //   /// <summary>A return statement.</summary>
//   //   public record ReturnNode(Position Position, ExpressionNode? Value) : StatementNode(Position) {
//   //     public override NodeKind Kind => NodeKind.ReturnStatement;
//   //     public ExpressionNode? Value { get; } = Value;
//   //   };
//   // };
//   // /// <summary>
//   // /// The supertype for all expression nodes, we use a supertype to ensure strict type checking.
//   // /// </summary>
//   // [JsonDerivedType(typeof(CallNode), "CallExpression")]
//   // [JsonDerivedType(typeof(BinopNode), "BinopExpression")]
//   // [JsonDerivedType(typeof(PrefixNode), "PrefixExpression")]
//   // [JsonDerivedType(typeof(LocationNode), "LocationExpression")]
//   // [JsonDerivedType(typeof(ThisNode), "ThisExpression")]
//   // [JsonDerivedType(typeof(IdentifierNode), "IdentifierExpression")]
//   // [JsonDerivedType(typeof(LiteralNode), "LiteralExpression")]
//   // public abstract record ExpressionNode : Node {
//   //   protected ExpressionNode(Position position) : base(position) { }
//   //   /// <summary>A call expression.</summary>
//   //   public record CallNode(
//   //     Position Position,
//   //     bool IsPrimitive,
//   //     LocationNode Path,
//   //     ExpressionNode[] Arguments
//   //   ) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.CallExpression;
//   //     public bool IsPrimitive { get; } = IsPrimitive;
//   //     public LocationNode Path { get; } = Path;
//   //     public ExpressionNode[] Arguments { get; } = Arguments;
//   //   };
//   //   /// <summary>A binop expression.</summary>
//   //   public record BinopNode(Position Position, ExpressionNode Lhs, string Operator, ExpressionNode Rhs) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.BinopExpression;
//   //     public ExpressionNode Lhs { get; } = Lhs;
//   //     public string Operator { get; } = Operator;
//   //     public ExpressionNode Rhs { get; } = Rhs;
//   //   };
//   //   /// <summary>A prefix expression.</summary>
//   //   public record PrefixNode(Position Position, string Operator, ExpressionNode Operand) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.PrefixExpression;
//   //     public string Operator { get; } = Operator;
//   //     public ExpressionNode Operand { get; } = Operand;
//   //   };
//   //   /// <summary>A location access expression.</summary>
//   //   public record LocationNode(
//   //     Position Position,
//   //     ExpressionNode Root,
//   //     string? Path,
//   //     ExpressionNode? IndexExpr
//   //   ) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.LocationExpression;
//   //     public ExpressionNode Root { get; } = Root;
//   //     public string? Path { get; } = Path;
//   //     public ExpressionNode? IndexExpr { get; } = IndexExpr;
//   //   };
//   //   /// <summary>A `this` expression.</summary>
//   //   public record ThisNode(Position Position) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.ThisExpression;
//   //   };
//   //   /// <summary>An identifier expression.</summary>
//   //   public record IdentifierNode(Position Position, string Name) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.IdentifierExpression;
//   //     public string Name { get; } = Name;
//   //   };
//   //   /// <summary>A literal expression.</summary>
//   //   public record LiteralNode(Position Position) : ExpressionNode(Position) {
//   //     public override NodeKind Kind => NodeKind.LiteralExpression;
//   //   };
//   // }
//   // /// <summary>
//   // /// The supertype for all literal nodes, we use a supertype to ensure strict type checking.
//   // /// </summary>
//   // [JsonDerivedType(typeof(IntegerNode), "IntegerLiteral")]
//   // [JsonDerivedType(typeof(CharacterNode), "CharLiteral")]
//   // [JsonDerivedType(typeof(StringNode), "StringLiteral")]
//   // [JsonDerivedType(typeof(BooleanNode), "BoolLiteral")]
//   // [JsonDerivedType(typeof(NullNode), "NullLiteral")]
//   // public abstract record LiteralNode : ExpressionNode {
//   //   protected LiteralNode(Position position) : base(position) { }
//   //   /// <summary>An integer literal node.</summary>
//   //   public record IntegerNode(Position Position, int Value) : LiteralNode(Position) {
//   //     public override NodeKind Kind => NodeKind.IntegerLiteral;
//   //     public int Value { get; } = Value;
//   //   };
//   //   /// <summary>A character literal node.</summary>
//   //   public record CharacterNode(Position Position, char Value) : LiteralNode(Position) {
//   //     public override NodeKind Kind => NodeKind.CharacterLiteral;
//   //     public char Value { get; } = Value;
//   //   };
//   //   /// <summary>A string literal node.</summary>
//   //   public record StringNode(Position Position, string Value) : LiteralNode(Position) {
//   //     public override NodeKind Kind => NodeKind.StringLiteral;
//   //     public string Value { get; } = Value;
//   //   };
//   //   /// <summary>An boolean literal node.</summary>
//   //   public record BooleanNode(Position Position, bool Value) : LiteralNode(Position) {
//   //     public override NodeKind Kind => NodeKind.BooleanLiteral;
//   //     public bool Value { get; } = Value;
//   //   };
//   //   /// <summary>An null literal node.</summary>
//   //   public record NullNode(Position Position) : LiteralNode(Position) {
//   //     public override NodeKind Kind => NodeKind.NullLiteral;
//   //   };
//   // };
// }
