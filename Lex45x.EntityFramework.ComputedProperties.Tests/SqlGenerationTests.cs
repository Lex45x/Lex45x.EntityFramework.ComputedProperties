using Lex45x.EntityFramework.ComputedProperties.Tests.Database;
using Microsoft.EntityFrameworkCore;

namespace Lex45x.EntityFramework.ComputedProperties.Tests;

public class SqlGenerationTests
{
    private VacationSystemContext dbContext = null!;

    [SetUp]
    public async Task Setup()
    {
        var dbContextOptionsBuilder =
            new DbContextOptionsBuilder<VacationSystemContext>().UseSqlite(
                "DataSource=InMemory;Mode=Memory;Cache=Shared");
        dbContext = new VacationSystemContext(dbContextOptionsBuilder.Options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureDeletedAsync();
        var creationResult = await dbContext.Database.EnsureCreatedAsync();
    }

    [Test]
    public async Task VacationBudgetProjection()
    {
        var query = dbContext.Employees.Select(employee => new
        {
            employee.Id,
            employee.VacationBudget
        }).ToQueryString();
    }
}