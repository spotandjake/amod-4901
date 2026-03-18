using System;
using System.Collections.Generic;
using System.Linq;

using Decaf.IR.ParseTree;
using ParseTree = Decaf.IR.ParseTree;
using Decaf.Utils;

namespace Decaf.Frontend {
  /// <summary>
  /// The ParseTreeMapper exists to map ANTLR parser contexts to our own internal parse tree representation.
  /// 
  /// The exact representation of the parse tree can be found in `./decaf/IR/ParseTree.cs`.
  /// 
  /// The mapping process itself is pretty straight forward we visit each node in the ANTLR parse tree and 
  /// create the corresponding parse tree node in our internal representation.
  /// </summary>
  public static class ParseTreeMapper {
    /// <summary>
    /// Maps a `DecafParser.ProgramContext` to a `ProgramNode` in our internal parse tree representation.
    /// </summary>
    /// <param name="context">The ANTLR parser context for the program node.</param>
    /// <returns>The corresponding `ProgramNode` in our internal parse tree representation.</returns>
    public static ProgramNode MapProgramContext(DecafParser.ProgramContext context) {
      var position = MapPositionContext(context);
      var classes = context.class_decl().Select(MapClassDeclContext).ToArray();
      return new ProgramNode(position, classes, null);
    }
    // Internal Helpers
    private static Position MapPositionContext(Antlr4.Runtime.ParserRuleContext context) {
      return new Position {
        fileName = context.Start.InputStream.SourceName,
        line = context.Start.Line,
        column = context.Start.Column,
        offset = context.Start.StartIndex
      };
    }
    // Declarations
    private static DeclarationNode.ClassNode MapClassDeclContext(DecafParser.Class_declContext context) {
      var position = MapPositionContext(context);
      var name = context.name.Text;
      var superClassName = context.superClassName?.Text;
      var varDecls = context.var_decl().Select(MapVarDeclContext).ToArray();
      var methodDecls = context.method_decl().Select(MapMethodDeclContext).ToArray();
      return new DeclarationNode.ClassNode(position, name, superClassName, varDecls, methodDecls, null);
    }
    private static DeclarationNode.VariableNode MapVarDeclContext(DecafParser.Var_declContext context) {
      var position = MapPositionContext(context);
      var typ = MapTypeContext(context.typ);
      var binds = context.binds.var_bind().Select(
        varBindCtx => {
          var position = MapPositionContext(varBindCtx);
          var name = varBindCtx.name.Text;
          var isArray = varBindCtx.LBRACK() != null && varBindCtx.RBRACK() != null;
          return new DeclarationNode.VariableNode.BindNode(position, name, isArray);
        }
      ).ToArray();
      return new DeclarationNode.VariableNode(position, typ, binds);
    }
    private static DeclarationNode.MethodNode MapMethodDeclContext(DecafParser.Method_declContext context) {
      var position = MapPositionContext(context);
      var returnType = MapTypeContext(context.returnType);
      var name = context.name.Text;
      var parameters = context.parameters != null ? context.parameters.method_decl_param().Select(
        paramCtx => {
          var position = MapPositionContext(paramCtx);
          var type = MapTypeContext(paramCtx.typ);
          var name = paramCtx.name.Text;
          var isArray = paramCtx.LBRACK() != null && paramCtx.RBRACK() != null;
          return new DeclarationNode.MethodNode.ParameterNode(position, type, name, isArray);
        }
      ).ToArray() : [];
      var body = MapBlockContext(context.body);
      return new DeclarationNode.MethodNode(position, returnType, name, parameters, body, null);
    }
    // General
    private static BlockNode MapBlockContext(DecafParser.BlockContext context) {
      var position = MapPositionContext(context);
      var varDecls = context.var_decl().Select(MapVarDeclContext).ToArray();
      var statements = context.statement().Select(MapStatementContext).ToArray();
      return new BlockNode(position, varDecls, statements, null);
    }
    private static TypeNode MapTypeContext(DecafParser.TypeContext context) {
      var position = MapPositionContext(context);
      var content = context.GetText();
      var type = context switch {
        DecafParser.VoidTypeContext _ => PrimitiveType.Void,
        DecafParser.IntTypeContext _ => PrimitiveType.Int,
        DecafParser.BooleanTypeContext _ => PrimitiveType.Boolean,
        DecafParser.CustomTypeContext _ => PrimitiveType.Custom,
        // NOTE: This should be impossible due to grammar restrictions
        _ => throw new InvalidProgramException("Impossible type at TypeNode.FromContext")
      };
      return new TypeNode(position, type, content);
    }
    // Statements
    private static StatementNode MapStatementContext(DecafParser.StatementContext context) {
      return context switch {
        DecafParser.AssignStatementContext assignCtx => MapAssignmentStatementNode(assignCtx.assign_stmt()),
        DecafParser.ExpressionStatementContext exprStmtCtx => MapExprStatementNode(exprStmtCtx.expression_stmt()),
        DecafParser.IfStatementContext ifCtx => MapIfStatementNode(ifCtx.if_stmt()),
        DecafParser.WhileStatementContext whileCtx => MapWhileStatementNode(whileCtx.while_stmt()),
        DecafParser.ReturnStatementContext returnCtx => MapReturnStatementNode(returnCtx.return_stmt()),
        // NOTE: This should be impossible due to grammar restrictions
        _ => throw new InvalidProgramException("Impossible statement at StatementNode.FromContext"),
      };
    }
    private static StatementNode.AssignmentNode MapAssignmentStatementNode(DecafParser.Assign_stmtContext context) {
      var position = MapPositionContext(context);
      var location = MapLocationExpressionContext(context.location());
      var expression = MapExpressionContext(context.expr());
      return new StatementNode.AssignmentNode(position, location, expression);
    }
    private static StatementNode.ExprNode MapExprStatementNode(DecafParser.Expression_stmtContext context) {
      var position = MapPositionContext(context);
      switch (context) {
        case DecafParser.CallExpressionStatementContext callExprStmtCtx:
          var callCtx = callExprStmtCtx.call_expr();
          var content = callCtx switch {
            DecafParser.MethodCallExprContext m => MapCallExpressionContext(m.method_call()),
            DecafParser.PrimCalloutExprContext p => MapPrimitiveCallExpressionContext(p.prim_callout()),
            // NOTE: This should be impossible due to grammar restrictions
            _ => throw new InvalidProgramException("Impossible statement at ExpressionStatementNode.FromContext"),
          };
          return new StatementNode.ExprNode(position, content);
        default:
          // NOTE: This should be impossible due to grammar restrictions
          throw new InvalidProgramException("Impossible statement at ExpressionStatementNode.FromContext");
      }
    }
    private static StatementNode.IfNode MapIfStatementNode(DecafParser.If_stmtContext context) {
      var position = MapPositionContext(context);
      var condition = MapExpressionContext(context.condition);
      var trueBranch = MapBlockContext(context.trueBranch);
      var falseBranch = context.falseBranch != null ? MapBlockContext(context.falseBranch) : null;
      return new StatementNode.IfNode(position, condition, trueBranch, falseBranch);
    }
    private static StatementNode.WhileNode MapWhileStatementNode(DecafParser.While_stmtContext context) {
      var position = MapPositionContext(context);
      var condition = MapExpressionContext(context.condition);
      var body = MapBlockContext(context.body);
      return new StatementNode.WhileNode(position, condition, body);
    }
    private static StatementNode.ReturnNode MapReturnStatementNode(DecafParser.Return_stmtContext context) {
      var position = MapPositionContext(context);
      var value = context.value != null ? MapExpressionContext(context.value) : null;
      return new StatementNode.ReturnNode(position, value);
    }
    // Expressions
    private static ExpressionNode MapExpressionContext(DecafParser.ExprContext context) {
      return context switch {
        DecafParser.SimpleExprContext simpleExprCtx => simpleExprCtx.simple_expr() switch {
          DecafParser.LocationExprContext locationCtx => MapLocationExpressionContext(locationCtx.location()),
          DecafParser.ThisExprContext thisCtx => MapThisExpressionContext(thisCtx),
          DecafParser.CallExprContext callCtx => callCtx.call_expr() switch {
            DecafParser.MethodCallExprContext methodCallCtx => MapCallExpressionContext(methodCallCtx.method_call()),
            DecafParser.PrimCalloutExprContext primCalloutCtx => MapPrimitiveCallExpressionContext(primCalloutCtx.prim_callout()),
            // NOTE: This should be impossible due to grammar restrictions
            _ => throw new InvalidProgramException("Impossible call expression at ExpressionNode.FromContext"),
          },
          // NOTE: This should be impossible due to grammar restrictions
          _ => throw new InvalidProgramException("Impossible expression at ExpressionNode.FromContext"),
        },
        DecafParser.NewObjectExprContext newObjExprCtx => MapNewClassExpressionContext(newObjExprCtx),
        DecafParser.NewArrayExprContext newArrExprCtx => MapNewArrayExpressionContext(newArrExprCtx),
        DecafParser.LiteralExprContext literalExprCtx => MapLiteralExpressionContext(literalExprCtx.literal()),
        DecafParser.BinaryOpExprContext binopExprCtx => MapBinopExpressionContext(binopExprCtx),
        DecafParser.NotExprContext prefixExprCtx => MapPrefixExpressionContext(prefixExprCtx),
        DecafParser.ParenExprContext parenExprCtx => MapExpressionContext(parenExprCtx.expr()),
        // NOTE: This should be impossible due to grammar restrictions
        _ => throw new InvalidProgramException("Impossible expression at ExpressionNode.FromContext"),
      };
    }
    private static ExpressionNode.CallNode MapCallExpressionContext(DecafParser.Method_callContext context) {
      var position = MapPositionContext(context);
      var path = MapLocationExpressionContext(context.methodPath);
      var args = context.args != null ? context.args.expr().Select(MapExpressionContext).ToArray() : [];
      return new ExpressionNode.CallNode(position, false, path, args);
    }
    private static ExpressionNode.CallNode MapPrimitiveCallExpressionContext(DecafParser.Prim_calloutContext context) {
      var position = MapPositionContext(context);
      // Create a fake location node
      var path = new ExpressionNode.LocationNode(
        position,
        new ExpressionNode.IdentifierNode(position, context.primId.Text),
        null,
        null
      );
      // Map the arguments
      var args = new List<ExpressionNode>();
      foreach (var child in context.args.children) {
        switch (child) {
          case DecafParser.ExprContext exprCtx:
            args.Add(MapExpressionContext(exprCtx));
            break;
          case Antlr4.Runtime.Tree.ITerminalNode strLitCtx:
            // Because strings are just a terminal we need to double check we are not seeing the `,` token which is also a terminal
            if (strLitCtx.Symbol.Type != DecafParser.STRINGLIT) continue;
            var txt = strLitCtx.GetText();
            // Remove the quotes from the string literal
            args.Add(new ExpressionNode.LiteralNode(
              position,
              new ParseTree.LiteralNodes.StringNode(position, txt[1..^1])
            ));
            break;
          default:
            throw new InvalidProgramException("Impossible argument at PrimitiveCallNode.FromContext");
        }
      }
      return new ExpressionNode.CallNode(position, true, path, args.ToArray());
    }
    private static ExpressionNode.BinopNode MapBinopExpressionContext(DecafParser.BinaryOpExprContext context) {
      var position = MapPositionContext(context);
      var lhs = MapExpressionContext(context.lhs);
      var op = context.op.GetText();
      var rhs = MapExpressionContext(context.rhs);
      return new ExpressionNode.BinopNode(position, lhs, op, rhs);
    }
    private static ExpressionNode.PrefixNode MapPrefixExpressionContext(DecafParser.NotExprContext context) {
      var position = MapPositionContext(context);
      var op = context.op.Text;
      var operand = MapExpressionContext(context.operand);
      return new ExpressionNode.PrefixNode(position, op, operand);
    }
    private static ExpressionNode.LocationNode MapLocationExpressionContext(DecafParser.LocationContext context) {
      var position = MapPositionContext(context);
      var root = new ExpressionNode.IdentifierNode(position, context.root.Text);
      var path = context.path?.ID().GetText();
      var indexExpr = context.indexExpr != null ? MapExpressionContext(context.indexExpr.expr()) : null;
      return new ExpressionNode.LocationNode(position, root, path, indexExpr);
    }
    private static ExpressionNode.ThisNode MapThisExpressionContext(DecafParser.ThisExprContext context) {
      var position = MapPositionContext(context);
      return new ExpressionNode.ThisNode(position);
    }
    private static ExpressionNode.NewClassNode MapNewClassExpressionContext(DecafParser.NewObjectExprContext context) {
      var position = MapPositionContext(context);
      var className = context.ID().GetText();
      var identifier = new ExpressionNode.LocationNode(
        position,
        new ExpressionNode.IdentifierNode(position, className),
        null,
        null
      );
      return new ExpressionNode.NewClassNode(position, identifier);
    }
    private static ExpressionNode.NewArrayNode MapNewArrayExpressionContext(DecafParser.NewArrayExprContext context) {
      var position = MapPositionContext(context);
      var identifier = MapTypeContext(context.type());
      var sizeExpr = MapExpressionContext(context.expr());
      return new ExpressionNode.NewArrayNode(position, identifier, sizeExpr);
    }
    private static ExpressionNode.LiteralNode MapLiteralExpressionContext(DecafParser.LiteralContext context) {
      var position = MapPositionContext(context);
      LiteralNode literal = context switch {
        DecafParser.IntLitContext intLitCtx =>
          new ParseTree.LiteralNodes.IntegerNode(position, int.Parse(intLitCtx.INTLIT().GetText())),
        DecafParser.CharLitContext charLitCtx =>
          new ParseTree.LiteralNodes.CharacterNode(position, charLitCtx.CHARLIT().GetText()[1]),
        DecafParser.BoolLitContext boolLitCtx =>
          new ParseTree.LiteralNodes.BooleanNode(position, boolLitCtx.bool_literal().TRUE() != null),
        DecafParser.NullLitContext _ => new ParseTree.LiteralNodes.NullNode(position),
        _ => throw new InvalidProgramException("Impossible literal at LiteralNode.FromContext"),// NOTE: This should be impossible due to grammar restrictions
      };
      return new ExpressionNode.LiteralNode(position, literal);
    }
  }
}
