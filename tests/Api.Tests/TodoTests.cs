using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public class TodoTests
{
    private HttpClient CreateClient()
    {
        var factory = new CustomWebApplicationFactory();
        return factory.CreateClient();
    }

    private async Task<(HttpClient client, string token)> CreateAuthenticatedClient(string email = "todo@test.com")
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "password123"
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, token);
    }

    [Fact]
    public async Task List_WithoutAuth_Returns401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/v1/todos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsNewTodo()
    {
        var (client, _) = await CreateAuthenticatedClient("create@test.com");

        var response = await client.PostAsJsonAsync("/api/v1/todos", new
        {
            title = "Test todo",
            priority = "HIGH"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.Equal("Test todo", data.GetProperty("title").GetString());
        Assert.Equal("HIGH", data.GetProperty("priority").GetString());
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400()
    {
        var (client, _) = await CreateAuthenticatedClient("notitle@test.com");

        var response = await client.PostAsJsonAsync("/api/v1/todos", new { priority = "LOW" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsUserTodos()
    {
        var (client, _) = await CreateAuthenticatedClient("list@test.com");

        var r1 = await client.PostAsJsonAsync("/api/v1/todos", new { title = "Todo 1" });
        var r2 = await client.PostAsJsonAsync("/api/v1/todos", new { title = "Todo 2" });
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode);

        var response = await client.GetAsync("/api/v1/todos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Update_ChangesTodoFields()
    {
        var (client, _) = await CreateAuthenticatedClient("update@test.com");

        var createResponse = await client.PostAsJsonAsync("/api/v1/todos", new { title = "Original" });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var todoId = createBody.GetProperty("data").GetProperty("id").GetString();

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/todos/{todoId}")
        {
            Content = JsonContent.Create(new { title = "Updated" })
        };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated", body.GetProperty("data").GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_NonexistentTodo_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClient("update404@test.com");

        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/todos/nonexistent")
        {
            Content = JsonContent.Create(new { title = "Nope" })
        };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletesTodo()
    {
        var (client, _) = await CreateAuthenticatedClient("delete@test.com");

        var createResponse = await client.PostAsJsonAsync("/api/v1/todos", new { title = "To delete" });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var todoId = createBody.GetProperty("data").GetProperty("id").GetString();

        var deleteResponse = await client.DeleteAsync($"/api/v1/todos/{todoId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/v1/todos");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, listBody.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Delete_NonexistentTodo_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClient("delete404@test.com");

        var response = await client.DeleteAsync("/api/v1/todos/nonexistent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
