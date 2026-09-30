using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TaskFlow.Application;

namespace TaskFlow.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"taskflow-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath};Pooling=False",
            }));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // best effort cleanup
        }
    }
}

public class TodoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenComplete_RoundTrips()
    {
        var created = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Try it"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var todo = await created.Content.ReadFromJsonAsync<TodoDto>();
        Assert.NotNull(todo);

        var completed = await _client.PostAsync($"/api/todos/{todo.Id}/complete", content: null);
        var done = await completed.Content.ReadFromJsonAsync<TodoDto>();

        Assert.True(done!.IsCompleted);
        Assert.NotNull(done.CompletedAt);
    }

    [Fact]
    public async Task Create_WithBlankTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest(" "));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/todos/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTodo()
    {
        var created = await (await _client.PostAsJsonAsync("/api/todos", new CreateTodoRequest("Temp")))
            .Content.ReadFromJsonAsync<TodoDto>();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/todos/{created!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/todos/{created.Id}")).StatusCode);
    }
}
