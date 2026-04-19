using System;
using System.Linq;

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
    public static class Runtime {
      public const string RuntimeModuleName = "Runtime";
      // Allocation API
      public static readonly string RuntimeMallocName = GetMemberName(RuntimeModuleName, "malloc");
      public static readonly string RuntimeCallocName = GetMemberName(RuntimeModuleName, "calloc");
      // Allocation APIs
      public static readonly string RuntimeAllocateArray = GetMemberName(RuntimeModuleName, "allocateArray");
      public static readonly string RuntimeAllocateString = GetMemberName(RuntimeModuleName, "allocateString");
    }
  }
}
