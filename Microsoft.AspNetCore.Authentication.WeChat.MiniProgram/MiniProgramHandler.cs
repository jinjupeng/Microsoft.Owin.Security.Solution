using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.WeChat.MiniProgram
{
    /// <summary>
    /// 微信小程序登录处理类
    /// </summary>
    public class MiniProgramHandler : AuthenticationHandler<MiniProgramOptions>, IAuthenticationRequestHandler
    {
        private HttpClient Backchannel => Options.Backchannel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="encoder"></param>
        /// <param name="clock"></param>
        public MiniProgramHandler(IOptionsMonitor<MiniProgramOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        /// <summary>
        /// 实现IAuthenticationRequestHandler，若返回true则阻止后续中间件的执行，直接响应用户请求
        /// </summary>
        /// <returns></returns>
        public async Task<bool> HandleRequestAsync()
        {
            if ((Options.CallbackPath != Request.Path))
                return false;

            var handler = Context.RequestServices.GetRequiredService<IMiniProgramLoginHandler>();
            //既然已经注册了这个身份验证方案，就必须要实现
            if (handler == null)
                throw new Exception($"请实现{nameof(IMiniProgramLoginHandler)}，否则不要注册微信小程序登录的身份验证方案");

            //在身份验证的多个步骤触发相应的事件，允许调用方控制身份验证过程，可以参考asp.net core默认实现，如：TwitterHandler
            //Options.Events.CreatingTicket(ct);

            var authResult = await HandleRemoteAuthenticateAsync();
            return await handler.ExcuteAsync(new MiniProgramLoginContext(base.Context, Options, authResult));
        }

        /// <summary>
        /// 从请求中获取用户上传信息
        /// 根据上传信息中的code向微信服务器发起请求，获取openid、session_key....
        /// </summary>
        /// <returns></returns>
        private async Task<MiniProgramUser> HandleRemoteAuthenticateAsync()
        {
            var input = await new StreamReader(Request.Body).ReadToEndAsync();
            var jsonDoc = JsonDocument.Parse(input).RootElement;
            var code = jsonDoc.GetString("code");

            var requestUrl = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, new Dictionary<string, string>
            {
                { "appid", Options.AppId },
                { "secret", Options.Secret },
                { "js_code",code },
                { "grant_type", "authorization_code" },
            });
            var response = await Backchannel.GetAsync(requestUrl, Context.RequestAborted);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"An error occurred when retrieving wechar mini program user information ({response.StatusCode}). Please check if the authentication information is correct and the corresponding Microsoft Account API is enabled.");

            var token = JsonSerializer.Deserialize<MiniProgramToken>(await response.Content.ReadAsStringAsync());

            ////调试
            //var token = new MiniProgramToken
            //{
            //    errcode = 0,
            //    errmsg = "",
            //    openid = "xxx",
            //    session_key = "xxx",
            //    unionid = ""
            //};

            if (token.errcode != 0)
                throw new HttpRequestException($"errcode:{token.errcode}, errmsg:{token.errmsg}");

            return new MiniProgramUser
            {
                Input = jsonDoc,
                openid = token.openid,
                session_key = token.session_key,
                unionid = token.unionid
            };
        }

        /// <summary>
        /// 此方法的主要职责是从当前请求获取用户信息
        /// 本类中暂不实现，因此尝试使用类似HttpContext.SignInAsync("微信小程序登录");的方法时将引发异常
        /// </summary>
        /// <returns></returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            throw new NotImplementedException();
        }
    }
}
