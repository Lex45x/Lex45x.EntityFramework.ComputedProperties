using System.Linq.Expressions;

namespace Lex45x.EntityFramework.ComputedProperties.Interceptor;

internal class ReplaceParametersExpressionVisitor : ExpressionVisitor
{
    private readonly Expression expression;
    private readonly ParameterExpression propertyParameter;

    public ReplaceParametersExpressionVisitor(ParameterExpression propertyParameter, Expression expression)
    {
        this.propertyParameter = propertyParameter;
        this.expression = expression;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == propertyParameter ? expression : base.VisitParameter(node);
    }
}