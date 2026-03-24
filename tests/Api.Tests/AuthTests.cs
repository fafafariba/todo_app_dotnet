using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsTokenAndUser()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "test@test.com",
            password = "password123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        Assert.NotEmpty(data.GetProperty("token").GetString()!);
        Assert.Equal("test@test.com", data.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var payload = new { email = "dup@test.com", password = "password123" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "short@test.com",
            password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "login@test.com",
            password = "password123"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "login@test.com",
            password = "password123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(body.GetProperty("data").GetProperty("token").GetString()!);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = "wrong@test.com",
            password = "password123"
        });

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "wrong@test.com",
            password = "wrongpassword"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonexistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nobody@test.com",
            password = "password123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
