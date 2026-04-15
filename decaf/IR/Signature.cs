using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Decaf.Utils;

namespace Decaf.IR.Signature {
  // The primitive types in our language
  // TODO: Give this a better name
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
  [JsonDerivedType(typeof(Signature.MethodSig), "MethodSignature")]
  [JsonDerivedType(typeof(Signature.ArraySig), "ArraySignature")]
  [JsonDerivedType(typeof(Signature.PrimitiveSig), "PrimitiveSignature")]
  public abstract record Signature {
    public Position Position { get; }
    private Signature(Position Position) { this.Position = Position; }
    public sealed record ModuleSig(Position Position, Dictionary<string, Signature> Members) : Signature(Position) {
      public override string ToString() {
        var sb = new System.Text.StringBuilder();
        foreach (var member in Members) {
          sb.Append($"{member.Key}: {member.Value}\n");
        }
        return $"Module {{\n{sb.ToString()}}}";
      }
    }
    public sealed record MethodSig(Position Position, Signature[] ParameterTypes, Signature ReturnType) : Signature(Position) {
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
    // TODO: Give this a better name
    public sealed record PrimitiveSig(Position Position, PrimitiveType Type) : Signature(Position) {
      public override string ToString() => Type switch {
        PrimitiveType.Int => "int",
        PrimitiveType.Boolean => "boolean",
        PrimitiveType.Character => "char",
        PrimitiveType.String => "string",
        PrimitiveType.Void => "void",
        // NOTE: This should never happen, if it does we forgot to update this method when we added a new primitive type
        _ => throw new Exception("Unknown primitive type")
      };
    }
  }
}
