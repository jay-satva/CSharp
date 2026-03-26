using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MyProject.Application.DTOs.QuickBooks;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Constants;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class QuickBooksService : IQuickBooksService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public QuickBooksService(
            ICompanyRepository companyRepository,
            IEncryptionService encryptionService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _companyRepository = companyRepository;
            _encryptionService = encryptionService;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public string GetAuthorizationUrl(string userId)
        {
            var clientId = _configuration["QuickBooks:ClientId"]!;
            var redirectUri = Uri.EscapeDataString(_configuration["QuickBooks:RedirectUri"]!);
            var scope = Uri.EscapeDataString(AppConstants.QuickBooksScope);
            var state = Uri.EscapeDataString(userId);

            return $"{AppConstants.QuickBooksAuthUrl}?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}";
        }

        public async Task<CompanyDto> HandleCallbackAsync(string code, string realmId, string userId)
        {
            var tokens = await ExchangeCodeForTokensAsync(code, _configuration["QuickBooks:RedirectUri"]!);
            var accessToken = tokens["access_token"];
            var refreshToken = tokens["refresh_token"];
            var accessTokenExpiry = DateTime.UtcNow.AddSeconds(int.Parse(tokens["expires_in"]));
            var refreshTokenExpiry = DateTime.UtcNow.AddSeconds(int.Parse(tokens["x_refresh_token_expires_in"]));

            var companyName = await GetCompanyNameAsync(accessToken, realmId);

            var existing = await _companyRepository.GetByUserAndRealmIdAsync(userId, realmId);

            if (existing != null)
            {
                existing.CompanyName = companyName;
                existing.AccessToken = _encryptionService.Encrypt(accessToken);
                existing.RefreshToken = _encryptionService.Encrypt(refreshToken);
                existing.AccessTokenExpiry = accessTokenExpiry;
                existing.RefreshTokenExpiry = refreshTokenExpiry;
                existing.IsConnected = true;
                existing.ConnectedAt = DateTime.UtcNow;
                await _companyRepository.UpdateAsync(existing);
                return MapToDto(existing);
            }

            var company = new Company
            {
                UserId = userId,
                RealmId = realmId,
                CompanyName = companyName,
                AccessToken = _encryptionService.Encrypt(accessToken),
                RefreshToken = _encryptionService.Encrypt(refreshToken),
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry
            };

            await _companyRepository.CreateAsync(company);
            return MapToDto(company);
        }

        public async Task DisconnectAsync(string userId, string companyId)
        {
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null || company.UserId != userId || !company.IsConnected)
                throw new NotFoundException("Connected QuickBooks company was not found.");

            var accessToken = _encryptionService.Decrypt(company.AccessToken);

            var revokeRequest = new HttpRequestMessage(HttpMethod.Post, AppConstants.QuickBooksRevokeUrl);
            var clientId = _configuration["QuickBooks:ClientId"]!;
            var clientSecret = _configuration["QuickBooks:ClientSecret"]!;
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            revokeRequest.Headers.Add("Authorization", $"Basic {credentials}");
            revokeRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["token"] = accessToken
            });

            try
            {
                await _httpClient.SendAsync(revokeRequest);
            }
            catch (Exception ex)
            {
                throw new ExternalServiceException("QuickBooks disconnect request failed. Please try again.", ex.Message);
            }

            company.IsConnected = false;
            company.AccessToken = string.Empty;
            company.RefreshToken = string.Empty;
            await _companyRepository.UpdateAsync(company);
        }

        public async Task<CompanyDto?> GetConnectedCompanyAsync(string userId, string? companyId = null)
        {
            var company = await ResolveCompanyForUserAsync(userId, companyId, throwIfNotFound: false);
            if (company == null)
                return null;

            return MapToDto(company);
        }

        public async Task<List<CompanyDto>> GetConnectedCompaniesAsync(string userId)
        {
            var companies = await _companyRepository.GetConnectedByUserIdAsync(userId);
            return companies.Select(MapToDto).ToList();
        }

        public async Task<string> GetValidAccessTokenAsync(string userId, string? companyId = null)
        {
            var company = await ResolveCompanyForUserAsync(userId, companyId, throwIfNotFound: true)
                ?? throw new BadRequestException("QuickBooks is not connected. Connect a company before using this feature.");

            if (company.AccessTokenExpiry > DateTime.UtcNow.AddMinutes(5))
                return _encryptionService.Decrypt(company.AccessToken);

            if (company.RefreshTokenExpiry <= DateTime.UtcNow)
                throw new UnauthorizedException("Your QuickBooks session has expired. Please reconnect your company.");

            var refreshToken = _encryptionService.Decrypt(company.RefreshToken);
            var tokens = await RefreshAccessTokenAsync(refreshToken);

            company.AccessToken = _encryptionService.Encrypt(tokens["access_token"]);
            company.RefreshToken = _encryptionService.Encrypt(tokens["refresh_token"]);
            company.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(int.Parse(tokens["expires_in"]));
            company.RefreshTokenExpiry = DateTime.UtcNow.AddSeconds(int.Parse(tokens["x_refresh_token_expires_in"]));
            await _companyRepository.UpdateAsync(company);

            return tokens["access_token"];
        }

        private async Task<Company?> ResolveCompanyForUserAsync(string userId, string? companyId, bool throwIfNotFound)
        {
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                var selected = await _companyRepository.GetByIdAsync(companyId);
                if (selected != null && selected.UserId == userId && selected.IsConnected)
                    return selected;

                if (throwIfNotFound)
                    throw new NotFoundException("Selected QuickBooks company is not connected.");

                return null;
            }

            var fallback = await _companyRepository.GetByUserIdAsync(userId);
            if (fallback != null && fallback.IsConnected)
                return fallback;

            if (throwIfNotFound)
                throw new BadRequestException("No connected QuickBooks company found.");

            return null;
        }

        private async Task<Dictionary<string, string>> ExchangeCodeForTokensAsync(string code, string redirectUri)
        {
            var clientId = _configuration["QuickBooks:ClientId"]!;
            var clientSecret = _configuration["QuickBooks:ClientSecret"]!;
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, AppConstants.QuickBooksTokenUrl);
            request.Headers.Add("Authorization", $"Basic {credentials}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                throw new ExternalServiceException("Could not reach QuickBooks while exchanging authorization code.", ex.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                var reason = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("QuickBooks authorization code exchange failed.", reason);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new Dictionary<string, string>
            {
                ["access_token"] = data.GetProperty("access_token").GetString()!,
                ["refresh_token"] = data.GetProperty("refresh_token").GetString()!,
                ["expires_in"] = data.GetProperty("expires_in").GetInt32().ToString(),
                ["x_refresh_token_expires_in"] = data.GetProperty("x_refresh_token_expires_in").GetInt32().ToString()
            };
        }

        private async Task<Dictionary<string, string>> RefreshAccessTokenAsync(string refreshToken)
        {
            var clientId = _configuration["QuickBooks:ClientId"]!;
            var clientSecret = _configuration["QuickBooks:ClientSecret"]!;
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, AppConstants.QuickBooksTokenUrl);
            request.Headers.Add("Authorization", $"Basic {credentials}");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                throw new ExternalServiceException("Could not reach QuickBooks while refreshing access token.", ex.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                var reason = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("QuickBooks access token refresh failed.", reason);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            return new Dictionary<string, string>
            {
                ["access_token"] = data.GetProperty("access_token").GetString()!,
                ["refresh_token"] = data.GetProperty("refresh_token").GetString()!,
                ["expires_in"] = data.GetProperty("expires_in").GetInt32().ToString(),
                ["x_refresh_token_expires_in"] = data.GetProperty("x_refresh_token_expires_in").GetInt32().ToString()
            };
        }

        private async Task<string> GetCompanyNameAsync(string accessToken, string realmId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/companyinfo/{realmId}?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return "Unknown Company";

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.GetProperty("CompanyInfo").GetProperty("CompanyName").GetString() ?? "Unknown Company";
        }

        private static CompanyDto MapToDto(Company company)
        {
            return new CompanyDto
            {
                Id = company.Id,
                RealmId = company.RealmId,
                CompanyName = company.CompanyName,
                IsConnected = company.IsConnected,
                ConnectedAt = company.ConnectedAt
            };
        }
    }
}
