using System.Linq.Expressions;
using Dapper;

namespace NzbDrone.Core.Datastore
{
    public abstract class WhereBuilder : ExpressionVisitor
    {
        public DynamicParameters Parameters { get; protected set; }

        /// <summary>
        /// Strips conversion nodes the C# compiler inserts around an expression so the
        /// underlying member/constant can be inspected.
        /// </summary>
        /// <remarks>
        /// The compiler wraps arguments in a conversion node whenever an implicit
        /// conversion is needed. The case that matters here is <c>int[]</c>: it has no
        /// instance <c>Contains</c>, so <c>ids.Contains(x)</c> binds to the static
        /// <c>Enumerable.Contains(IEnumerable&lt;int&gt;, int)</c> overload and the
        /// array argument arrives as <c>Convert(member, IEnumerable&lt;int&gt;)</c>
        /// rather than as a bare <see cref="MemberExpression"/>. A <c>List&lt;int&gt;</c>
        /// binds to the instance method and needs no conversion, which is why that
        /// shape always worked and this one did not.
        /// </remarks>
        protected static Expression Unwrap(Expression expression)
        {
            while (true)
            {
                switch (expression)
                {
                    case UnaryExpression unary when unary.NodeType is ExpressionType.Convert
                                                                   or ExpressionType.ConvertChecked
                                                                   or ExpressionType.TypeAs:
                        expression = unary.Operand;
                        continue;

                    // User-defined conversion operators, e.g. the nodes .NET 10 emits
                    // for some enum-typed collections.
                    case MethodCallExpression call when call.Arguments.Count == 1 &&
                                                        call.Method.IsStatic &&
                                                        (call.Method.Name == "op_Implicit" || call.Method.Name == "op_Explicit"):
                        expression = call.Arguments[0];
                        continue;

                    default:
                        return expression;
                }
            }
        }
    }
}
