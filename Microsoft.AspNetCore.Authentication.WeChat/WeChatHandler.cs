//#if NETSTANDARD2_1

//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Security.Claims;
//using System.Text.Encodings.Web;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authentication.OAuth;
//using Microsoft.AspNetCore.WebUtilities;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json.Linq;

//namespace Microsoft.AspNetCore.Authentication.WeChat
//{
//    /// <summary>
//    /// 支持OAuth2的认证处理器的实现
//    /// </summary>
//    internal class WeChatHandler : OAuthHandler<WeChatOptions>
//    {
//        public WeChatHandler(IOptionsMonitor<WeChatOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
//            : base(options, logger, encoder, clock)
//        { }


//        /// <summary>
//        /// OAuth第四步，获取用户信息
//        /// </summary>
//        /// <param name="identity"></param>
//        /// <param name="properties"></param>
//        /// <param name="tokens"></param>
//        /// <returns></returns>
//        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
//        {
//            var address = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, new Dictionary<string, string>
//            {
//                ["access_token"] = tokens.AccessToken,
//                ["openid"] = tokens.Response.Value<string>("openid"),
//                ["lang"] = "zh_CN"
//            });

//            var response = await Backchannel.GetAsync(address);

//            if (!response.IsSuccessStatusCode)
//            {
//                throw new HttpRequestException($"Failed to retrieve WeChat user information ({response.StatusCode}) Please check if the authentication information is correct and the corresponding WeChat Graph API is enabled.");
//            }

//            var user = JObject.Parse(await response.Content.ReadAsStringAsync());
//            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, user);

//            var identifier = user.Value<string>("openid");
//            if (!string.IsNullOrEmpty(identifier))
//            {
//                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var nickname = user.Value<string>("nickname");
//            if (!string.IsNullOrEmpty(nickname))
//            {
//                identity.AddClaim(new Claim(ClaimTypes.Name, nickname, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var sex = user.Value<string>("sex");
//            if (!string.IsNullOrEmpty(sex))
//            {
//                identity.AddClaim(new Claim("urn:WeChat:sex", sex, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var country = user.Value<string>("country");
//            if (!string.IsNullOrEmpty(country))
//            {
//                identity.AddClaim(new Claim(ClaimTypes.Country, country, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var province = user.Value<string>("province");
//            if (!string.IsNullOrEmpty(province))
//            {
//                identity.AddClaim(new Claim(ClaimTypes.StateOrProvince, province, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var city = user.Value<string>("city");
//            if (!string.IsNullOrEmpty(city))
//            {
//                identity.AddClaim(new Claim("urn:WeChat:city", city, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var headimgurl = user.Value<string>("headimgurl");
//            if (!string.IsNullOrEmpty(headimgurl))
//            {
//                identity.AddClaim(new Claim("urn:WeChat:headimgurl", headimgurl, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            var unionid = user.Value<string>("unionid");
//            if (!string.IsNullOrEmpty(unionid))
//            {
//                identity.AddClaim(new Claim("urn:WeChat:unionid", unionid, ClaimValueTypes.String, Options.ClaimsIssuer));
//            }

//            context.RunClaimActions();
//            await Options.Events.CreatingTicket(context);
//            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
//        }

//        /// <summary>
//        /// OAuth第二步,通过code获取access_token
//        /// </summary>
//        /// <param name="code"></param>
//        /// <param name="redirectUri"></param>
//        /// <returns></returns>
//        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
//        {
//            var address = QueryHelpers.AddQueryString(Options.TokenEndpoint, new Dictionary<string, string>()
//            {
//                ["appid"] = Options.ClientId,
//                ["secret"] = Options.ClientSecret,
//                ["code"] = code,
//                ["grant_type"] = "authorization_code"
//            });

//            var response = await Backchannel.GetAsync(address);
//            if (!response.IsSuccessStatusCode)
//            {
//                Logger.LogError("An error occurred while retrieving an access token: the remote server " +
//                                "returned a {Status} response with the following payload: {Headers} {Body}.",
//                                /* Status: */ response.StatusCode,
//                                /* Headers: */ response.Headers.ToString(),
//                                /* Body: */ await response.Content.ReadAsStringAsync());

//                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
//            }

//            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
//            if (!string.IsNullOrEmpty(payload.Value<string>("errcode")))
//            {
//                Logger.LogError("An error occurred while retrieving an access token: the remote server " +
//                                "returned a {Status} response with the following payload: {Headers} {Body}.",
//                                /* Status: */ response.StatusCode,
//                                /* Headers: */ response.Headers.ToString(),
//                                /* Body: */ await response.Content.ReadAsStringAsync());

//                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
//            }
//            return OAuthTokenResponse.Success(payload);
//        }

