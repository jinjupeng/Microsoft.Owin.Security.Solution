#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0

using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication.QQ
{
    internal class QQConnectHandler : OAuthHandler<QQConnectOptions>
    {
        public QQConnectHandler(IOptionsMonitor<QQConnectOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }


        /// <summary>
        ///  Last Step
        /// </summary>
        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            // 获取用户OpenID
            var userOpenId = await ObtainUserOpenIdAsync(tokens);
            if (string.IsNullOrWhiteSpace(userOpenId))
            {
                throw new HttpRequestException("User openId was not found.");
            }

            // 获取用户基本信息
            var address = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, new Dictionary<string, string>
            {
                ["access_token"] = tokens.AccessToken,
                ["oauth_consumer_key"] = Options.ClientId,
                ["openid"] = userOpenId,
            });

            var response = await Backchannel.GetAsync(address);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                /* Status: */ response.StatusCode,
                                /* Headers: */ response.Headers.ToString(),
                                /* Body: */ await response.Content.ReadAsStringAsync());

                throw new HttpRequestException("An error occurred while retrieving user information.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using (var payload = JsonDocument.Parse(responseContent))
            {
                if (payload.RootElement.TryGetProperty("ret", out var result))
                {
                    if (result.GetInt32() != 0)
                    {
                        Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
                                        "returned a {Status} response with the following payload: {Headers} {Body}.",
                                        /* Status: */ response.StatusCode,
                                        /* Headers: */ response.Headers.ToString(),
                                        /* Body: */ responseContent);

                        throw new HttpRequestException("An error occurred while retrieving user information.");
                    }
                }

                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userOpenId, Options.ClaimsIssuer));
                identity.AddClaim(new Claim("urn:qq:openid", userOpenId, Options.ClaimsIssuer));
                identity.AddClaim(new Claim("urn:qq:user_info", responseContent, Options.ClaimsIssuer));

                var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);

                context.RunClaimActions();

                await Events.CreatingTicket(context);

                return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
            }
        }

        /// <summary>
        ///  Step2：通过Authorization Code获取Access Token
        ///  http://wiki.connect.qq.com/%E4%BD%BF%E7%94%A8authorization_code%E8%8E%B7%E5%8F%96access_token
        /// </summary>
        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            var address = QueryHelpers.AddQueryString(Options.TokenEndpoint, new Dictionary<string, string>()
            {
                ["client_id"] = Options.ClientId,
                ["client_secret"] = Options.ClientSecret,
                ["code"] = context.Code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = context.RedirectUri,
            });

            var response = await Backchannel.GetAsync(address);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving an access token: the remote server returned a {Status} response with the following payload: {Headers} {Body}.",
                                /* Status: */ response.StatusCode,
                                /* Headers: */ response.Headers.ToString(),
                                /* Body: */ await response.Content.ReadAsStringAsync());

                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
            }

            // 成功：  access_token=FE04************************CCE2&expires_in=7776000&refresh_token=88E4************************BE14
            // 失败：  callback( {"error":123456 ,"error_description":"**************"} );

            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString.StartsWith("callback"))
            {
                Logger.LogError("An error occurred while retrieving an access token: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                /* Status: */ response.StatusCode,
                                /* Headers: */ response.Headers.ToString(),
                                /* Body: */ await response.Content.ReadAsStringAsync());

                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
            }

            JsonDocument payload = JsonDocument.Parse("{}");

            var responseParams = responseString.Split('&');

            var buffer = new ArrayBufferWriter<byte>();
            var write = new Utf8JsonWriter(buffer);

            foreach (var parm in responseParams)
            {
                var kv = parm.Split('=');

                write.WritePropertyName(kv[0]);
                write.WriteStringValue(kv[1]);
            }

            return OAuthTokenResponse.Success(payload);
        }

        /// <summary>
        ///  Step3：通过Access Token获取OpenId
        /// </summary>
        /// <param name="tokens"></param>
        protected async Task<string> ObtainUserOpenIdAsync(OAuthTokenResponse tokens)
        {
            var address = QueryHelpers.AddQueryString(Options.OpenIdEndpoint, new Dictionary<string, string>
            {
                ["access_token"] = tokens.AccessToken,
            });

            var response = await Backchannel.GetAsync(address);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("An error occurred while retrieving the user open id: the remote server " +
                                "returned a {Status} response with the following payload: {Headers} {Body}.",
                                /* Status: */ response.StatusCode,
                                /* Headers: */ response.Headers.ToString(),
                                /* Body: */ await response.Content.ReadAsStringAsync());

                throw new HttpRequestException("An error occurred while retrieving user information.");
            }

            string responseString = await response.Content.ReadAsStringAsync();

            // callback( {"client_id":"YOUR_APPID","openid":"YOUR_OPENID"} );\n

            responseString = responseString.Remove(0, 9);
            responseString = responseString.Remove(responseString.Length - 3);

            JsonDocument document = JsonDocument.Parse(responseString);

            return document.RootElement.GetProperty("openid").GetString();
        }

        protected override string FormatScope()
        {
            return string.Join(",", Options.Scope);
        }

        /// <summary>
        ///  Step1：获取Authorization Code
        ///  构建请求地址
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var url = base.BuildChallengeUrl(properties, redirectUri);
            return url;
        }

    }
}

