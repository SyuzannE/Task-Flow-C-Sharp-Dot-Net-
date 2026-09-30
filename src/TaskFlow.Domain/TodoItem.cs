namespace TaskFlow.Domain;

public sealed class TodoItem
{
    public const int MaxTitleLength = 200;

    private TodoItem() => Title = string.Empty; // EF Core

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static TodoItem Create(string? title, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = NormalizeTitle(title),
            CreatedAt = clock.GetUtcNow(),
        };
    }

    public void Rename(string? title) => Title = NormalizeTitle(title);

    public void Complete(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (IsCompleted)
        {
            return;
        }

        IsCompleted = true;
        CompletedAt = clock.GetUtcNow();
    }

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        var trimmed = title.Trim();
        return trimmed.Length > MaxTitleLength
            ? throw new DomainException($"Title must be at most {MaxTitleLength} characters.")
            : trimmed;
    }
}
