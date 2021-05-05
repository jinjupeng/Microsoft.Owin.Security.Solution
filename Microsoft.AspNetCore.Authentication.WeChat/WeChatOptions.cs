using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.WeChat
{
    /// <summary>
    /// Configuration options for <see cref="WeChatHandler"/>.
    /// </summary>
    public class WeChatOptions : OAuthOptions
    {
        /// <summary>
        /// Initializes a new <see cref="WeChatOptions"/>.
        /// </summary>
        public WeChatOptions()
        {
            ClaimsIssuer = WeChatDefaults.Issuer;
            CallbackPath = new PathString("/signin-wechat"); // 回调接口

            AuthorizationEndpoint = WeChatDefaults.AuthorizationEndpoint;
            TokenEndpoint = WeChatDefaults.TokenEndpoint;
            UserInformationEndpoint = WeChatDefaults.UserInformationEndpoint;

            StateAddition = "#wechat_redirect";
            Scope.Add("snsapi_login");
        }

        public string StateAddition { get; set; }

    }
}
