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
            string? targetUserId,
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

                if (string.IsNullOrWhiteSpace(resolvedName))
                {
                    var givenName = jwtToken.Claims.FirstOrDefault(
                        c => string.Equals(c.Type, "given_name", StringComparison.OrdinalIgnoreCase))?.Value;
                    var familyName = jwtToken.Claims.FirstOrDefault(
                        c => string.Equals(c.Type, "family_name", StringComparison.OrdinalIgnoreCase))?.Value;
                    resolvedName = string.Join(" ", new[] { givenName, familyName }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
                    if (string.IsNullOrWhiteSpace(resolvedName))
                        resolvedName = null;
                }
            }

            if (string.IsNullOrWhiteSpace(resolvedEmail) || string.IsNullOrWhiteSpace(resolvedName) || string.IsNullOrWhiteSpace(subject))
            {
                var userInfo = await GetUserInfoAsync(tokenResponse.access_token);
                resolvedEmail = userInfo?.email ?? resolvedEmail;
                resolvedName = userInfo?.name ?? resolvedName;
                subject = userInfo?.sub ?? subject;
            }

            resolvedEmail ??= userEmailFallbackForMissingIdToken;
            if (string.IsNullOrWhiteSpace(resolvedEmail) && string.IsNullOrWhiteSpace(subject))
                throw new Exception("Could not resolve the Intuit user identity.");

            var user = await _userRepository.UpsertIntuitUserAsync(
                intuitSub: subject,
                email: resolvedEmail,
                targetUserId: targetUserId,
                name: resolvedName,
                linkIntuitSub: string.IsNullOrWhiteSpace(targetUserId));

            if (syncCompanyInfo && realmId != null)
                await SyncCompanyInfoAsync(user.UserId, subject, realmId, tokenResponse);

            return (tokenResponse, user);
        }

        private async Task<UserInfoDto?> GetUserInfoAsync(string accessToken)
        {
            var endpoints = new[]
            {
                "https://sandbox-accounts.platform.intuit.com/v1/openid_connect/userinfo",
                "https://accounts.platform.intuit.com/v1/openid_connect/userinfo"
            };

            foreach (var endpoint in endpoints)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    continue;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var givenName = ReadOptionalString(root, "givenName") ?? ReadOptionalString(root, "given_name");
                var familyName = ReadOptionalString(root, "familyName") ?? ReadOptionalString(root, "family_name");
                var fullName = ReadOptionalString(root, "name");
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = string.Join(" ", new[] { givenName, familyName }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
                    if (string.IsNullOrWhiteSpace(fullName))
                        fullName = null;
                }

                return new UserInfoDto
                {
                    sub = ReadOptionalString(root, "sub") ?? string.Empty,
                    email = ReadOptionalString(root, "email") ?? ReadOptionalString(root, "emailAddress"),
                    name = fullName,
                    givenName = givenName,
                    familyName = familyName,
                    email_verified = root.TryGetProperty("email_verified", out var verified) && verified.ValueKind == JsonValueKind.True
                };
            }

            return null;
        }

        private static string? ReadOptionalString(JsonElement source, string propertyName)
        {
            if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return null;

            var value = property.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private async Task SyncCompanyInfoAsync(string userId, string? intuitSub, string realmId, TokenResponseDto tokenResponse)
        {
            var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/companyinfo/{realmId}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.access_token);
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
                    IntuitSub = intuitSub,
                    RealmId = realmId,
                    CompanyName = companyInfo.GetProperty("CompanyName").GetString() ?? "Unknown",
                    Country = companyInfo.GetProperty("Country").GetString() ?? "US",
                    AccessToken = tokenResponse.access_token,
                    IdToken = tokenResponse.id_token,
                    RefreshToken = tokenResponse.refresh_token,
                    AccessTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
                    RefreshTokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.x_refresh_token_expires_in),
                    IsActive = true,
                    LinkedAt = DateTime.UtcNow
                };

                await _companyRepository.UpsertConnectedCompanyAsync(userId, company);
            }
        }

        public async Task RefreshTokenAsync(string userId, string realmId)
        {
            var existing = await _companyRepository.GetActiveByUserIdAndRealmIdAsync(userId, realmId);
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
                existing.LinkedAt = DateTime.UtcNow;

                await _companyRepository.UpsertConnectedCompanyAsync(userId, existing);
            }
        }

        public async Task<string> GetAccessTokenAsync(string userId, string? realmId = null)
        {
            var company = !string.IsNullOrWhiteSpace(realmId)
                ? await _companyRepository.GetActiveByUserIdAndRealmIdAsync(userId, realmId)
                : (await _companyRepository.GetActiveByUserIdAsync(userId)).FirstOrDefault();

            if (company == null || string.IsNullOrWhiteSpace(company.AccessToken))
                throw new Exception("Tokens not found");

            if (company.AccessTokenExpiry.HasValue && DateTime.UtcNow >= company.AccessTokenExpiry.Value)
            {
                await RefreshTokenAsync(userId, company.RealmId);
                company = await _companyRepository.GetActiveByUserIdAndRealmIdAsync(userId, company.RealmId);
            }

            return company!.AccessToken!;
        }

        public async Task<string> GetRealmIdAsync(string userId)
        {
            var company = (await _companyRepository.GetActiveByUserIdAsync(userId)).FirstOrDefault();
            return company?.RealmId ?? throw new Exception("RealmId not found");
        }

        public async Task<bool> DisconnectCompanyAsync(string userId, string realmId)
        {
            var company = await _companyRepository.GetByUserIdAndRealmIdAsync(userId, realmId);
            if (company == null)
                return false;

            var tokenToRevoke = !string.IsNullOrWhiteSpace(company.AccessToken)
                ? company.AccessToken
                : company.RefreshToken;

            if (!string.IsNullOrWhiteSpace(tokenToRevoke))
            {
                var clientId = _config["QuickBooks:ClientId"];
                var clientSecret = _config["QuickBooks:ClientSecret"];
                var request = new HttpRequestMessage(HttpMethod.Post, "https://developer.api.intuit.com/v2/oauth2/tokens/revoke");
                var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "token", tokenToRevoke }
                });

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"QuickBooks disconnect failed: {error}");
                }
            }

            company.IsActive = false;
            company.AccessToken = string.Empty;
            company.IdToken = null;
            company.RefreshToken = string.Empty;
            company.AccessTokenExpiry = null;
            company.RefreshTokenExpiry = null;

            await _companyRepository.UpsertConnectedCompanyAsync(userId, company);
            return true;
        }
    }
}
