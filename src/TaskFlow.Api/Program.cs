using Microsoft.EntityFrameworkCore;
using TaskFlow.Api;
using TaskFlow.Application;
using TaskFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    // Swap for EF Core migrations once the schema starts evolving.
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
}

app.MapHealthChecks("/health");

var todos = app.MapGroup("/api/todos").WithTags("Todos");

todos.MapGet("/", (TodoService s, CancellationToken ct) => s.ListAsync(ct));

todos.MapGet("/{id:guid}", async (Guid id, TodoService s, CancellationToken ct) =>
    await s.GetAsync(id, ct) is { } todo ? Results.Ok(todo) : Results.NotFound());

todos.MapPost("/", async (CreateTodoRequest request, TodoService s, CancellationToken ct) =>
{
    var created = await s.CreateAsync(request, ct);
    return Results.Created($"/api/todos/{created.Id}", created);
});

todos.MapPost("/{id:guid}/complete", async (Guid id, TodoService s, CancellationToken ct) =>
    await s.CompleteAsync(id, ct) is { } todo ? Results.Ok(todo) : Results.NotFound());

todos.MapDelete("/{id:guid}", async (Guid id, TodoService s, CancellationToken ct) =>
    await s.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

await app.RunAsync();

public partial class Program; // exposed for WebApplicationFactory<Program>
