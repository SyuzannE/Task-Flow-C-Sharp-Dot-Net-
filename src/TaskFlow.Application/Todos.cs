using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Domain;

namespace TaskFlow.Application;

public sealed record TodoDto(Guid Id, string Title, bool IsCompleted, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

public sealed record CreateTodoRequest(string Title);

public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> ListAsync(CancellationToken ct);
    Task<TodoItem?> FindAsync(Guid id, CancellationToken ct);
    Task AddAsync(TodoItem item, CancellationToken ct);
    void Remove(TodoItem item);
    Task SaveChangesAsync(CancellationToken ct);
}

public sealed class TodoService(ITodoRepository repository, TimeProvider clock)
{
    public async Task<IReadOnlyList<TodoDto>> ListAsync(CancellationToken ct = default) =>
        [.. (await repository.ListAsync(ct)).Select(ToDto)];

    public async Task<TodoDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        await repository.FindAsync(id, ct) is { } item ? ToDto(item) : null;

    public async Task<TodoDto> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = TodoItem.Create(request.Title, clock);
        await repository.AddAsync(item, ct);
        await repository.SaveChangesAsync(ct);
        return ToDto(item);
    }

    public async Task<TodoDto?> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await repository.FindAsync(id, ct);
        if (item is null)
        {
            return null;
        }

        item.Complete(clock);
        await repository.SaveChangesAsync(ct);
        return ToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await repository.FindAsync(id, ct);
        if (item is null)
        {
            return false;
        }

        repository.Remove(item);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    private static TodoDto ToDto(TodoItem i) => new(i.Id, i.Title, i.IsCompleted, i.CreatedAt, i.CompletedAt);
}

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services.AddScoped<TodoService>();
}
