using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lex45x.EntityFramework.ComputedProperties;

[Generator]
public class ComputedPropertiesGenerator : ISourceGenerator
{
    private const string EfFriendlyAttributeSource = @"
namespace Lex45x.EntityFramework.ComputedProperties;
/// <summary>
/// Marks a property as one that has to be translated to respective ExpressionTree and substituted in EF queries
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EfFriendlyAttribute : Attribute
{
}";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new EntitySyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not EntitySyntaxReceiver receiver)
        {
            return;
        }

        context.AddSource("EfFriendlyAttribute.g.cs", SourceText.From(EfFriendlyAttributeSource, Encoding.UTF8));

        var namespacesBuilder = new HashSet<string>();

        //making sure that all used namespaces will be imported
        foreach (var property in receiver.Properties)
        {
            namespacesBuilder.Add(property.Symbol.ContainingNamespace.ToString());
        }

        var computedPropertiesLookup = @$"
using System.Linq.Expressions;
using System.Reflection;
{namespacesBuilder.Aggregate(new StringBuilder(), (builder, s) => builder.AppendLine($"using {s};"))}

namespace Lex45x.EntityFramework.ComputedProperties;
public static class EfFriendlyPropertiesLookup 
{{
    public static IReadOnlyDictionary<PropertyInfo, LambdaExpression> ComputedPropertiesExpression {{ get; }} = new Dictionary<PropertyInfo, LambdaExpression>
    {{
      {receiver.Properties.Aggregate(new StringBuilder(), (builder, declaration) => builder.AppendLine($"[{declaration.GetPropertyInfoDeclaration()}] = {declaration.GetExpressionDeclaration()},"))}
    }};    
}}";
        context.AddSource("EfFriendlyPropertiesLookup.g.cs", SourceText.From(computedPropertiesLookup, Encoding.UTF8));
    }
}

public class ComputedPropertySymbolVisitor : CSharpSyntaxWalker
{
    private readonly INamedTypeSymbol currentType;

    private readonly List<string> usedProperties = new();

    public ComputedPropertySymbolVisitor(INamedTypeSymbol currentType)
    {
        this.currentType = currentType;
    }

    public IReadOnlyList<string> UsedProperties => usedProperties;

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var referencedProperty = currentType.GetMembers(node.Identifier.ValueText);

        if (referencedProperty.Length > 0)
        {
            usedProperties.Add(node.Identifier.ValueText);
        }

        base.VisitIdentifierName(node);
    }
}

public class EntitySyntaxReceiver : ISyntaxContextReceiver
{
    public List<ComputedPropertyDeclaration> Properties { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax { AttributeLists.Count: > 0 } propertyDeclarationSyntax)
        {
            return;
        }

        var declaredSymbol = (IPropertySymbol)context.SemanticModel.GetDeclaredSymbol(context.Node)!;
        var attributes = declaredSymbol.GetAttributes();

        if (!attributes.Any(data => data.AttributeClass?.ToDisplayString() == "EfFriendly"))
        {
            return;
        }

        var visitor = new ComputedPropertySymbolVisitor(declaredSymbol.ContainingType);
        visitor.Visit(propertyDeclarationSyntax.ExpressionBody);


        Properties.Add(new ComputedPropertyDeclaration(declaredSymbol, propertyDeclarationSyntax,
            visitor.UsedProperties));
    }
}

public class ComputedPropertyDeclaration
{
    public ComputedPropertyDeclaration(IPropertySymbol symbol, PropertyDeclarationSyntax underlyingSyntax,
        IReadOnlyList<string> referencedProperties)
    {
        Symbol = symbol;
        UnderlyingSyntax = underlyingSyntax;
        ReferencedProperties = referencedProperties;
    }

    public IPropertySymbol Symbol { get; }
    public PropertyDeclarationSyntax UnderlyingSyntax { get; }
    public IReadOnlyList<string> ReferencedProperties { get; }

    public string GetExpressionDeclaration()
    {
        var getMethodBody = UnderlyingSyntax.ExpressionBody!.ToFullString();

        foreach (var usedProperty in ReferencedProperties)
        {
            getMethodBody = getMethodBody.Replace(usedProperty, $"entity.{usedProperty}");
        }

        return $"(Expression<Func<{Symbol.ContainingType.Name},{Symbol.Type.Name}>>) ((entity) {getMethodBody})";
    }

    public string GetPropertyInfoDeclaration()
    {
        return $"typeof({Symbol.ContainingType}).GetProperty(\"{Symbol.Name}\")";
    }
}