using Lex45x.EntityFramework.ComputedProperties.Interceptor;
using Lex45x.EntityFramework.ComputedProperties.Tests.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lex45x.EntityFramework.ComputedProperties.Tests.Database;

public class VacationSystemContext : DbContext
{
    public VacationSystemContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(
            new ComputedPropertyCallInterceptor(EfFriendlyPropertiesLookup.ComputedPropertiesExpression));

        base.OnConfiguring(optionsBuilder);
    }
}