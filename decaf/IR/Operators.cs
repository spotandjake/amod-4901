namespace Decaf.IR.Operators {
  public enum PrefixOperator {
    Not,
    BitwiseNot
  }
  public enum BinaryOperator {
    // Arithmetic
    Add, Minus, Multiply, Divide,
    // Relational
    LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
    // Equality
    Equal, NotEqual,
    // Conditional
    And, Or,
    // Bitwise
    BitwiseAnd, BitwiseOr,
    BitwiseLeftShift, BitwiseRightShift
  }
  public static class OperatorConverter {
    public static PrefixOperator PrefixOperatorFromString(string op) => op switch {
      "!" => PrefixOperator.Not,
      "~" => PrefixOperator.BitwiseNot,
      // Unknown
      // NOTE: Parsing should have already failed if we encounter an unknown operator, so this is more of a sanity check than an expected error case (it would indicate that we forgot to update this method after adding a new operator)
      _ => throw new System.Exception($"Unknown prefix operator: {op}")
    };
    public static BinaryOperator BinaryOperatorFromString(string op) => op switch {
      // Arithmetic
      "+" => BinaryOperator.Add,
      "-" => BinaryOperator.Minus,
      "*" => BinaryOperator.Multiply,
      "/" => BinaryOperator.Divide,
      // Relational
      "<" => BinaryOperator.LessThan,
      "<=" => BinaryOperator.LessThanOrEqual,
      ">" => BinaryOperator.GreaterThan,
      ">=" => BinaryOperator.GreaterThanOrEqual,
      // Equality
      "==" => BinaryOperator.Equal,
      "!=" => BinaryOperator.NotEqual,
      // Conditional
      "&&" => BinaryOperator.And,
      "||" => BinaryOperator.Or,
      // Bitwise
      "&" => BinaryOperator.BitwiseAnd,
      "|" => BinaryOperator.BitwiseOr,
      "<<" => BinaryOperator.BitwiseLeftShift,
      ">>" => BinaryOperator.BitwiseRightShift,
      // Unknown
      // NOTE: Parsing should have already failed if we encounter an unknown operator, so this is more of a sanity check than an expected error case (it would indicate that we forgot to update this method after adding a new operator)
      _ => throw new System.Exception($"Unknown binary operator: {op}")
    };
  }
}
