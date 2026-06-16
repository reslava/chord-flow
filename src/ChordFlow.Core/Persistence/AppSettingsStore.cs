using ChordFlow.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChordFlow.Persistence;

/// <summary>
/// SQLite-backed <see cref="IAppSettings"/> over the shared <see cref="ChordFlowDbContext"/>. Unlike the
/// per-request content stores (which take a live context), this is an app-lifetime accessor: it holds the
/// <see cref="DbContextOptions{TContext}"/> and opens a short-lived context per Get/Set, so the singleton
/// never owns a long-lived tracking context. Access is infrequent (read on boot, write on a settings change).
/// </summary>
public sealed class AppSettingsStore : IAppSettings
{
    private readonly DbContextOptions<ChordFlowDbContext> _options;

    public AppSettingsStore(DbContextOptions<ChordFlowDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc/>
    public string? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var db = new ChordFlowDbContext(_options);
        return db.AppSettings.AsNoTracking().FirstOrDefault(s => s.Key == key)?.Value;
    }

    /// <inheritdoc/>
    public void Set(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        using var db = new ChordFlowDbContext(_options);
        AppSettingEntity? row = db.AppSettings.Find(key);
        if (row is null)
        {
            db.AppSettings.Add(new AppSettingEntity { Key = key, Value = value });
        }
        else
        {
            row.Value = value;
        }

        db.SaveChanges();
    }
}
