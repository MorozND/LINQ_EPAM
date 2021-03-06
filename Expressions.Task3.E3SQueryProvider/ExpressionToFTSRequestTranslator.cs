using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            var memberExpression = node.Object as MemberExpression;

            if (memberExpression is null)
                throw new NotSupportedException("MemberExpression is required for MethodCall");

            switch (node.Method.Name)
            {
                case "Equals":
                    Visit(memberExpression);
                    _resultStringBuilder.Append("(");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(")");

                    return node;

                case "StartsWith":
                    Visit(memberExpression);
                    _resultStringBuilder.Append("(");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append("*)");

                    return node;

                case "Contains":
                    Visit(memberExpression);
                    _resultStringBuilder.Append("(*");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append("*)");

                    return node;

                case "EndsWith":
                    Visit(memberExpression);
                    _resultStringBuilder.Append("(*");
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(")");

                    return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    Expression memberAccess = null;
                    Expression constant = null;

                    switch (node.Left.NodeType)
                    {
                        case ExpressionType.MemberAccess:
                            memberAccess = node.Left;
                            break;

                        case ExpressionType.Constant:
                            constant = node.Left;
                            break;

                        default:
                            throw new NotSupportedException($"Node type not supported (BinaryExpression): {node.Left.NodeType}");
                    }

                    switch (node.Right.NodeType)
                    {
                        case ExpressionType.MemberAccess:
                            memberAccess = node.Right;
                            break;

                        case ExpressionType.Constant:
                            constant = node.Right;
                            break;

                        default:
                            throw new NotSupportedException($"Node type not supported (BinaryExpression): {node.Right.NodeType}");
                    }

                    if (memberAccess is null || constant is null)
                        throw new NotSupportedException($"MemberAccess and Constant are required for BinaryExpression: Equal");

                    Visit(memberAccess);
                    _resultStringBuilder.Append("(");
                    Visit(constant);
                    _resultStringBuilder.Append(")");
                    break;

                case ExpressionType.AndAlso:
                    _resultStringBuilder.Append("\"statements\": [ ");

                    _resultStringBuilder.Append("{ \"query\":\"");
                    Visit(node.Left);
                    _resultStringBuilder.Append("\"}, ");

                    _resultStringBuilder.Append("{ \"query\":\"");
                    Visit(node.Right);
                    _resultStringBuilder.Append("\"} ]");
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }
        #endregion
    }
}
