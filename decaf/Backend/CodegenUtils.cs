namespace Decaf.Backend {
  using System;
  using Decaf.IR.Signature;
  using Decaf.Utils;
  using Decaf.WasmBuilder;

  /// <summary>Helper methods for code generation.</summary>
  public static class CodegenUtils {
    private static string GetMemberName(string moduleName, string memberName) {
      return $"{moduleName}_{memberName}";
    }
    public static WasmLabel GetMemberLabel(Position position, string moduleName, string memberName) {
      return new WasmLabel.Label(position, GetMemberName(moduleName, memberName));
    }
    // We need to be able to refer to the runtime module so we can call its functions.
#nullable enable
    public class Runtime(Signature.ModuleSig? ModuleSig) {
      private static Symbol RuntimeModuleNotFound() => throw new Exception($"Runtime module `{RuntimeModuleName}` not found");

      public static readonly string RuntimeModuleName = "Runtime";
      // Allocation API
      public Symbol RuntimeMallocName => ModuleSig?.Resolutions["malloc"] ?? RuntimeModuleNotFound();
      public Symbol RuntimeCallocName => ModuleSig?.Resolutions["calloc"] ?? RuntimeModuleNotFound();
      // Allocation APIs
      public Symbol RuntimeAllocateArray => ModuleSig?.Resolutions["allocateArray"] ?? RuntimeModuleNotFound();
      public Symbol RuntimeAllocateString => ModuleSig?.Resolutions["allocateString"] ?? RuntimeModuleNotFound();
      // Equality
      public Symbol RuntimeStringEqual => ModuleSig?.Resolutions["stringEqual"] ?? RuntimeModuleNotFound();
      public Symbol RuntimeStringNotEqual => ModuleSig?.Resolutions["stringNotEqual"] ?? RuntimeModuleNotFound();
    }
#nullable restore
  }
}
