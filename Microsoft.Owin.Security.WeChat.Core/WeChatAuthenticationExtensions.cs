using System;
using Microsoft.Owin.Security.WeChat.Core;
using Microsoft.Owin.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Owin
{
    public static class WeChatAuthenticationExtensions
    {
        public static IApplicationBuilder UseWeChatAuthentication(this IApplicationBuilder app, WeChatAuthenticationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            return app.UseMiddleware<WeChatAuthenticationMiddleware>();
            // app.Use(typeof(WeChatAuthenticationMiddleware), app, options);
        }

        public static IApplicationBuilder UseWeChatAuthentication(this IApplicationBuilder app, string appId, string appSecret)
        {
            return UseWeChatAuthentication(app, new WeChatAuthenticationOptions()
            {
                AppId = appId,
                AppSecret = appSecret,
                //SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType()
            });
        }
    }
}
