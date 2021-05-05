

namespace Microsoft.AspNetCore.Authentication.WeChat
{
    /// <summary>
    /// Default values for WeChat authentication
    /// </summary>
    public static class WeChatDefaults
    {
        /// <summary>
		/// Default value for <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
		/// </summary>
        public const string AuthenticationScheme = "WeChat";

        public static readonly string DisplayName = "WeChat";

		/// <summary>
		/// Default value for <see cref="AuthenticationSchemeOptions.ClaimsIssuer"/>.
		/// </summary>
		public const string Issuer = "WeChat";

		/// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.AuthorizationEndpoint"/>.
		/// </summary>
		public const string AuthorizationEndpoint = "https://open.weixin.qq.com/connect/qrconnect";

		/// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.TokenEndpoint"/>.
		/// </summary>
		public const string TokenEndpoint = "https://api.weixin.qq.com/sns/oauth2/access_token";

		/// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.UserInformationEndpoint"/>.
		/// </summary>
		public const string UserInformationEndpoint = "https://api.weixin.qq.com/sns/userinfo";
	}
}