#endif

#if NETSTANDARD2_0

using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Microsoft.AspNetCore.Authentication.QQ
{
	/// <summary>
	/// </summary>
	internal class QQConnectHandler : OAuthHandler<QQConnectOptions>
	{
		public QQConnectHandler(IOptionsMonitor<QQConnectOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
		{
		}

		/// <summary>
		///  Last Step
		/// </summary>
		protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
		{
			// 获取用户OpenID
			var userOpenId = await ObtainUserOpenIdAsync(tokens);
			if (string.IsNullOrWhiteSpace(userOpenId))
			{
				throw new HttpRequestException("User openId was not found.");
			}

			// 获取用户基本信息
			var address = QueryHelpers.AddQueryString(Options.UserInformationEndpoint, new Dictionary<string, string>
			{
				["access_token"] = tokens.AccessToken,
				["oauth_consumer_key"] = Options.ClientId,
				["openid"] = userOpenId,
			});

			var response = await Backchannel.GetAsync(address);
			if (!response.IsSuccessStatusCode)
			{
				Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
								"returned a {Status} response with the following payload: {Headers} {Body}.",
								/* Status: */ response.StatusCode,
								/* Headers: */ response.Headers.ToString(),
								/* Body: */ await response.Content.ReadAsStringAsync());

				throw new HttpRequestException("An error occurred while retrieving user information.");
			}

			var user = JObject.Parse(await response.Content.ReadAsStringAsync());
			if (user.Value<int>("ret") != 0)
			{
				Logger.LogError("An error occurred while retrieving the user profile: the remote server " +
								"returned a {Status} response with the following payload: {Headers} {Body}.",
								/* Status: */ response.StatusCode,
								/* Headers: */ response.Headers.ToString(),
								/* Body: */ await response.Content.ReadAsStringAsync());

				throw new HttpRequestException("An error occurred while retrieving user information.");
			}
			if (!string.IsNullOrEmpty(userOpenId))
			{
				identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userOpenId, ClaimValueTypes.String, Options.ClaimsIssuer));
				identity.AddClaim(new Claim("urn:qq:openid", userOpenId, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var nickname = user.Value<string>("nickname");
			if (!string.IsNullOrEmpty(nickname))
			{
				identity.AddClaim(new Claim(ClaimTypes.Name, nickname, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var sex = user.Value<string>("sex");
			if (!string.IsNullOrEmpty(sex))
			{
				identity.AddClaim(new Claim("urn:qq:sex", sex, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var figureurl = user.Value<string>("figureurl");
			if (!string.IsNullOrEmpty(figureurl))
			{
				identity.AddClaim(new Claim("urn:qq:figureurl", figureurl, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var figureurl_1 = user.Value<string>("figureurl_1");
			if (!string.IsNullOrEmpty(figureurl_1))
			{
				identity.AddClaim(new Claim("urn:qq:figureurl_1", figureurl_1, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var figureurl_2 = user.Value<string>("figureurl_2");
			if (!string.IsNullOrEmpty(figureurl_2))
			{
				identity.AddClaim(new Claim("urn:qq:figureurl_2", figureurl_2, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var figureurl_qq_1 = user.Value<string>("figureurl_qq_1");
			if (!string.IsNullOrEmpty(figureurl_qq_1))
			{
				identity.AddClaim(new Claim("urn:qq:figureurl_qq_1", figureurl_qq_1, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var figureurl_qq_2 = user.Value<string>("figureurl_qq_2");
			if (!string.IsNullOrEmpty(figureurl_qq_2))
			{
				identity.AddClaim(new Claim("urn:qq:figureurl_qq_2", figureurl_qq_2, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var gender = user.Value<string>("gender");
			if (!string.IsNullOrEmpty(gender))
			{
				identity.AddClaim(new Claim("urn:qq:gender", gender, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var is_yellow_vip = user.Value<string>("is_yellow_vip");
			if (!string.IsNullOrEmpty(is_yellow_vip))
			{
				identity.AddClaim(new Claim("urn:qq:is_yellow_vip", is_yellow_vip, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var vip = user.Value<string>("vip");
			if (!string.IsNullOrEmpty(vip))
			{
				identity.AddClaim(new Claim("urn:qq:vip", vip, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var yellow_vip_level = user.Value<string>("yellow_vip_level");
			if (!string.IsNullOrEmpty(yellow_vip_level))
			{
				identity.AddClaim(new Claim("urn:qq:yellow_vip_level", yellow_vip_level, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var level = user.Value<string>("level");
			if (!string.IsNullOrEmpty(level))
			{
				identity.AddClaim(new Claim("urn:qq:level", level, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			var is_yellow_year_vip = user.Value<string>("is_yellow_year_vip");
			if (!string.IsNullOrEmpty(is_yellow_year_vip))
			{
				identity.AddClaim(new Claim("urn:qq:is_yellow_year_vip", is_yellow_year_vip, ClaimValueTypes.String, Options.ClaimsIssuer));
			}

			identity.AddClaim(new Claim("urn:qq:user_info", user.ToString(), ClaimValueTypes.String, Options.ClaimsIssuer));
			
			var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, user);
			context.RunClaimActions();

			await Events.CreatingTicket(context);

			return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
		}

		/// <summary>
		///  Step2：通过Authorization Code获取Access Token
		///  http://wiki.connect.qq.com/%E4%BD%BF%E7%94%A8authorization_code%E8%8E%B7%E5%8F%96access_token
		/// </summary>
		protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
		{
			var address = QueryHelpers.AddQueryString(Options.TokenEndpoint, new Dictionary<string, string>()
			{
				["client_id"] = Options.ClientId,
				["client_secret"] = Options.ClientSecret,
				["code"] = code,
				["grant_type"] = "authorization_code",
				["redirect_uri"] = redirectUri,
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

			// 成功：  access_token=FE04************************CCE2&expires_in=7776000&refresh_token=88E4************************BE14
			// 失败：  callback( {"error":123456 ,"error_description":"**************"} );

			var responseString = await response.Content.ReadAsStringAsync();

			if (responseString.StartsWith("callback"))
			{
				Logger.LogError("An error occurred while retrieving an access token: the remote server " +
								"returned a {Status} response with the following payload: {Headers} {Body}.",
								/* Status: */ response.StatusCode,
								/* Headers: */ response.Headers.ToString(),
								/* Body: */ await response.Content.ReadAsStringAsync());

				return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
			}

			JObject payload = new JObject();

			var responseParams = responseString.Split('&');

			foreach (var parm in responseParams)
			{
				var kv = parm.Split('=');

				payload[kv[0]] = kv[1];
			}

			return OAuthTokenResponse.Success(payload);
		}

		/// <summary>
		///  Step3：通过Access Token获取OpenId
		/// </summary>
		/// <param name="tokens"></param>
		protected async Task<string> ObtainUserOpenIdAsync(OAuthTokenResponse tokens)
		{
			var address = QueryHelpers.AddQueryString(Options.OpenIdEndpoint, new Dictionary<string, string>
			{
				["access_token"] = tokens.AccessToken,
			});

			var response = await Backchannel.GetAsync(address);
			if (!response.IsSuccessStatusCode)
			{
				Logger.LogError("An error occurred while retrieving the user open id: the remote server " +
								"returned a {Status} response with the following payload: {Headers} {Body}.",
								/* Status: */ response.StatusCode,
								/* Headers: */ response.Headers.ToString(),
								/* Body: */ await response.Content.ReadAsStringAsync());

				throw new HttpRequestException("An error occurred while retrieving user information.");
			}

			string responseString = await response.Content.ReadAsStringAsync();

			// callback( {"client_id":"YOUR_APPID","openid":"YOUR_OPENID"} );\n

			responseString = responseString.Remove(0, 9);
			responseString = responseString.Remove(responseString.Length - 3);

			JObject oauth2Token = JObject.Parse(responseString);

			return oauth2Token.Value<string>("openid");
		}

		protected override string FormatScope()
		{
			return string.Join(",", Options.Scope);
		}

		/// <summary>
		///  Step1：获取Authorization Code
		///  构建请求地址
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="redirectUri"></param>
		/// <returns></returns>
		protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
		{
			var url = base.BuildChallengeUrl(properties, redirectUri);
			return url;
		}
	}
}

#endif
