using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Owin.Security.WeChat.Core
{
    public class WeChatReturnEndpointContext //: ReturnEndpointContext
    {
        public WeChatReturnEndpointContext(
            HttpContext context,
            AuthenticationTicket ticket)
            //: base(context, ticket)
        {
        }
    }
}
