using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lex45x.EntityFramework.ComputedProperties.Interceptor;

public class ComputedPropertyCallInterceptor : IQueryExpressionInterceptor
{
    private readonly ComputedPropertiesVisitor visitor;

    public ComputedPropertyCallInterceptor(IReadOnlyDictionary<PropertyInfo, LambdaExpression> configuration)
    {
        visitor = new ComputedPropertiesVisitor(configuration);
    }

    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        return visitor.Visit(queryExpression);
    }
}