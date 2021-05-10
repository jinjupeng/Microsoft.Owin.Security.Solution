using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.Authentication.WeChat.MiniProgram
{
    /// <summary>
    /// 微信小程序身份验证方案关联的选项对象
    /// 参考OAuthOptions和MicrosoftAccountOptions
    /// 选项对象的初始化点有如下几个：
    /// 1、MiniProgramAuthenticationOptions的构造函数
    /// 2、startup中，调用方对象选项对象赋值
    /// 3、MiniProgramAuthenticationOptions
    /// 4、MiniProgramAuthenticationHandler的构造函数
    /// </summary>
    public class MiniProgramOptions : OAuthOptions
    {
        public MiniProgramOptions()
        {
            CallbackPath = new PathString(MiniProgramConstants.CallbackPath);
            UserInformationEndpoint = MiniProgramConstants.UserInformationEndpoint;

            //请求微信服务器获取openid、session_key的超时时间，默认30秒
            BackchannelTimeout = TimeSpan.FromSeconds(30);

            //此处定义的是想存储到第三方登录的额外字段，必须有的字段在handler中处理
            ClaimActions.MapJsonKey("openid", "openid");
            ClaimActions.MapJsonKey("session_key", "session_key");
            ClaimActions.MapJsonKey("unionid", "unionid");
        }

        /// <summary>
        /// 重载：验证选项对象
        /// </summary>
        public override void Validate()
        {
            //base.Validate();

            if (string.IsNullOrEmpty(ClientId))
            {
                throw new ArgumentException();
            }

            if (string.IsNullOrEmpty(ClientSecret))
            {
                throw new ArgumentException();
            }

            if (string.IsNullOrEmpty(UserInformationEndpoint))
            {
                throw new ArgumentException();
            }
            if (!CallbackPath.HasValue)
            {
                throw new ArgumentException();
            }
        }
    }
}
