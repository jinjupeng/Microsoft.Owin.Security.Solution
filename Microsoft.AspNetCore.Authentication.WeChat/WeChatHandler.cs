using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.WeChat
{
    public class WeChatHandler : OAuthHandler<WeChatOptions>
    {
        public WeChatHandler(IOptionsMonitor<WeChatOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            var query = Request.Query;

            var error = query["error"];
            if (!StringValues.IsNullOrEmpty(error))
            {
                var failureMessage = new StringBuilder();
                failureMessage.Append(error);
                var errorDescription = query["error_description"];
                if (!StringValues.IsNullOrEmpty(errorDescription))
                {
                    failureMessage.Append(";Description=").Append(errorDescription);
                }
                var errorUri = query["error_uri"];
                if (!StringValues.IsNullOrEmpty(errorUri))
                {
                    failureMessage.Append(";Uri=").Append(errorUri);
                }
                return HandleRequestResult.Fail(failureMessage.ToString());
            }

            var code = query["code"];
            var state = query["state"];
            var oauthState = query["oauthstate"];

            AuthenticationProperties properties = Options.StateDataFormat.Unprotect(oauthState);

            if (state != Options.StateAddition || properties == null)
            {
                return HandleRequestResult.Fail("The oauth state was missing or invalid.");
            }

            // OAuth2 10.12 CSRF
            if (!ValidateCorrelationId(properties))
            {
                return HandleRequestResult.Fail("Correlation failed.");
            }

            if (StringValues.IsNullOrEmpty(code))
            {
                return HandleRequestResult.Fail("Code was not found.");
            }

            //获取tokens
            var tokens = await ExchangeCodeAsync(code, BuildRedirectUri(Options.CallbackPath));

            var identity = new ClaimsIdentity(Options.ClaimsIssuer);

            AuthenticationTicket ticket = null;
            if (Options.WeChatScope == Options.InfoScope || Options.WeChatScope == Options.LoginScope)
            {
                //获取用户信息
                ticket = await CreateTicketAsync(identity, properties, tokens);
            }
            else if(Options.WeChatScope == Options.BaseScope)
            {
                //不获取信息，只使用openid
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, tokens.TokenType, ClaimValueTypes.String, Options.ClaimsIssuer));
                ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Options.AuthenticationScheme);
            }

            if (ticket != null)
            {
                return HandleRequestResult.Success(ticket);
            }
            else
            {
                return HandleRequestResult.Fail("Failed to retrieve user information from remote server.");
            }
        }


        /// <summary>
        /// 扫码登录第一步：获取code值
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            // WeChat Identity Platform Manual:
            // https://cloud.tencent.com/developer/article/1447723
            // https://developers.weixin.qq.com/doc/oplatform/Website_App/WeChat_Login/Wechat_Login.html
            // https://www.jb51.net/article/91575.htm

            // 加密OAuth状态
            var state = Options.StateDataFormat.Protect(properties);

            var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "appid", Options.ClientId },
                { "redirect_uri", redirectUri },
                { "response_type", "code" },
                { "grant_type","authorization_code"},
                { "scope", Options.WeChatScope },
                { "state", $"{state}{Options.StateAddition}"}
            };

            var authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
            return authorizationEndpoint;
        }

        /// <summary>
        /// OAuth第二步,获取token
        /// </summary>
        /// <param name="code"></param>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {
            var tokenRequestParameters = new Dictionary<string, string>()
            {
                { "appid", Options.ClientId },
                { "secret", Options.ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            var response = await Backchannel.SendAsync(requestMessage, Context.RequestAborted);
            if (response.IsSuccessStatusCode)
            {
                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

                string ErrCode = payload.Value<string>("errcode");
                string ErrMsg = payload.Value<string>("errmsg");

                if (!string.IsNullOrEmpty(ErrCode) | !string.IsNullOrEmpty(ErrMsg))
                {
                    return OAuthTokenResponse.Failed(new Exception($"ErrCode:{ErrCode},ErrMsg:{ErrMsg}"));
                }

                var tokens = OAuthTokenResponse.Success(payload);

                //借用TokenType属性保存openid
                tokens.TokenType = payload.Value<string>("openid");

                return tokens;
            }
            else
            {
                var error = "OAuth token endpoint failure";
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        }

        ///// <summary>
        ///// 根据回调获取登录信息
        ///// </summary>
        ///// <param name="identity"></param>
        ///// <param name="properties"></param>
        ///// <param name="tokens"></param>
        ///// <returns></returns>
        //protected override async Task<AuthenticationTicket> CreateTicketAsync(
        //    ClaimsIdentity identity,
        //    AuthenticationProperties properties,
        //    OAuthTokenResponse tokens)
        //{
        //    // Get the WeChat user
        //    var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        //    var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new HttpRequestException($"An error occurred when retrieving WeChat user information ({response.StatusCode}). Please check if the authentication information is correct.");
        //    }

        //    var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
        //    var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload);
        //    context.RunClaimActions();
        //    await Events.CreatingTicket(context);
        //    return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);

        //    //using (var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        //    //{
        //    //    var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
        //    //    context.RunClaimActions();
        //    //    await Events.CreatingTicket(context);
        //    //    return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        //    //}
        //}

        /// <summary>
        /// OAuth第四步，获取用户信息
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="properties"></param>
        /// <param name="tokens"></param>
        /// <returns></returns>
        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            var queryBuilder = new QueryBuilder()
            {
                { "access_token", tokens.AccessToken },
                { "openid", tokens.TokenType },//在第二步中，openid被存入TokenType属性
                { "lang", "zh_CN" }
            };

            var infoRequest = $"{Options.UserInformationEndpoint}{queryBuilder}";
            var response = await Backchannel.GetAsync(infoRequest, Context.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve WeChat user information ({response.StatusCode}) Please check if the authentication information is correct and the corresponding WeChat Graph API is enabled.");
            }

            var user = JObject.Parse(await response.Content.ReadAsStringAsync());
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, user);

            var identifier = user.Value<string>("openid");
            if (!string.IsNullOrEmpty(identifier))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var nickname = user.Value<string>("nickname");
            if (!string.IsNullOrEmpty(nickname))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, nickname, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var sex = user.Value<string>("sex");
            if (!string.IsNullOrEmpty(sex))
            {
                identity.AddClaim(new Claim("urn:WeChat:sex", sex, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var country = user.Value<string>("country");
            if (!string.IsNullOrEmpty(country))
            {
                identity.AddClaim(new Claim(ClaimTypes.Country, country, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var province = user.Value<string>("province");
            if (!string.IsNullOrEmpty(province))
            {
                identity.AddClaim(new Claim(ClaimTypes.StateOrProvince, province, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var city = user.Value<string>("city");
            if (!string.IsNullOrEmpty(city))
            {
                identity.AddClaim(new Claim("urn:WeChat:city", city, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var headimgurl = user.Value<string>("headimgurl");
            if (!string.IsNullOrEmpty(headimgurl))
            {
                identity.AddClaim(new Claim("urn:WeChat:headimgurl", headimgurl, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var unionid = user.Value<string>("unionid");
            if (!string.IsNullOrEmpty(unionid))
            {
                identity.AddClaim(new Claim("urn:WeChat:unionid", unionid, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            context.RunClaimActions();
            await Options.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }

        private void AddQueryString<T>(
            IDictionary<string, string> queryStrings,
            AuthenticationProperties properties,
            string name,
            Func<T, string> formatter,
            T defaultValue)
        {
            string value = null;
            var parameterValue = properties.GetParameter<T>(name);
            if (parameterValue != null)
            {
                value = formatter(parameterValue);
            }
            else if (!properties.Items.TryGetValue(name, out value))
            {
                value = formatter(defaultValue);
            }

            // Remove the parameter from AuthenticationProperties so it won't be serialized into the state
            properties.Items.Remove(name);

            if (value != null)
            {
                queryStrings[name] = value;
            }
        }

        private void AddQueryString(
            IDictionary<string, string> queryStrings,
            AuthenticationProperties properties,
            string name,
            string defaultValue = null)
            => AddQueryString(queryStrings, properties, name, x => x, defaultValue);
    }
}
