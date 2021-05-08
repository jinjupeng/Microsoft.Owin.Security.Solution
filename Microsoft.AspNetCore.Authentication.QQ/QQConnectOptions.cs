using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.QQ
{
    /// <summary>
    /// Configuration options for <see cref="QQConnectHandler"/>.
    /// </summary>
    public class QQConnectOptions : OAuthOptions
    {
        /// <summary>
        /// Initializes a new <see cref="QQConnectOptions"/>.
        /// </summary>
        public QQConnectOptions()
        {
            ClaimsIssuer = QQConnectDefaults.Issuer;
            CallbackPath = new PathString("/signin-qq"); // 回调接口

            AuthorizationEndpoint = QQConnectDefaults.AuthorizationEndpoint;
            TokenEndpoint = QQConnectDefaults.TokenEndpoint;
            OpenIdEndpoint = QQConnectDefaults.UserOpenIdEndpoint;
            UserInformationEndpoint = QQConnectDefaults.UserInformationEndpoint;
        }

        public string OpenIdEndpoint { get; set; }
    }
}
