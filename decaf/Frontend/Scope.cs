using System;
using System.Collections.Generic;
using System.Text;

namespace ParseTree {
  // TODO: Attach position to the exception information
  // An exception thrown when a duplicate declaration is encountered during scope mapping
  public class DuplicateDeclarationException(string message) : Exception(message) { }
  // An exception thrown when a declaration is not found during scope lookup
  public class DeclarationNotDefinedException(string message) : Exception(message) { }
#nullable enable
  public class Scope<T>(Scope<T>? parent) {
#nullable enable
    public Scope<T>? Parent { get; } = parent;
    public Dictionary<string, T> Declarations { get; } = [];

    // Adds a variable to the scope, throwing an exception if it already exists in the current scope
    public void AddVariable(string name, T value) {
      if (this.HasVariable(name, false)) {
        throw new DuplicateDeclarationException($"Declaration already exists: {name}");
      }
      Declarations.Add(name, value);
    }
    public bool HasVariable(string name, bool checkParent = true) {
      // Check if the variable exists in the current scope
      if (Declarations.ContainsKey(name)) return true;
      // Check if the variable exists in the parent scope (if enabled)
      if (checkParent && Parent != null && Parent.HasVariable(name)) return true;
      // Otherwise the variable does not exist in this scope or any parent scope
      return false;
    }
    public T GetVariable(string name) {
      // Get the variable from the current scope
      if (Declarations.ContainsKey(name)) return Declarations[name];
      // Get the variable from the parent scope
      if (Parent != null) {
        try {
          return Parent.GetVariable(name);
        }
        catch (DeclarationNotDefinedException) {
          // We don't really care about the exception here
        }
      }
      // Otherwise the variable does not exist in this scope or any parent scope
      throw new DeclarationNotDefinedException($"Declaration not found: {name}");
    }
    public void SetVariable(string name, T value) {
      // Get the variable from the current scope
      if (Declarations.ContainsKey(name)) {
        Declarations[name] = value;
        return;
      }
      // Get the variable from the parent scope
      if (Parent != null) {
        try {
          Parent.SetVariable(name, value);
          return;
        }
        catch (DeclarationNotDefinedException) {
          // We don't really care about the exception here
        }
      }
      // Otherwise the variable does not exist in this scope or any parent scope
      throw new DeclarationNotDefinedException($"Declaration not found: {name}");
    }
    private void ToStringHelp(StringBuilder sb, int indent = 0) {
      var indentStr = new string(' ', indent * 2);
      sb.AppendLine($"{indentStr}Scope:");
      foreach (var decl in Declarations) {
        sb.AppendLine($"{indentStr}  {decl.Key}: {decl.Value}");
      }
      if (Parent != null) {
        Parent.ToStringHelp(sb, indent + 1);
      }
    }
    public override string ToString() {
      var sb = new StringBuilder();
      ToStringHelp(sb);
      return sb.ToString();
    }
  }
}
