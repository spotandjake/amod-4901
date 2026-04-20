using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Decaf.Utils;

namespace Decaf.IR.Signature {
  // The primitive types in our language
  public enum PrimitiveType {
    Int,
    Boolean,
    Character,
    String,
    Void
  }
  /// <summary>
  /// A signature represents the type information of a given item in our program.
  /// This is used for type checking and eventually code generation.
  /// </summary>
  [JsonDerivedType(typeof(Signature.ModuleSig), "ModuleSignature")]
  [JsonDerivedType(typeof(Signature.FunctionSig), "FunctionSignature")]
  [JsonDerivedType(typeof(Signature.ArraySig), "ArraySignature")]
  [JsonDerivedType(typeof(Signature.PrimitiveSig), "PrimitiveSignature")]
  public abstract record Signature {
    public Position Position { get; }
    private Signature(Position Position) { this.Position = Position; }
    public sealed record ModuleSig(
      Position Position,
      Dictionary<Symbol, Signature> Members,
      Dictionary<string, Symbol> Resolutions
    ) : Signature(Position) {
      public override string ToString() {
        var sb = new System.Text.StringBuilder();
        foreach (var member in Members) {
          sb.Append($"{member.Key.Name}: {member.Value}\n");
        }
        return $"Module {{\n{sb.ToString()}}}";
      }
    }
    public sealed record FunctionSig(Position Position, Signature[] ParameterTypes, Signature ReturnType) : Signature(Position) {
      public override string ToString() {
        var sb = new System.Text.StringBuilder();
        foreach (var param in ParameterTypes) {
          sb.Append(param.ToString());
          sb.Append(", ");
        }
        return $"({sb.ToString()}) => {ReturnType}";
      }
    }
    public sealed record ArraySig(Position Position, Signature Typ) : Signature(Position) {
      public override string ToString() => $"{Typ}[]";
    }
    public sealed record PrimitiveSig(Position Position, PrimitiveType Type) : Signature(Position) {
      public override string ToString() => Enum.GetName(Type);
    }
  }
}
