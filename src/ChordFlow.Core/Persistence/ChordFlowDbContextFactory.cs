using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChordFlow.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> can build the context without
/// launching the WinForms host. Runtime wiring constructs the context directly in
/// <c>Program.cs</c>; this exists only for the EF tooling.
/// </summary>
public sealed class ChordFlowDbContextFactory : IDesignTimeDbContextFactory<ChordFlowDbContext>
{
    public ChordFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ChordFlowDbContext>()
            .UseSqlite($"Data Source={ChordFlowDbContext.DefaultDbPath()}")
            .Options;
        return new ChordFlowDbContext(options);
    }
}
