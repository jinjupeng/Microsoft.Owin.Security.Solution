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
            CallbackPath = new PathString("/signin-wechat"); // 回调接口，可自定义
            AuthorizationEndpoint = WeChatDefaults.AuthorizationEndpoint;
            TokenEndpoint = WeChatDefaults.TokenEndpoint;
            UserInformationEndpoint = WeChatDefaults.UserInformationEndpoint;
            Scope.Add("snsapi_login"); // 微信暂时只支持这一种

            ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
            ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
            ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
            ClaimActions.MapJsonKey("urn:WeChat:profile", "link");
            ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        }

        /// <summary>
        /// access_type. Set to 'offline' to request a refresh token.
        /// </summary>
        public string AccessType { get; set; }

        public string OpenIdEndpoint { get; }

        public string AppId
        {
            get { return ClientId; }
            set { ClientId = value; }
        }

        public string AppKey
        {
            get { return ClientSecret; }
            set { ClientSecret = value; }
        }
    }
}
