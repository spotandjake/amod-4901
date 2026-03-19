namespace Decaf.Utils {
  /// <summary>
  /// This struct represents a position within a source file.
  /// </summary>
  public struct Position {
    /// <summary>
    /// The name of the source file.
    /// </summary>
#nullable enable
    public required string? fileName;
    /// <summary>
    /// The line number within the source file (1-based).
    /// </summary>
    public required int line;
    /// <summary>
    /// The column number within the source file (0-based).
    /// </summary>
    public required int column;
    /// <summary>
    /// The offset from the beginning of the file (0-based).
    /// </summary>
    public required int offset;
  }
}
