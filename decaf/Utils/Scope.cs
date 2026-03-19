using System.Collections.Generic;
using System.Text;
using Decaf.Utils.Errors.ScopeErrors;

namespace Decaf.Utils {
  /// <summary>
  /// This class represents a scope.
  /// 
  /// Scopes follow a hierarchical structure, where each scope can have a parent scope. 
  /// This allows for nested scopes, such as those created by functions or blocks.
  /// 
  /// When looking up a declaration, the scope will check for the first occurrence of the declaration in the hierarchy.
  /// </summary>
  /// <typeparam name="T">The value to be tracked related to a declaration.</typeparam>
  /// <param name="parent">The parent scope.</param>
#nullable enable
  public class Scope<T>(Scope<T>? parent) {
    /// <summary>
    /// The parent scope, or null if this is the global scope.
    /// </summary>
    private Scope<T>? Parent { get; } = parent;
    /// <summary>
    /// The declarations in this scope, mapping from the declaration name to the value being tracked.
    /// </summary>
    private Dictionary<string, T> Declarations { get; } = [];
    /// <summary>
    /// Adds a declaration to the current scope. If a declaration with the same name already exists 
    /// in the current scope an exception will be thrown.
    /// 
    /// Note that this method does not check for declarations in parent scopes, 
    /// so it is possible to shadow declarations from parent scopes without an error.
    /// </summary>
    /// <param name="name">The name of the declaration to add.</param>
    /// <param name="value">The value to associate with the declaration.</param>
    /// <exception cref="DuplicateDeclarationException">
    /// If a declaration with the same name already exists in the current scope.
    /// </exception>
    public void AddDeclaration(Position position, string name, T value) {
      if (this.HasDeclaration(name, false)) throw new DuplicateDeclarationException(position, name);
      this.Declarations.Add(name, value);
    }
    /// <summary>
    /// Checks if a declaration with the given name exists in the current scope or any parent scope.
    /// </summary>
    /// <param name="name">The name of the declaration to check.</param>
    /// <param name="searchParent">Whether to search parent scopes.</param>
    /// <returns>`true` if the declaration exists, `false` otherwise.</returns>
    public bool HasDeclaration(string name, bool searchParent = true) {
      // Check if the declaration exists in the current scope
      if (this.Declarations.ContainsKey(name)) return true;
      // Check if the declaration exists in the parent scope (if enabled)
      if (searchParent && this.Parent != null && this.Parent.HasDeclaration(name)) return true;
      // Otherwise the declaration does not exist in this scope or any parent scope
      return false;
    }
    /// <summary>
    /// Gets the value associated with the declaration with the given name. If the declaration does not exist in the current scope,
    /// </summary>
    /// <param name="position">The position of the declaration access being searched.</param>
    /// <param name="name">The name of the declaration to get.</param>
    /// <returns>The value associated with the declaration.</returns>
    /// <exception cref="DeclarationNotDefinedException"></exception>
    public T GetDeclaration(Position position, string name) {
      // Get the variable from the current scope
      if (this.Declarations.ContainsKey(name)) return Declarations[name];
      // Get the variable from the parent scope
      if (this.Parent != null) return this.Parent.GetDeclaration(position, name);
      // Otherwise the variable does not exist in this scope or any parent scope
      throw new DeclarationNotDefinedException(position, name);
    }
    /// <summary>
    /// Sets the value associated with the declaration with the given name.
    /// 
    /// If the declaration does not exist in the current scope, the parent scopes will be searched for the declaration. 
    /// If the declaration is found in a parent scope, its value will be updated. 
    /// If the declaration is not found in the current scope or any parent scope, an exception will be thrown
    /// </summary>
    /// <param name="position">The position of the declaration being set.</param>
    /// <param name="name">The name of the declaration to set.</param>
    /// <param name="value">The value to associate with the declaration.</param>
    /// <exception cref="DeclarationNotDefinedException">
    /// If the declaration is not found in the current scope or any parent scope.
    /// </exception>
    public void SetDeclaration(Position position, string name, T value) {
      // Get the variable from the current scope
      if (Declarations.ContainsKey(name)) {
        Declarations[name] = value;
        return;
      }
      // Get the variable from the parent scope
      if (Parent != null) {
        Parent.SetDeclaration(position, name, value);
        return;
      }
      // Otherwise the variable does not exist in this scope or any parent scope
      throw new DeclarationNotDefinedException(position, name);
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
