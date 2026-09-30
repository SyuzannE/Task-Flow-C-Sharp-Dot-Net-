using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite cannot order DateTimeOffset natively, so store as unix milliseconds.
        var unixMs = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        modelBuilder.Entity<TodoItem>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Title).IsRequired().HasMaxLength(TodoItem.MaxTitleLength);
            b.Property(t => t.CreatedAt).HasConversion(unixMs);
            b.Property(t => t.CompletedAt).HasConversion(unixMs);
        });
    }
}

internal sealed class TodoRepository(AppDbContext db) : ITodoRepository
{
    public async Task<IReadOnlyList<TodoItem>> ListAsync(CancellationToken ct) =>
        await db.Todos.AsNoTracking().OrderBy(t => t.CreatedAt).ToListAsync(ct);

    public Task<TodoItem?> FindAsync(Guid id, CancellationToken ct) =>
        db.Todos.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(TodoItem item, CancellationToken ct) => await db.Todos.AddAsync(item, ct);

    public void Remove(TodoItem item) => db.Todos.Remove(item);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Resolve the connection string lazily so test hosts can override configuration.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                     ?? "Data Source=taskflow.db";
            options.UseSqlite(cs);
        });
        services.AddScoped<ITodoRepository, TodoRepository>();
        return services;
    }
}
