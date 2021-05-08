using System;

namespace Microsoft.AspNetCore.Authentication.QQ
{
	/// <summary>
	/// Default values for QQ authentication
	/// </summary>
	public class QQConnectDefaults
    {
		/// <summary>
		/// Default value for <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
		/// </summary>
		public const string AuthenticationScheme = "QQ";

		public static readonly string DisplayName = "QQ";

		/// <summary>
		/// Default value for <see cref="AuthenticationSchemeOptions.ClaimsIssuer"/>.
		/// </summary>
		public const string Issuer = "QQ";

		/// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.AuthorizationEndpoint"/>.
		/// </summary>
		public const string AuthorizationEndpoint = "https://graph.qq.com/oauth2.0/authorize";

		/// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.TokenEndpoint"/>.
		/// </summary>
		public const string TokenEndpoint = "https://graph.qq.com/oauth2.0/token";

		public const string UserOpenIdEndpoint = "https://graph.qq.com/oauth2.0/me";

		/// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.UserInformationEndpoint"/>.
		/// </summary>
		public const string UserInformationEndpoint = "https://graph.qq.com/user/get_user_info";
	}
}
