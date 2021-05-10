using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.AspNetCore.Authentication.WeChat.MiniProgram
{
    public static class MiniProgramExtensions
    {
        public static AuthenticationBuilder AddMiniProgram<TLoginHandler>(this AuthenticationBuilder builder)
            where TLoginHandler : class, IMiniProgramLoginHandler
            => builder.AddMiniProgram<TLoginHandler>(MiniProgramConsts.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddMiniProgram<TLoginHandler>(this AuthenticationBuilder builder, Action<MiniProgramOptions> configureOptions)
            where TLoginHandler : class, IMiniProgramLoginHandler
            => builder.AddMiniProgram<TLoginHandler>(MiniProgramConsts.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddMiniProgram<TLoginHandler>(this AuthenticationBuilder builder, string authenticationScheme, Action<MiniProgramOptions> configureOptions)
            where TLoginHandler : class, IMiniProgramLoginHandler
            => builder.AddMiniProgram<TLoginHandler>(authenticationScheme, MiniProgramConsts.AuthenticationSchemeDisplayName, configureOptions);

        public static AuthenticationBuilder AddMiniProgram<TLoginHandler>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<MiniProgramOptions> configureOptions)
            where TLoginHandler : class, IMiniProgramLoginHandler
        {
            builder.Services.AddScoped<IMiniProgramLoginHandler, TLoginHandler>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<MiniProgramOptions>, MiniProgramPostConfigureOptions>());
            return builder.AddScheme<MiniProgramOptions, MiniProgramHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
