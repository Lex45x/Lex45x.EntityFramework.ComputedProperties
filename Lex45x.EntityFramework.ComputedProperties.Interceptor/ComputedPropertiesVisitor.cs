using System.Linq.Expressions;
using System.Reflection;

namespace Lex45x.EntityFramework.ComputedProperties.Interceptor;

internal class ComputedPropertiesVisitor : ExpressionVisitor
{
    private readonly IReadOnlyDictionary<PropertyInfo, LambdaExpression> configuration;

    public ComputedPropertiesVisitor(IReadOnlyDictionary<PropertyInfo, LambdaExpression> configuration)
    {
        this.configuration = configuration;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is not PropertyInfo propertyInfo || !configuration.ContainsKey(propertyInfo))
        {
            return base.VisitMember(node);
        }

        var expression = configuration[propertyInfo];
        var resultExpression = expression.Body;
        var parametersToReplace = expression.Parameters;

        var parameterExpression = parametersToReplace[index: 0];

        var expressionVisitor = new ReplaceParametersExpressionVisitor(parameterExpression, node.Expression);
        resultExpression = expressionVisitor.Visit(resultExpression);

        return resultExpression;
    }
}