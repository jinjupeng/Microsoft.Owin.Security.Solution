using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.WeChat;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenApiTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o => o.LoginPath = new PathString("/login"))

                // Î¢ÐÅÉ¨ÂëµÇÂ¼
                .AddWeChat(o =>
                {
                    o.ClientId = Configuration["WeChat:AppId"];
                    o.ClientSecret = Configuration["WeChat:AppSecret"];
                    o.SaveTokens = true;
                    o.Events = new OAuthEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                // GitHubµÇÂ¼
               .AddOAuth("GitHub", options =>
               {
                   options.ClientId = Configuration["GitHub:ClientId"];
                   options.ClientSecret = Configuration["GitHub:ClientSecret"];
                   options.CallbackPath = new PathString("/github-oauth");

                   options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                   options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                   options.UserInformationEndpoint = "https://api.github.com/user";

                   options.SaveTokens = true;

                   options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                   options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                   options.ClaimActions.MapJsonKey("urn:github:login", "login");
                   options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
                   options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

                   options.Events = new OAuthEvents
                   {
                       OnCreatingTicket = async context =>
                       {
                           var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                           request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                           var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                           response.EnsureSuccessStatusCode();

                           var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                           context.RunClaimActions(json.RootElement);
                       }
                   };
               });
            services.AddControllers();
        }

        private async Task HandleOnRemoteFailure(RemoteFailureContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<html><body>");
            await context.Response.WriteAsync("A remote failure has occurred: <br>" +
                context.Failure.Message.Split(Environment.NewLine).Select(s => HtmlEncoder.Default.Encode(s) + "<br>").Aggregate((s1, s2) => s1 + s2));

            if (context.Properties != null)
            {
                await context.Response.WriteAsync("Properties:<br>");
                foreach (var pair in context.Properties.Items)
                {
                    await context.Response.WriteAsync($"-{ HtmlEncoder.Default.Encode(pair.Key)}={ HtmlEncoder.Default.Encode(pair.Value)}<br>");
                }
            }

            await context.Response.WriteAsync("<a href=\"/\">Home</a>");
            await context.Response.WriteAsync("</body></html>");

            //context.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(context.Failure.Message));

            context.HandleResponse();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseHsts();

            app.UseAuthentication();

            app.Map("/login", signinApp =>
            {
                signinApp.Run(async context =>
                {
                    var authType = context.Request.Query["authscheme"];
                    if (!string.IsNullOrEmpty(authType))
                    {
                        // By default the client will be redirect back to the URL that issued the challenge (/login?authtype=foo),
                        // send them to the home page instead (/).
                        await context.ChallengeAsync(authType, new AuthenticationProperties() { RedirectUri = "/" });
                        return;
                    }

                    var response = context.Response;
                    response.ContentType = "text/html; charset=utf-8";
                    await response.WriteAsync("<html><body>");
                    await response.WriteAsync("Choose an authentication scheme: <br>");
                    var schemeProvider = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
                    foreach (var provider in await schemeProvider.GetAllSchemesAsync())
                    {
                        await response.WriteAsync("<a href=\"?authscheme=" + provider.Name + "\">" + (provider.DisplayName ?? "(suppressed)") + "</a><br>");
                    }
                    await response.WriteAsync("</body></html>");
                });
            });

            // Refresh the access token
            app.Map("/refresh_token", signinApp =>
            {
                signinApp.Run(async context =>
                {
                    var response = context.Response;

                    // Setting DefaultAuthenticateScheme causes User to be set
                    // var user = context.User;

                    // This is what [Authorize] calls
                    var userResult = await context.AuthenticateAsync();
                    var user = userResult.Principal;
                    var authProperties = userResult.Properties;

                    // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                    // var user = await context.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                    // Deny anonymous request beyond this point.
                    if (!userResult.Succeeded || user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                    {
                        // This is what [Authorize] calls
                        // The cookie middleware will handle this and redirect to /login
                        await context.ChallengeAsync();

                        // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                        // await context.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                        return;
                    }

                    var currentAuthType = user.Identities.First().AuthenticationType;
                    if (string.Equals(WeChatDefaults.AuthenticationScheme, currentAuthType))
                    {
                        var refreshToken = authProperties.GetTokenValue("refresh_token");

                        if (string.IsNullOrEmpty(refreshToken))
                        {
                            response.ContentType = "text/html; charset=utf-8";
                            await response.WriteAsync("<html><body>");
                            await response.WriteAsync("No refresh_token is available.<br>");
                            await response.WriteAsync("<a href=\"/\">Home</a>");
                            await response.WriteAsync("</body></html>");
                            return;
                        }

                        var options = await GetOAuthOptionsAsync(context, currentAuthType);

                        var pairs = new Dictionary<string, string>()
                        {
                            { "client_id", options.ClientId },
                            { "client_secret", options.ClientSecret },
                            { "grant_type", "refresh_token" },
                            { "refresh_token", refreshToken }
                        };
                        var content = new FormUrlEncodedContent(pairs);
                        var refreshResponse = await options.Backchannel.PostAsync(options.TokenEndpoint, content, context.RequestAborted);
                        refreshResponse.EnsureSuccessStatusCode();

                        using (var payload = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync()))
                        {

                            // Persist the new acess token
                            authProperties.UpdateTokenValue("access_token", payload.RootElement.GetString("access_token"));
                            refreshToken = payload.RootElement.GetString("refresh_token");
                            if (!string.IsNullOrEmpty(refreshToken))
                            {
                                authProperties.UpdateTokenValue("refresh_token", refreshToken);
                            }
                            if (payload.RootElement.TryGetProperty("expires_in", out var property) && property.TryGetInt32(out var seconds))
                            {
                                var expiresAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(seconds);
                                authProperties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));
                            }
                            await context.SignInAsync(user, authProperties);

                            await PrintRefreshedTokensAsync(response, payload, authProperties);
                        }
                        return;
                    }

                    response.ContentType = "text/html; charset=utf-8";
                    await response.WriteAsync("<html><body>");
                    await response.WriteAsync("Refresh has not been implemented for this provider.<br>");
                    await response.WriteAsync("<a href=\"/\">Home</a>");
                    await response.WriteAsync("</body></html>");
                });
            });

            // Sign-out to remove the user cookie.
            app.Map("/logout", signoutApp =>
            {
                signoutApp.Run(async context =>
                {
                    var response = context.Response;
                    response.ContentType = "text/html; charset=utf-8";
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    await response.WriteAsync("<html><body>");
                    await response.WriteAsync("You have been logged out. Goodbye " + context.User.Identity.Name + "<br>");
                    await response.WriteAsync("<a href=\"/\">Home</a>");
                    await response.WriteAsync("</body></html>");
                });
            });

            // Display the remote error
            app.Map("/error", errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var response = context.Response;
                    response.ContentType = "text/html; charset=utf-8";
                    await response.WriteAsync("<html><body>");
                    await response.WriteAsync("An remote failure has occurred: " + context.Request.Query["FailureMessage"] + "<br>");
                    await response.WriteAsync("<a href=\"/\">Home</a>");
                    await response.WriteAsync("</body></html>");
                });
            });


            app.Run(async context =>
            {
                // Setting DefaultAuthenticateScheme causes User to be set
                var user = context.User;

                // This is what [Authorize] calls
                // var user = await context.AuthenticateAsync();

                // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                // var user = await context.AuthenticateAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                // Deny anonymous request beyond this point.
                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    // This is what [Authorize] calls
                    // The cookie middleware will handle this and redirect to /login
                    await context.ChallengeAsync();

                    // This is what [Authorize(ActiveAuthenticationSchemes = MicrosoftAccountDefaults.AuthenticationScheme)] calls
                    // await context.ChallengeAsync(MicrosoftAccountDefaults.AuthenticationScheme);

                    return;
                }

                // Display user information
                var response = context.Response;
                response.ContentType = "text/html; charset=utf-8";
                await response.WriteAsync("<html><body>");
                await response.WriteAsync("Hello " + (context.User.Identity.Name ?? "anonymous") + "<br>");
                foreach (var claim in context.User.Claims)
                {
                    await response.WriteAsync(claim.Type + ": " + claim.Value + "<br>");
                }

                await response.WriteAsync("Tokens:<br>");

                await response.WriteAsync("Access Token: " + await context.GetTokenAsync("access_token") + "<br>");
                await response.WriteAsync("Refresh Token: " + await context.GetTokenAsync("refresh_token") + "<br>");
                await response.WriteAsync("Token Type: " + await context.GetTokenAsync("token_type") + "<br>");
                await response.WriteAsync("expires_at: " + await context.GetTokenAsync("expires_at") + "<br>");
                await response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
                await response.WriteAsync("<a href=\"/refresh_token\">Refresh Token</a><br>");
                await response.WriteAsync("</body></html>");
            });
        }

        private Task<OAuthOptions> GetOAuthOptionsAsync(HttpContext context, string currentAuthType)
        {
            if (string.Equals(WeChatDefaults.AuthenticationScheme, currentAuthType))
            {
                return Task.FromResult<OAuthOptions>(context.RequestServices.GetRequiredService<IOptionsMonitor<WeChatOptions>>().Get(currentAuthType));
            }

            throw new NotImplementedException(currentAuthType);
        }

        private async Task PrintRefreshedTokensAsync(HttpResponse response, JsonDocument payload, AuthenticationProperties authProperties)
        {
            response.ContentType = "text/html; charset=utf-8";
            await response.WriteAsync("<html><body>");
            await response.WriteAsync("Refreshed.<br>");
            await response.WriteAsync(HtmlEncoder.Default.Encode(payload.RootElement.ToString()).Replace(",", ",<br>") + "<br>");

            await response.WriteAsync("<br>Tokens:<br>");

            await response.WriteAsync("Access Token: " + authProperties.GetTokenValue("access_token") + "<br>");
            await response.WriteAsync("Refresh Token: " + authProperties.GetTokenValue("refresh_token") + "<br>");
            await response.WriteAsync("Token Type: " + authProperties.GetTokenValue("token_type") + "<br>");
            await response.WriteAsync("expires_at: " + authProperties.GetTokenValue("expires_at") + "<br>");

            await response.WriteAsync("<a href=\"/\">Home</a><br>");
            await response.WriteAsync("<a href=\"/refresh_token\">Refresh Token</a><br>");
            await response.WriteAsync("</body></html>");
        }
    }
}
