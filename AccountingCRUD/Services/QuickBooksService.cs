using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AccountingCRUD.Repository;

namespace AccountingCRUD.Services
{
    public class QuickBooksService
    {
        private readonly HttpClient _httpClient;
        private readonly TokenRepository _tokenRepository;
        private readonly string _baseUrl;

        public QuickBooksService(HttpClient httpClient, TokenRepository tokenRepository, IConfiguration config)
        {
            _httpClient = httpClient;
            _tokenRepository = tokenRepository;
            _baseUrl = config["QuickBooks:BaseUrl"];
        }

        private async Task<string> GetAccessTokenAsync(string userId)
        {
            var token = await _tokenRepository.GetTokenByUserIdAsync(userId);
            if (token == null) throw new Exception($"No token found for user {userId}");
            
            if (DateTime.UtcNow >= token.AccessTokenExpiry)
            {
                throw new Exception("Access token expired. Please re-authenticate via OAuthDemo app.");
            }

            return token.AccessToken;
        }

        private async Task<string> GetRealmIdAsync(string userId)
        {
            var token = await _tokenRepository.GetTokenByUserIdAsync(userId);
            return token?.RealmId ?? throw new Exception($"No realmId found for user {userId}");
        }

        public async Task<string> ExecuteRequestAsync(string userId, HttpMethod method, string resource, object body = null)
        {
            var accessToken = await GetAccessTokenAsync(userId);
            var realmId = await GetRealmIdAsync(userId);
            
            var separator = resource.Contains("?") ? "&" : "?";
            var url = $"{_baseUrl}/v3/company/{realmId}/{resource}{separator}minorversion=75";

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"QuickBooks API Error: {content}");
            }

            return content;
        }
    }
}
