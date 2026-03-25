namespace OAuthDemo.Services;

using OAuthDemo.Data;
using OAuthDemo.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class QuickBooksService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly TokenRepository _tokenRepository;
    private readonly SqlRepository _sqlRepository;

    public QuickBooksService(IConfiguration config, IHttpClientFactory factory, TokenRepository tokenRepository, SqlRepository sqlRepository)
    {
        _config = config;
        _httpClient = factory.CreateClient();
        _tokenRepository = tokenRepository;
        _sqlRepository = sqlRepository;
    }

    public string GetAuthorizationUrl(string state)
    {
        var clientId = _config["QuickBooks:ClientId"];
        var redirectUri = Uri.EscapeDataString(_config["QuickBooks:RedirectUri"]);

        return $"https://appcenter.intuit.com/connect/oauth2" +
               $"?client_id={clientId}" +
               $"&response_type=code" +
               $"&scope=com.intuit.quickbooks.accounting%20openid%20profile%20email" +
               $"&redirect_uri={redirectUri}" +
               $"&state={state}";
    }

    public async Task<TokenResponse> ExchangeCodeForTokens(string code, string userId, string realmId)
    {
        var clientId = _config["QuickBooks:ClientId"];
        var clientSecret = _config["QuickBooks:ClientSecret"];
        var redirectUri = _config["QuickBooks:RedirectUri"];

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer");

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri }
        });

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Token exchange failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

        var tokenEntity = new QuickBooksToken
        {
            UserId = userId,
            AccessToken = tokenResponse.access_token,
            RefreshToken = tokenResponse.refresh_token,
            IdToken = tokenResponse.id_token,
            RealmId = realmId,
            AccessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
            RefreshTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.x_refresh_token_expires_in)
        };

        var existing = await _tokenRepository.GetByUserIdAsync(userId);

        if (existing == null)
        {
            await _tokenRepository.SaveTokenAsync(tokenEntity);
        }
        else
        {
            tokenEntity.Id = existing.Id; 
            await _tokenRepository.UpdateTokenAsync(userId, tokenEntity);
        }

        // Save to SQL Server
        await _sqlRepository.SaveOrUpdateTokenAsync(tokenEntity);

        return tokenResponse;
    }

    public async Task RefreshTokenAsync(string userId)
    {
        var existing = await _tokenRepository.GetByUserIdAsync(userId);
        if (existing == null) return;

        var clientId = _config["QuickBooks:ClientId"];
        var clientSecret = _config["QuickBooks:ClientSecret"];

        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer");

        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", existing.RefreshToken }
        });

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Refresh failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var updated = JsonSerializer.Deserialize<TokenResponse>(json);

        existing.AccessToken = updated.access_token;
        existing.RefreshToken = updated.refresh_token;
        existing.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(updated.expires_in);

        await _tokenRepository.UpdateTokenAsync(userId, existing);
        
        // Update SQL Server
        await _sqlRepository.SaveOrUpdateTokenAsync(existing);
    }
}