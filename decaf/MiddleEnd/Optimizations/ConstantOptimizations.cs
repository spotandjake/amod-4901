namespace Decaf.MiddleEnd.Optimizations {
  using System.Collections.Generic;
  using System.Linq;
  using Decaf.IR.Operators;
  using Decaf.Utils;
  using AnfTree = Decaf.IR.AnfTree;
  /// <summary>
  /// This class is responsible for performing constant folding and constant 
  /// propagation optimizations on the ANF tree.
  ///
  /// A great reading for additional context: 
  /// https://en.wikipedia.org/wiki/Constant_folding#:~:text=Constant%20propagation%20is%20the%20process,(28%20%2F%20x%20%2B%202)%3B 
  /// 
  /// Examples:
  /// * Binops should be folded as follows:
  ///   * x = 1 + 1 -> x = 2 
  ///   * if(1 == 1) -> if(true)
  ///   * if(1 <= 2) -> if(true)
  /// * Prefix folding:
  ///   * !true -> false
  ///   * ~5(00000101) -> -6
  /// * Constant propagation:
  ///   * let x: int = 5;
  ///   * print(x); -> print(5);
  /// </summary>
  /// 
  internal static class ConstantOptimization {
    private record struct Context {
      // Map of symbol to constant value for all variables that have known constant values.
      public Dictionary<Symbol, AnfTree.ImmediateNode.ConstantNode> Constants { get; init; }
      // Whether the current instruction is guaranteed to be executed (i.e. not in a branch that may not be taken).
      public bool IsSureToBeExecuted { get; init; }
      // Whether the current instruction is in a loop, this is important to track for constant propagation as 
      // we don't want to propagate constants into loops since they may not actually be constant across iterations.
      public bool IsInLoop { get; init; }
    }
    private static bool IsOptimizableConstant(AnfTree.ImmediateNode.ConstantNode constant) {
      // Currently we only fold integer and character constants, but this can be easily extended to other types as well.
      return constant.Value is AnfTree.LiteralNode.IntegerNode or AnfTree.LiteralNode.CharacterNode or AnfTree.LiteralNode.BooleanNode;
    }
    public static AnfTree.ProgramNode Optimize(AnfTree.ProgramNode node) {
      var ctx = new Context {
        Constants = [],
        IsSureToBeExecuted = true,
        IsInLoop = false
      };
      // Visit all the modules in the program and optimize them
      var optimizedModules = node.Modules.Select(m => OptimizeModule(ctx, m)).ToArray();
      // Rebuild the node with the optimizations applied
      return new AnfTree.ProgramNode(node.Position, optimizedModules);
    }
    private static AnfTree.ModuleNode OptimizeModule(Context ctx, AnfTree.ModuleNode node) {
      // Optimize all the functions in the module
      var optimizedFunctions = new List<AnfTree.FunctionNode>();
      foreach (var func in node.Functions) {
        // NOTE: Because functions are removed from the regular control flow and can be called from multiple places we need to be 
        // conservative with optimizations inside functions, this means we can't assume the 
        // relationship between functions or with globals.
        var functionCtx = new Context {
          Constants = [],
          IsSureToBeExecuted = true,
          IsInLoop = false
        };
        // Optimize the body of the function
        var optimizedBody = OptimizeBlockInstruction(functionCtx, func.Body);
        // Rebuild the function with the optimizations applied
        optimizedFunctions.Add(new AnfTree.FunctionNode(
          func.Position,
          func.ID,
          func.Parameters,
          optimizedBody,
          func.Signature
        ));
      }
      // Optimize the module body as well
      var moduleCtx = new Context {
        Constants = [],
        IsSureToBeExecuted = true,
        IsInLoop = false
      };
      var optimizedModuleBody = OptimizeBlockInstruction(moduleCtx, node.Body);
      // Rebuild the node with the optimizations applied
      return new AnfTree.ModuleNode(
        node.Position,
        node.ID,
        node.Imports,
        optimizedFunctions.ToArray(),
        optimizedModuleBody,
        node.Signature
      );
    }
    // --- Instructions ---
    private static AnfTree.InstructionNode OptimizeInstruction(Context ctx, AnfTree.InstructionNode node) {
      switch (node) {
        case AnfTree.InstructionNode.BlockNode blockNode:
          return OptimizeBlockInstruction(ctx, blockNode);
        // If we hit a bind node optimize it's simple expression.
        case AnfTree.InstructionNode.BindNode bindNode:
          return OptimizeBindInstruction(ctx, bindNode);
        // Check the if node true and false branches (false node may not always be present).
        case AnfTree.InstructionNode.IfNode ifNode: {
            var newCtx = ctx with { IsSureToBeExecuted = false };
            var optimizedTrue = OptimizeInstruction(newCtx, ifNode.TrueBranch);
            var optimizedFalse = ifNode.FalseBranch != null ? OptimizeInstruction(newCtx, ifNode.FalseBranch) : null;
            return new AnfTree.InstructionNode.IfNode(ifNode.Position, ifNode.Condition, optimizedTrue, optimizedFalse);
          }
        // visit the body of loop nodes as well.
        case AnfTree.InstructionNode.LoopNode loopNode: {
            var newCtx = ctx with { IsInLoop = true };
            var optimizedBody = OptimizeInstruction(newCtx, loopNode.Body);
            return new AnfTree.InstructionNode.LoopNode(loopNode.Position, optimizedBody);
          }
        // For most nodes we just optimize the immediate directly
        case AnfTree.InstructionNode.AssignmentNode assignmentNode:
          return OptimizeAssignmentInstruction(ctx, assignmentNode);
        case AnfTree.InstructionNode.ReturnNode returnNode: {
            var optimizedReturnImm = OptimizeImmediate(ctx, returnNode.Value);
            return new AnfTree.InstructionNode.ReturnNode(returnNode.Position, optimizedReturnImm);
          }
        case AnfTree.InstructionNode.SimpleExprInstructionNode simpleExprNode: {
            var optimizedSimpleExprNode = OptimizeSimpleExpressionNode(ctx, simpleExprNode.Expr);
            return new AnfTree.InstructionNode.SimpleExprInstructionNode(simpleExprNode.Position, optimizedSimpleExprNode);
          }
        // by default it's not important to optimize the instruction, return as is.
        default: return node;
      }
    }
    private static AnfTree.InstructionNode.BlockNode OptimizeBlockInstruction(Context ctx, AnfTree.InstructionNode.BlockNode node) {
      var optimizedInstructions = new List<AnfTree.InstructionNode>();
      foreach (var instr in node.Instructions) {
        var optimizedInstrs = OptimizeInstruction(ctx, instr);
        optimizedInstructions.Add(optimizedInstrs);
      }
      // Rebuild the node with the optimizations applied
      return new AnfTree.InstructionNode.BlockNode(node.Position, optimizedInstructions.ToArray());
    }
    private static AnfTree.InstructionNode.BindNode OptimizeBindInstruction(Context ctx, AnfTree.InstructionNode.BindNode node) {
      var optimizedSimpleExpr = OptimizeSimpleExpressionNode(ctx, node.SimpleExpression);
      if (
        !ctx.IsInLoop &&
        optimizedSimpleExpr is AnfTree.SimpleExpressionNode.ImmediateExpressionNode {
          Imm: AnfTree.ImmediateNode.ConstantNode constantValue
        } &&
        IsOptimizableConstant(constantValue)
      ) ctx.Constants.Add(node.ID, constantValue);
      return new AnfTree.InstructionNode.BindNode(node.Position, node.ID, optimizedSimpleExpr);
    }
    private static AnfTree.InstructionNode.AssignmentNode OptimizeAssignmentInstruction(
      Context ctx,
      AnfTree.InstructionNode.AssignmentNode node
    ) {
      if ((ctx.IsInLoop || !ctx.IsSureToBeExecuted) && node.Location is AnfTree.LocationNode.SymbolLocation symLoc) {
        // If we are in a loop we can't be sure that the value is actually constant across iterations, so we shouldn't propagate it.
        ctx.Constants.Remove(symLoc.ID);
      }
      var optimizedImmediate = OptimizeImmediate(ctx, node.Imm);
      if (
        !ctx.IsInLoop &&
        ctx.IsSureToBeExecuted &&
        optimizedImmediate is AnfTree.ImmediateNode.ConstantNode constantValue &&
        IsOptimizableConstant(constantValue) &&
        node.Location is AnfTree.LocationNode.SymbolLocation symbolLocation
      ) ctx.Constants.Add(symbolLocation.ID, constantValue);
      return new AnfTree.InstructionNode.AssignmentNode(node.Position, node.Location, optimizedImmediate);
    }
    // --- Simple Expressions ---
    private static AnfTree.SimpleExpressionNode OptimizeSimpleExpressionNode(Context ctx, AnfTree.SimpleExpressionNode simpleExpr) {
      return simpleExpr switch {
        // Constant folding for binop and prefix nodes
        AnfTree.SimpleExpressionNode.BinopNode binop => OptimizeBinopNode(ctx, binop),
        AnfTree.SimpleExpressionNode.PrefixNode prefix => OptimizePrefixNode(ctx, prefix),
        // For most other nodes we just optimize the immediate directly and rebuild the node with the optimized immediate.
        AnfTree.SimpleExpressionNode.CallNode callNode =>
          new AnfTree.SimpleExpressionNode.CallNode(
            callNode.Position,
            callNode.Callee,
            callNode.Arguments.Select(arg => OptimizeImmediate(ctx, arg)).ToArray(),
            callNode.ExpressionType
          ),
        AnfTree.SimpleExpressionNode.PrimCallNode primCallNode =>
          new AnfTree.SimpleExpressionNode.PrimCallNode(
            primCallNode.Position,
            primCallNode.Callee,
            primCallNode.Arguments.Select(arg => OptimizeImmediate(ctx, arg)).ToArray(),
            primCallNode.ExpressionType
          ),
        AnfTree.SimpleExpressionNode.ArrayInitNode arrayInitNode =>
          new AnfTree.SimpleExpressionNode.ArrayInitNode(
            arrayInitNode.Position,
            OptimizeImmediate(ctx, arrayInitNode.SizeImm),
            arrayInitNode.ExpressionType
          ),
        AnfTree.SimpleExpressionNode.ImmediateExpressionNode immediateNode =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            immediateNode.Position,
            OptimizeImmediate(ctx, immediateNode.Imm),
            immediateNode.ExpressionType
          ),
        // Unknown, this may be something we don't need to visit or haven't implemented yet, return as is.
        _ => simpleExpr,
      };
    }

    private static AnfTree.SimpleExpressionNode OptimizeBinopNode(Context ctx, AnfTree.SimpleExpressionNode.BinopNode binop) {
      // Optimize both operands first before attempting to fold
      var optimizedLhs = OptimizeImmediate(ctx, binop.Lhs);
      var optimizedRhs = OptimizeImmediate(ctx, binop.Rhs);
      // Switch expression match on all binop operators and both operand types simultaneously.
      // If both sides are int constants fold at compile time.
      // Otherwise rebuild the binop with the optimized operands.
      return (binop.Operator, optimizedLhs, optimizedRhs) switch {
        // Fold on addition
        (
          BinaryOperator.Add,
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType }
          },
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 }
          }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 + val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on subtraction
        (
          BinaryOperator.Minus,
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType }
          },
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 }
          }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 - val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on multiplication
        // two constants, then 0*x, then x*0
        (
          BinaryOperator.Multiply,
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType }
          },
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 }
          }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 * val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        (
          BinaryOperator.Multiply,
          AnfTree.ImmediateNode.ConstantNode {
            Value: AnfTree.LiteralNode.IntegerNode { Value: 0, LiteralType: var litType }
          },
          _
       ) =>
        new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
          binop.Position,
          new AnfTree.ImmediateNode.ConstantNode(
            binop.Position,
            // Optimization is 0 * x = 0, we can return 0 of the correct literal type without needing to evaluate x at all.
            new AnfTree.LiteralNode.IntegerNode(binop.Position, 0, litType),
            binop.ExpressionType
          ),
          binop.ExpressionType
        ),
        (
          BinaryOperator.Multiply,
          _,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: 0, LiteralType: var litType } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              // Optimization is 0 * x = 0, we can return 0 of the correct literal type without needing to evaluate x at all.
              new AnfTree.LiteralNode.IntegerNode(binop.Position, 0, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on division 
        (
          BinaryOperator.Divide,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) when val2 != 0 =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 / val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Division by self optimization, x / x = 1 no need to evaluate x at all, just return 1 of the correct literal type.
        // NOTE: This does break x / x when x is 0, but we allow this optimization because it is still a net win in terms of perf and 
        //       the user can always disable this optimization if they want to preserve the division by zero behavior.
        (
          BinaryOperator.Divide,
          AnfTree.ImmediateNode.LocationImmNode { Location: AnfTree.LocationNode.SymbolLocation { ID: var sym1 } },
          AnfTree.ImmediateNode.LocationImmNode { Location: AnfTree.LocationNode.SymbolLocation { ID: var sym2 } }
        ) when sym1 == sym2 =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, 1, binop.ExpressionType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on equality (Int)
        (
          BinaryOperator.Equal,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 == val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on equality (Char)
        (
          BinaryOperator.Equal,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.CharacterNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.CharacterNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 == val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on not equal operator (Int)
        (
          BinaryOperator.NotEqual,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 != val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on not equal operator (Char)
        (
          BinaryOperator.NotEqual,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.CharacterNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.CharacterNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 != val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on greater than operator
        (
          BinaryOperator.GreaterThan,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 > val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on less than operator
        (
          BinaryOperator.LessThan,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 < val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on greater than or equal operator
        (
          BinaryOperator.GreaterThanOrEqual,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 >= val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on less than or equal operator
        (
          BinaryOperator.LessThanOrEqual,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 <= val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on bitwise and operator
        (
          BinaryOperator.BitwiseAnd,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 & val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on bitwise or operator
        (
          BinaryOperator.BitwiseOr,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 | val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on bitwise left shift operator
        (
          BinaryOperator.BitwiseLeftShift,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 << val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on bitwise right shift operator
        (
          BinaryOperator.BitwiseRightShift,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.IntegerNode(binop.Position, val1 >> val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on conditional 'and' operator
        (
          BinaryOperator.And,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 && val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // Fold on conditional 'or' operator
        (
          BinaryOperator.Or,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: var val1, LiteralType: var litType } },
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: var val2 } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            binop.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              binop.Position,
              new AnfTree.LiteralNode.BooleanNode(binop.Position, val1 || val2, litType),
              binop.ExpressionType
            ),
            binop.ExpressionType
          ),
        // No match can't fold rebuild with the optimized operands
        _ => new AnfTree.SimpleExpressionNode.BinopNode(binop.Position, optimizedLhs, binop.Operator, optimizedRhs, binop.ExpressionType)
      };
    }

    private static AnfTree.SimpleExpressionNode OptimizePrefixNode(Context ctx, AnfTree.SimpleExpressionNode.PrefixNode prefix) {
      // Optimize the operand first before attempting to fold
      var optimizedOperand = OptimizeImmediate(ctx, prefix.Operand);

      // If the operand is a boolean constant fold at compile time.
      // Otherwise rebuild
      return (prefix.Operator, optimizedOperand) switch {
        // Fold on the 'not' operator
        (
          PrefixOperator.Not,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.BooleanNode { Value: var val, LiteralType: var litType } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            prefix.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              prefix.Position,
              new AnfTree.LiteralNode.BooleanNode(prefix.Position, !val, litType),
              prefix.ExpressionType
            ),
            prefix.ExpressionType
          ),
        // Fold on the bitwise 'not' operator
        (
          PrefixOperator.BitwiseNot,
          AnfTree.ImmediateNode.ConstantNode { Value: AnfTree.LiteralNode.IntegerNode { Value: var val, LiteralType: var litType } }
        ) =>
          new AnfTree.SimpleExpressionNode.ImmediateExpressionNode(
            prefix.Position,
            new AnfTree.ImmediateNode.ConstantNode(
              prefix.Position,
              new AnfTree.LiteralNode.IntegerNode(prefix.Position, ~val, litType),
              prefix.ExpressionType
            ),
            prefix.ExpressionType
          ),
        // No match means no fold, rebuild with optimized operand
        _ => new AnfTree.SimpleExpressionNode.PrefixNode(prefix.Position, prefix.Operator, optimizedOperand, prefix.ExpressionType)
      };
    }

    // --- Immediate Nodes ---
    private static AnfTree.ImmediateNode OptimizeImmediate(Context ctx, AnfTree.ImmediateNode immediate) {
      // If the immediate is a location and that location is in our constant map, replace it with the constant value.
      if (
        !ctx.IsInLoop &&
        immediate is AnfTree.ImmediateNode.LocationImmNode locImm &&
        locImm.Location is AnfTree.LocationNode.SymbolLocation symLoc &&
        ctx.Constants.TryGetValue(symLoc.ID, out var constantValue) &&
        IsOptimizableConstant(constantValue)
      ) return constantValue;
      return immediate;
    }
  }
}


