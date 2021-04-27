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
            AuthenticationScheme = WeChatDefaults.AuthenticationScheme;
            CallbackPath = new PathString("/signin-wechat"); // 回调接口，可自定义
            AuthorizationEndpoint = WeChatDefaults.AuthorizationEndpoint;
            TokenEndpoint = WeChatDefaults.TokenEndpoint;
            UserInformationEndpoint = WeChatDefaults.UserInformationEndpoint;
            StateAddition = "#wechat_redirect";

            //BaseScope （不弹出授权页面，直接跳转，只能获取用户openid），
            //InfoScope （弹出授权页面，可通过openid拿到昵称、性别、所在地。并且，即使在未关注的情况下，只要用户授权，也能获取其信息）
            //LoginScope (PC端扫码登录)
            WeChatScope = InfoScope;
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

        public string StateAddition { get; set; }

        public string AuthenticationScheme { get; set; }

        public string WeChatScope { get; set; }

        public string BaseScope = "snsapi_base";

        public string InfoScope = "snsapi_userinfo";

        public string LoginScope = "snsapi_login";

        /*
         * snsapi_login，snsapi_base ，snsapi_userinfo 三者之间的区别
         * 后两者都是公众平台提供，只能在移动端使用的扫码获取用户信息，而只有 snsapi_login 权限能够在PC获取用户信息
         */

    }
}
