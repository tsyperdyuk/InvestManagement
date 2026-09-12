using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InvestManagement.Api.Tests.Integration;

internal static class TestAuthentication
{
    public static async Task SignInAsync(HttpClient client)
    {
        var credentials = new
        {
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Password = "TestPassword!482"
        };

        using var registration = await client.PostAsJsonAsync("/api/auth/register", credentials);
        registration.EnsureSuccessStatusCode();

        using var login = await client.PostAsJsonAsync("/api/auth/login?useCookies=false", credentials);
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", tokens.GetProperty("accessToken").GetString());
    }
}
