using System.Net;
using System.Net.Http.Json;
using BankAssist.Mcp.Configuration;
using BankAssist.Mcp.Masking;
using BankAssist.Mcp.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankAssist.Mcp.Tests;

[Trait("Category", "Integration")]
public class ResolveEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string ValidApiKey = "test-api-key-1234567890123456";
    private const string ValidAesKey = "1234567890123456789012345678901234567890123=";

    public ResolveEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(new SecretOptions
                {
                    ApiKey = ValidApiKey,
                    AesKey = Convert.FromBase64String(ValidAesKey)
                });
            });
        });
    }

    [Fact(DisplayName = "E-01: POST /vault/resolve đúng API key - 200, trả ánh xạ")]
    public async Task Resolve_WithValidApiKey_ReturnsResolvedMapping()
    {
        var client = _factory.CreateClient();
        
        // Seed token into vault
        var vault = _factory.Services.GetRequiredService<IVault>();
        var token = vault.Tokenize("session-e01", "Nguyễn Văn A", PiiEntityTypes.Person);

        var request = new VaultResolveRequest("session-e01", [token]);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/vault/resolve")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Api-Key", ValidApiKey);

        var response = await client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VaultResolveResponse>();
        result.Should().NotBeNull();
        result!.Resolved.Should().ContainKey(token);
        result.Resolved[token].Should().Be("Nguyễn Văn A");
    }

    [Fact(DisplayName = "E-02: Thiếu X-Api-Key - 401")]
    public async Task Resolve_MissingApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new VaultResolveRequest("session-e02", ["[PERSON_1]"]);
        
        var response = await client.PostAsJsonAsync("/vault/resolve", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "E-03: Sai X-Api-Key - 401")]
    public async Task Resolve_WrongApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var request = new VaultResolveRequest("session-e03", ["[PERSON_1]"]);
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/vault/resolve")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Api-Key", "wrong-api-key-99999999999");

        var response = await client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "E-04: tools/list không chứa bất kỳ tool nào liên quan tới vault hay resolve")]
    public async Task McpToolsList_DoesNotContainResolveTool()
    {
        var client = _factory.CreateClient();
        var requestContent = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(requestContent)
        };
        httpRequest.Headers.Add("Accept", "application/json, text/event-stream");

        var response = await client.SendAsync(httpRequest);
        var responseText = await response.Content.ReadAsStringAsync();

        responseText.Should().NotContain("resolve");
        responseText.Should().NotContain("vault");
    }

    [Fact(DisplayName = "E-05: Body rỗng - 400 kèm hướng dẫn")]
    public async Task Resolve_EmptyBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/vault/resolve")
        {
            Content = JsonContent.Create(new { })
        };
        httpRequest.Headers.Add("X-Api-Key", ValidApiKey);

        var response = await client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
