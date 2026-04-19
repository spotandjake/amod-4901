using System.Collections.Generic;

namespace Decaf.Utils {
  public record struct CompilationConfig {
    // Global config
    public bool UseStartSection;
    // Related to modules
    public bool BundleRuntime;
    public List<string> SkipOptimizationPasses;
  }
};
