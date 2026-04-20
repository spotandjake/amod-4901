using Decaf.IR.Signature;
using Decaf.Utils;
using Decaf.WasmBuilder;

namespace Decaf.Backend {
  /// <summary>Helper methods for code generation.</summary>
  public static class CodegenUtils {
    private static string GetMemberName(string moduleName, string memberName) {
      return $"{moduleName}_{memberName}";
    }
    public static WasmLabel GetMemberLabel(Position position, string moduleName, string memberName) {
      return new WasmLabel.Label(position, GetMemberName(moduleName, memberName));
    }
    // We need to be able to refer to the runtime module so we can call its functions.
    public class Runtime(Signature.ModuleSig ModuleSig) {
      public static readonly string RuntimeModuleName = "Runtime";
      // Allocation API
      public readonly Symbol RuntimeMallocName = ModuleSig.Resolutions["malloc"];
      public readonly Symbol RuntimeCallocName = ModuleSig.Resolutions["calloc"];
      // Allocation APIs
      public readonly Symbol RuntimeAllocateArray = ModuleSig.Resolutions["allocateArray"];
      public readonly Symbol RuntimeAllocateString = ModuleSig.Resolutions["allocateString"];
      // Equality
      public readonly Symbol RuntimeStringEqual = ModuleSig.Resolutions["stringEqual"];
    }
  }
}
