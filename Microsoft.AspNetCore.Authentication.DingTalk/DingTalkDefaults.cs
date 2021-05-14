// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Authentication.DingTalk
{
    /// <summary>
    /// Default values for DingTalk authentication
    /// </summary>
    public static class DingTalkDefaults
    {
        /// <summary>
		/// Default value for <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
		/// </summary>
        public const string AuthenticationScheme = "ding";

        public static readonly string DisplayName = "ding";

        /// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.AuthorizationEndpoint"/>.
		/// </summary>
        public static readonly string AuthorizationEndpoint = "https://oapi.dingtalk.com/connect/qrconnect";

        /// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.TokenEndpoint"/>.
		/// </summary>
        public static readonly string TokenEndpoint = "https://oapi.dingtalk.com/sns/gettoken";

        /// <summary>
		/// Default value for <see cref="OAuth.OAuthOptions.UserInformationEndpoint"/>.
		/// </summary>
        public static readonly string UserInformationEndpoint = "https://oapi.dingtalk.com/sns/getuserinfo_bycode";
    }
}
