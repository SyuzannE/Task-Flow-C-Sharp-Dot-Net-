using TaskFlow.Domain;

namespace TaskFlow.Tests;

public class TodoItemTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_TrimsTitleAndStampsCreationTime()
    {
        var item = TodoItem.Create("  Write tests  ", Clock);

        Assert.Equal("Write tests", item.Title);
        Assert.Equal(Clock.GetUtcNow(), item.CreatedAt);
        Assert.False(item.IsCompleted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankTitle(string? title) =>
        Assert.Throws<DomainException>(() => TodoItem.Create(title, Clock));

    [Fact]
    public void Create_RejectsOverlongTitle() =>
        Assert.Throws<DomainException>(() => TodoItem.Create(new string('x', TodoItem.MaxTitleLength + 1), Clock));

    [Fact]
    public void Complete_IsIdempotent()
    {
        var item = TodoItem.Create("Ship it", Clock);
        item.Complete(Clock);
        var first = item.CompletedAt;
        item.Complete(new FixedClock(Clock.GetUtcNow().AddDays(1)));

        Assert.True(item.IsCompleted);
        Assert.Equal(first, item.CompletedAt);
    }
}
