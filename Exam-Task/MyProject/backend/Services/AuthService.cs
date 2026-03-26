using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MyProject.Application.DTOs.Auth;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<AuthResponseDto> SignUpAsync(SignUpDto dto)
        {
            var existing = await _userRepository.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new ConflictException("This email is already registered. Please sign in instead.");

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                AuthProvider = "manual",
                UpdatedAt = DateTime.UtcNow
            };

            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.Add(refreshToken);
            await _userRepository.CreateAsync(user);

            return BuildAuthResponse(user, refreshToken.Token);
        }

        public async Task<AuthResponseDto> SignInAsync(SignInDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
                throw new UnauthorizedException("Invalid email or password.");

            if (user.AuthProvider == "intuit" || string.IsNullOrWhiteSpace(user.PasswordHash))
                throw new UnauthorizedException("This account uses Intuit sign-in. Please continue with Intuit.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.RemoveAll(t => !t.IsActive);
            user.RefreshTokens.Add(refreshToken);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return BuildAuthResponse(user, refreshToken.Token);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var user = await _userRepository.FindByRefreshTokenAsync(refreshToken);
            if (user == null)
                throw new UnauthorizedException("Invalid refresh token.");

            var token = user.RefreshTokens.Single(t => t.Token == refreshToken);
            if (!token.IsActive)
                throw new UnauthorizedException("Refresh token is expired or revoked.");

            var newRefreshToken = _tokenService.GenerateRefreshToken();
            token.IsRevoked = true;
            token.ReplacedByToken = newRefreshToken.Token;
            user.RefreshTokens.RemoveAll(t => !t.IsActive && t.ReplacedByToken == null);
            user.RefreshTokens.Add(newRefreshToken);
            await _userRepository.UpdateAsync(user);

            return BuildAuthResponse(user, newRefreshToken.Token);
        }

        public async Task RevokeTokenAsync(string refreshToken, string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User account was not found.");

            var token = user.RefreshTokens.SingleOrDefault(t => t.Token == refreshToken);
            if (token == null || !token.IsActive)
                throw new BadRequestException("Refresh token is invalid or already revoked.");

            token.IsRevoked = true;
            await _userRepository.UpdateAsync(user);
        }

        public async Task<AuthResponseDto> IntuitCallbackAsync(string code, string state)
        {
            var clientId = _configuration["QuickBooks:ClientId"]!;
            var clientSecret = _configuration["QuickBooks:ClientSecret"]!;
            var redirectUri = _configuration["QuickBooks:SsoRedirectUri"]!;

            var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer");
            tokenRequest.Headers.Add("Authorization", $"Basic {credentials}");
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(tokenRequest);
            }
            catch (Exception ex)
            {
                throw new ExternalServiceException("Could not reach Intuit while exchanging the authorization code.", ex.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                var reason = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Intuit rejected the authorization code exchange request.", reason);
            }

            var tokenJson = await response.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
            var accessToken = tokenData.GetProperty("access_token").GetString()!;

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://sandbox-accounts.platform.intuit.com/v1/openid_connect/userinfo");
            userInfoRequest.Headers.Add("Authorization", $"Bearer {accessToken}");
            HttpResponseMessage userInfoResponse;
            try
            {
                userInfoResponse = await _httpClient.SendAsync(userInfoRequest);
            }
            catch (Exception ex)
            {
                throw new ExternalServiceException("Could not reach Intuit while fetching user profile details.", ex.Message);
            }

            if (!userInfoResponse.IsSuccessStatusCode)
            {
                var reason = await userInfoResponse.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Unable to retrieve user profile from Intuit.", reason);
            }

            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<JsonElement>(userInfoJson);

            var intuitSubId = userInfo.GetProperty("sub").GetString()!;
            var email = userInfo.GetProperty("email").GetString()!;
            var firstName = GetOptionalString(userInfo, "givenName")
                            ?? GetOptionalString(userInfo, "firstName")
                            ?? email;
            var lastName = GetOptionalString(userInfo, "familyName")
                           ?? GetOptionalString(userInfo, "lastName")
                           ?? string.Empty;
            var phoneNumber = GetOptionalString(userInfo, "phone_number")
                              ?? GetOptionalString(userInfo, "phoneNumber");

            var user = await _userRepository.GetByIntuitSubIdAsync(intuitSubId)
                       ?? await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Email = email.ToLower(),
                    PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
                    AuthProvider = "intuit",
                    IntuitSubId = intuitSubId,
                    PasswordHash = null,
                    UpdatedAt = DateTime.UtcNow
                };
                await _userRepository.CreateAsync(user);
            }
            else
            {
                user.AuthProvider = "intuit";
                user.IntuitSubId = intuitSubId;
                if (string.IsNullOrWhiteSpace(user.FirstName))
                    user.FirstName = firstName.Trim();
                if (string.IsNullOrWhiteSpace(user.LastName))
                    user.LastName = lastName.Trim();
                if (string.IsNullOrWhiteSpace(user.PhoneNumber) && !string.IsNullOrWhiteSpace(phoneNumber))
                    user.PhoneNumber = phoneNumber.Trim();
            }

            var refreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokens.RemoveAll(t => !t.IsActive);
            user.RefreshTokens.Add(refreshToken);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return BuildAuthResponse(user, refreshToken.Token);
        }

        private AuthResponseDto BuildAuthResponse(User user, string refreshToken)
        {
            return new AuthResponseDto
            {
                AccessToken = _tokenService.GenerateAccessToken(user),
                RefreshToken = refreshToken,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AuthProvider = user.AuthProvider,
                PhoneNumber = user.PhoneNumber,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                IncompleteFields = GetIncompleteFields(user)
            };
        }

        private static string? GetOptionalString(JsonElement source, string propertyName)
        {
            if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
                return null;

            var value = property.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static List<string> GetIncompleteFields(User user)
        {
            var incompleteFields = new List<string>();

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                incompleteFields.Add("phoneNumber");

            if (string.IsNullOrWhiteSpace(user.ProfilePhotoUrl))
                incompleteFields.Add("profilePhotoUrl");

            return incompleteFields;
        }
    }
}
