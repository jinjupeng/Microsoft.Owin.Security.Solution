using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.WeChat.MiniProgram
{
    /// <summary>
    /// 调用方的实现IWeChatMiniProgramLoginHandler时会使用到的上下文对象
    /// 具体包含哪些内容请看定义
    /// </summary>
    public class MiniProgramLoginContext
    {
        /// <summary>
        /// http请求上下文
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// 实现类本身可以通过选项模式访问到这个对象，不过这里提供会让实现方更方便
        /// </summary>
        public MiniProgramOptions Options { get; }

        /// <summary>
        /// 微信小程序登录的用户信息
        /// </summary>
        public MiniProgramUser MiniProgramUser { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="options"></param>
        /// <param name="miniProgramUser"></param>
        public MiniProgramLoginContext(HttpContext httpContext, MiniProgramOptions options, MiniProgramUser miniProgramUser)
        {
            HttpContext = httpContext;
            Options = options;
            MiniProgramUser = miniProgramUser;
        }
    }
}