//        /// <summary>
//        /// 扫码登录第一步：获取code值
//        /// 构建用户授权地址
//        /// </summary>
//        /// <param name="properties"></param>
//        /// <param name="redirectUri"></param>
//        /// <returns></returns>
//        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
//        {
//            // 加密OAuth状态
//            var state = Options.StateDataFormat.Protect(properties);

//            var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
//            {
//                { "appid", Options.ClientId },
//                { "redirect_uri", redirectUri },
//                { "response_type", "code" },
//                { "scope", FormatScope() },
//                { "state", $"{state}{Options.StateAddition}"}
//            };

//            var authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
//            return authorizationEndpoint;
//        }

//        protected override string FormatScope()
//        {
//            return string.Join(",", Options.Scope);
//        }
//    }
//}

//#endif


#if NETCOREAPP3_0 || NETCOREAPP3_1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.WeChat
{
	internal class WeChatHandler : OAuthHandler<WeChatOptions>
	{
		public WeChatHandler(IOptionsMonitor<WeChatOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
		{
		}

        /// <summary>
        /// 最后一步，获取用户信息
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="properties"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
		{
            var address = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, new Dictionary<string, string>
            {
                ["access_token"] = tokens.AccessToken,
                ["openid"] = tokens.Response.RootElement.GetProperty("openid").GetString(),
                ["lang"] = "zh_CN"
            });

            var response = await Backchannel.GetAsync(address);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve WeChat user information ({response.StatusCode}) Please check if the authentication information is correct and the corresponding WeChat Graph API is enabled.");
            }

            var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, user.RootElement);

            var identifier = user.RootElement.GetString("openid");
            if (!string.IsNullOrEmpty(identifier))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var nickname = user.RootElement.GetString("nickname");
            if (!string.IsNullOrEmpty(nickname))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, nickname, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var sex = user.RootElement.GetString("sex");
            if (!string.IsNullOrEmpty(sex))
            {
                identity.AddClaim(new Claim("urn:WeChat:sex", sex, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var country = user.RootElement.GetString("country");
            if (!string.IsNullOrEmpty(country))
            {
                identity.AddClaim(new Claim(ClaimTypes.Country, country, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var province = user.RootElement.GetString("province");
            if (!string.IsNullOrEmpty(province))
            {
                identity.AddClaim(new Claim(ClaimTypes.StateOrProvince, province, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var city = user.RootElement.GetString("city");
            if (!string.IsNullOrEmpty(city))
            {
                identity.AddClaim(new Claim("urn:WeChat:city", city, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var headimgurl = user.RootElement.GetString("headimgurl");
            if (!string.IsNullOrEmpty(headimgurl))
            {
                identity.AddClaim(new Claim("urn:WeChat:headimgurl", headimgurl, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var unionid = user.RootElement.GetString("unionid");
            if (!string.IsNullOrEmpty(unionid))
            {
                identity.AddClaim(new Claim("urn:WeChat:unionid", unionid, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            context.RunClaimActions();
            await Options.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
		}

		/// <summary>
		/// Step 2：通过code获取access_token
		/// </summary> 
		protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
		{
			var address = QueryHelpers.AddQueryString(Options.TokenEndpoint, new Dictionary<string, string>()
			{
				["appid"] = Options.ClientId,
				["secret"] = Options.ClientSecret,
				["code"] = context.Code,
				["grant_type"] = "authorization_code"
			});

			var response = await Backchannel.GetAsync(address);
			if (!response.IsSuccessStatusCode)
			{
				Logger.LogError("An error occurred while retrieving an access token: the remote server " +
								"returned a {Status} response with the following payload: {Headers} {Body}.",
								/* Status: */ response.StatusCode,
								/* Headers: */ response.Headers.ToString(),
								/* Body: */ await response.Content.ReadAsStringAsync());

				return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
			}

			var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
			if (!string.IsNullOrEmpty(payload.RootElement.GetString("errcode")))
			{
				Logger.LogError("An error occurred while retrieving an access token: the remote server returned a {Status} response with the following payload: {Headers} {Body}.",
								/* Status: */ response.StatusCode,
								/* Headers: */ response.Headers.ToString(),
								/* Body: */ await response.Content.ReadAsStringAsync());

				return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
			}
			return OAuthTokenResponse.Success(payload);
		}

		/// <summary>
		///  Step 1：请求CODE 
		///  构建用户授权地址
		/// </summary> 
		protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
		{
			return QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, new Dictionary<string, string>
			{
				["appid"] = Options.ClientId,
				["redirect_uri"] = redirectUri,
				["response_type"] = "code",
				["scope"] = FormatScope(),
				["state"] = Options.StateDataFormat.Protect(properties)
			});
		}

		protected override string FormatScope()
		{
			return string.Join(",", Options.Scope);
		}

	}
}

#endif