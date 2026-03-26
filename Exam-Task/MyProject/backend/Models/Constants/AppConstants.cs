namespace MyProject.Domain.Constants
{
    public static class AppConstants
    {
        public const string QuickBooksBaseUrl = "https://sandbox-quickbooks.api.intuit.com";
        public const string QuickBooksAuthUrl = "https://appcenter.intuit.com/connect/oauth2";
        public const string QuickBooksTokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
        public const string QuickBooksRevokeUrl = "https://developer.api.intuit.com/v2/oauth2/tokens/revoke";
        public const string QuickBooksUserInfoUrl = "https://sandbox-accounts.platform.intuit.com/v1/openid_connect/userinfo";
        public const string QuickBooksScope = "com.intuit.quickbooks.accounting openid profile email";
        public const int AccessTokenExpiryMinutes = 15;
        public const int RefreshTokenExpiryDays = 7;
    }
}