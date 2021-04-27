using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Authentication.WeChat
{
    public class WeChatHandler : OAuthHandler<WeChatOptions>
    {
        public WeChatHandler(IOptionsMonitor<WeChatOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            // Get the WeChat user
            var request = new HttpRequestMessage(HttpMethod.Get, Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            var response = await Backchannel.SendAsync(request, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"An error occurred when retrieving WeChat user information ({response.StatusCode}). Please check if the authentication information is correct.");
            }

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload);
            context.RunClaimActions();
            await Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);

            //using (var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
            //{
            //    var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload.RootElement);
            //    context.RunClaimActions();
            //    await Events.CreatingTicket(context);
            //    return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
            //}
        }

        // TODO: Abstract this properties override pattern into the base class?
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            // WeChat Identity Platform Manual:
            // https://cloud.tencent.com/developer/article/1447723


            var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "response_type", "code" },
                { "client_id", Options.ClientId },
                { "client_secret", Options.ClientSecret},
                { "grant_type","authorization_code"},
                { "redirect_uri", redirectUri }
            };

            AddQueryString(queryStrings, properties, WeChatChallengeProperties.ScopeKey, FormatScope, Options.Scope);
            AddQueryString(queryStrings, properties, WeChatChallengeProperties.AccessTypeKey, Options.AccessType);
            AddQueryString(queryStrings, properties, WeChatChallengeProperties.ApprovalPromptKey);
            AddQueryString(queryStrings, properties, WeChatChallengeProperties.PromptParameterKey);
            AddQueryString(queryStrings, properties, WeChatChallengeProperties.LoginHintKey);
            AddQueryString(queryStrings, properties, WeChatChallengeProperties.IncludeGrantedScopesKey, v => v?.ToString().ToLower(), (bool?)null);

            var state = Options.StateDataFormat.Protect(properties);
            queryStrings.Add("state", state);

            var authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
            return authorizationEndpoint;
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
