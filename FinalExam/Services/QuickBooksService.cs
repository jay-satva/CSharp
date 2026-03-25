namespace FinalExam.Services
{
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.IdentityModel.Tokens.Jwt;
    using FinalExam.Models;
    using FinalExam.DTOs;
    using FinalExam.Data;

    public class QuickBooksService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly UserRepository _userRepository;
        private readonly CompanyRepository _companyRepository;

        public QuickBooksService(
            IConfiguration config, 
            IHttpClientFactory factory, 
            UserRepository userRepository,
            CompanyRepository companyRepository)
        {
            _config = config;
            _httpClient = factory.CreateClient();
            _userRepository = userRepository;
            _companyRepository = companyRepository;
        }

        public string GetSsoAuthorizationUrl(string state)
        {
            var clientId = _config["QuickBooks:ClientId"];
            var redirectUri = Uri.EscapeDataString(_config["QuickBooks:SsoRedirectUri"]!);
            var scopes = _config["QuickBooks:SsoScopes"]!;
            
            return $"https://appcenter.intuit.com/connect/oauth2" +
                   $"?client_id={clientId}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopes)}" +
                   $"&redirect_uri={redirectUri}" +
                   $"&state={state}";
        }

        public string GetQbAuthorizationUrl(string state)
        {
            var clientId = _config["QuickBooks:ClientId"];
            var redirectUri = Uri.EscapeDataString(_config["QuickBooks:QbRedirectUri"]!);
            var scopes = _config["QuickBooks:QbScopes"]!;

            return $"https://appcenter.intuit.com/connect/oauth2" +
                   $"?client_id={clientId}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopes)}" +
                   $"&redirect_uri={redirectUri}" +
                   $"&state={state}";
        }

        public async Task<(TokenResponseDto TokenResponse, AppUser User)> ExchangeCodeForTokensAsync(
            string code,
            string? realmId,
            string? userEmailFallbackForMissingIdToken,
            string? userNameFallbackForMissingIdToken,
            bool syncCompanyInfo)
        {
            var clientId = _config["QuickBooks:ClientId"];
            var clientSecret = _config["QuickBooks:ClientSecret"];

            // Intuit expects the same redirect_uri as used during authorization
            var redirectUri =
                syncCompanyInfo
                    ? _config["QuickBooks:QbRedirectUri"]
                    : _config["QuickBooks:SsoRedirectUri"];
            if (string.IsNullOrWhiteSpace(redirectUri))
                throw new Exception("RedirectUri config is missing.");

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
            var tokenResponse = JsonSerializer.Deserialize<TokenResponseDto>(json)!;

            string? resolvedEmail = null;
            string? resolvedName = userNameFallbackForMissingIdToken;
            string? subject = null;
            if (!string.IsNullOrWhiteSpace(tokenResponse.id_token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(tokenResponse.id_token);
                subject = jwtToken.Subject;
                var emailClaim = jwtToken.Claims.FirstOrDefault(
                    c => string.Equals(c.Type, "email", StringComparison.OrdinalIgnoreCase));
                resolvedEmail = emailClaim?.Value;

                var nameClaim = jwtToken.Claims.FirstOrDefault(
                    c => string.Equals(c.Type, "name", StringComparison.OrdinalIgnoreCase));
                resolvedName = nameClaim?.Value ?? resolvedName;
            }

            if (string.IsNullOrWhiteSpace(resolvedEmail))
            {
                var userInfo = await GetUserInfoAsync(tokenResponse.access_token);
                resolvedEmail = userInfo?.email ?? resolvedEmail;
                resolvedName = userInfo?.name ?? resolvedName;
            }

            resolvedEmail ??= userEmailFallbackForMissingIdToken;
            if (string.IsNullOrWhiteSpace(resolvedEmail) && string.IsNullOrWhiteSpace(subject))
                throw new Exception("Could not resolve the Intuit user identity.");

            var user = await _userRepository.UpsertQuickBooksTokensAsync(
                intuitSub: subject,
                email: resolvedEmail,
                name: resolvedName,
                accessToken: tokenResponse.access_token,
                refreshToken: tokenResponse.refresh_token,
                idToken: tokenResponse.id_token,
                realmId: realmId,
                accessTokenExpiry: DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
                refreshTokenExpiry: DateTime.UtcNow.AddSeconds(tokenResponse.x_refresh_token_expires_in));

            if (syncCompanyInfo && realmId != null)
                await SyncCompanyInfoAsync(user.UserId, realmId, tokenResponse.access_token);

            return (tokenResponse, user);
        }

        private async Task<UserInfoDto?> GetUserInfoAsync(string accessToken)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://accounts.platform.intuit.com/v1/openid_connect/userinfo");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserInfoDto>(json);
        }

        private async Task SyncCompanyInfoAsync(string userId, string realmId, string accessToken)
        {
            var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/companyinfo/{realmId}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var companyInfo = doc.RootElement.GetProperty("CompanyInfo");

                var company = new Company
                {
                    UserId = userId,
                    RealmId = realmId,
                    CompanyName = companyInfo.GetProperty("CompanyName").GetString() ?? "Unknown",
                    Country = companyInfo.GetProperty("Country").GetString() ?? "US",
                    IsActive = true,
                    LinkedAt = DateTime.UtcNow
                };

                await _companyRepository.UpsertConnectedCompanyAsync(userId, company);
            }
        }

        public async Task RefreshTokenAsync(string userId)
        {
            var existing = await _userRepository.GetByUserIdAsync(userId);
            if (existing == null) return;
            if (string.IsNullOrWhiteSpace(existing.RefreshToken)) return;

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
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var updated = JsonSerializer.Deserialize<TokenResponseDto>(json)!;

                existing.AccessToken = updated.access_token;
                existing.RefreshToken = updated.refresh_token;
                existing.IdToken = updated.id_token ?? existing.IdToken;
                existing.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(updated.expires_in);
                existing.RefreshTokenExpiry = DateTime.UtcNow.AddSeconds(updated.x_refresh_token_expires_in);

                await _userRepository.UpdateAsync(existing);
            }
        }

        public async Task<string> GetAccessTokenAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.AccessToken))
                throw new Exception("Tokens not found");

            if (user.AccessTokenExpiry.HasValue && DateTime.UtcNow >= user.AccessTokenExpiry.Value)
            {
                await RefreshTokenAsync(userId);
                user = await _userRepository.GetByUserIdAsync(userId);
            }

            return user!.AccessToken!;
        }

        public async Task<string> GetRealmIdAsync(string userId)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            return user?.RealmId ?? throw new Exception("RealmId not found");
        }
    }
}
