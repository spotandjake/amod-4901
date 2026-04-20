using System.Collections.Generic;
using System.Text;
using Decaf.Utils.Errors.ScopeErrors;

namespace Decaf.Utils {
  // A shared generator for unique IDs
  public sealed record IDGenerator {
    private uint Counter { get; set; } = 0;
    public uint GenerateID() => Counter++;
  }
  /// <summary>A symbol, is a unique identifier for a declaration in the program.</summary>
#nullable enable
  public sealed class Symbol {
    public Position Position { get; }
    public bool IsGlobal { get; }
    public string? Name;
    public uint ID { get; }
    // Symbols can only be created through the `create` method, which ensures that each symbol has a unique ID.
    private Symbol(Position Position, uint ID, bool IsGlobal, string? Name = null) {
      this.Position = Position;
      this.IsGlobal = IsGlobal;
      this.ID = ID;
      this.Name = Name;
    }
    public override bool Equals(object? obj) => obj is Symbol other && this.ID == other.ID;
    public override int GetHashCode() => ID.GetHashCode();
    public string GetUniqueName() => $"sym_{this.Name}_{this.ID}";
    public static Symbol Create(IDGenerator generator, Position position, bool isGlobal, string? name = null) {
      var id = generator.GenerateID();
      return new Symbol(position, id, isGlobal, name);
    }
  };
#nullable restore
  /// <summary>
  /// This class represents a scope.
  /// 
  /// Scopes follow a hierarchical structure, where each scope can have a parent scope. 
  /// This allows for nested scopes, such as those created by functions or blocks.
  /// 
  /// When looking up a declaration, the scope will check for the first occurrence of the declaration in the hierarchy.
  /// </summary>
  /// <typeparam name="TKey">The type of the declaration id.</typeparam>
  /// <typeparam name="T">The value to be tracked related to a declaration.</typeparam>
  /// <param name="parent">The parent scope.</param>
#nullable enable
  public class Scope<TKey, T>(Scope<TKey, T>? parent) where TKey : notnull {
    /// <summary>
    /// The parent scope, or null if this is the global scope.
    /// </summary>
    private Scope<TKey, T>? Parent { get; } = parent;
    /// <summary>
    /// The declarations in this scope, mapping from the declaration id to the value being tracked.
    /// </summary>
    private Dictionary<TKey, T> Declarations { get; } = [];
    public int Size => (Parent?.Size ?? 0) + Declarations.Count;
    /// <summary>
    /// Adds a declaration to the current scope. If a declaration with the same id already exists 
    /// in the current scope an exception will be thrown.
    /// 
    /// Note that this method does not check for declarations in parent scopes, 
    /// so it is possible to shadow declarations from parent scopes without an error.
    /// </summary>
    /// <param name="id">The id of the declaration to add.</param>
    /// <param name="value">The value to associate with the declaration.</param>
    /// <exception cref="DuplicateDeclarationException">
    /// If a declaration with the same id already exists in the current scope.
    /// </exception>
    public void AddDeclaration(Position position, TKey id, T value) {
      if (this.HasDeclaration(id, false)) throw new DuplicateDeclarationException(position, id.ToString());
      this.Declarations.Add(id, value);
    }
    /// <summary>
    /// Checks if a declaration with the given id exists in the current scope or any parent scope.
    /// </summary>
    /// <param name="id">The id of the declaration to check.</param>
    /// <param name="searchParent">Whether to search parent scopes.</param>
    /// <returns>`true` if the declaration exists, `false` otherwise.</returns>
    public bool HasDeclaration(TKey id, bool searchParent = true) {
      // Check if the declaration exists in the current scope
      if (this.Declarations.ContainsKey(id)) return true;
      // Check if the declaration exists in the parent scope (if enabled)
      if (searchParent && this.Parent != null && this.Parent.HasDeclaration(id)) return true;
      // Otherwise the declaration does not exist in this scope or any parent scope
      return false;
    }
    /// <summary>
    /// Gets the value associated with the declaration with the given id. If the declaration does not exist in the current scope,
    /// </summary>
    /// <param name="position">The position of the declaration access being searched.</param>
    /// <param name="id">The id of the declaration to get.</param>
    /// <returns>The value associated with the declaration.</returns>
    /// <exception cref="DeclarationNotDefinedException"></exception>
    public T GetDeclaration(Position position, TKey id) {
      // Get the variable from the current scope
      if (this.Declarations.ContainsKey(id)) return Declarations[id];
      // Get the variable from the parent scope
      if (this.Parent != null) return this.Parent.GetDeclaration(position, id);
      // Otherwise the variable does not exist in this scope or any parent scope
      throw new DeclarationNotDefinedException(position, id.ToString());
    }
    /// <summary>
    /// Sets the value associated with the declaration with the given id.
    /// 
    /// If the declaration does not exist in the current scope, the parent scopes will be searched for the declaration. 
    /// If the declaration is found in a parent scope, its value will be updated. 
    /// If the declaration is not found in the current scope or any parent scope, an exception will be thrown
    /// </summary>
    /// <param name="position">The position of the declaration being set.</param>
    /// <param name="id">The id of the declaration to set.</param>
    /// <param name="value">The value to associate with the declaration.</param>
    /// <exception cref="DeclarationNotDefinedException">
    /// If the declaration is not found in the current scope or any parent scope.
    /// </exception>
    public void SetDeclaration(Position position, TKey id, T value) {
      // Get the variable from the current scope
      if (Declarations.ContainsKey(id)) {
        Declarations[id] = value;
        return;
      }
      // Get the variable from the parent scope
      if (Parent != null) {
        Parent.SetDeclaration(position, id, value);
        return;
      }
      // Otherwise the variable does not exist in this scope or any parent scope
      throw new DeclarationNotDefinedException(position, id.ToString());
    }
    // An internal helper method for generating a string representation of the scope.
    private void ToStringHelp(StringBuilder sb, int indent = 0) {
      var indentStr = new string(' ', indent * 2);
      sb.AppendLine($"{indentStr}Scope:");
      foreach (var decl in Declarations) {
        sb.AppendLine($"{indentStr}  {decl.Key}: {decl.Value}");
      }
      if (Parent != null) Parent.ToStringHelp(sb, indent + 1);
    }
    public override string ToString() {
      var sb = new StringBuilder();
      ToStringHelp(sb);
      return sb.ToString();
    }
  }
}
