using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.WeChat
{
    /// <summary>
    /// Configuration options for <see cref="WeChatHandler"/>.
    /// </summary>
    public class WeChatOptions : OAuthOptions
    {
        /// <summary>
        /// Initializes a new <see cref="WeChatOptions"/>.
        /// </summary>
        public WeChatOptions()
        {
            AuthenticationScheme = WeChatDefaults.AuthenticationScheme;
            CallbackPath = new PathString("/signin-wechat"); // �ص��ӿڣ����Զ���
            AuthorizationEndpoint = WeChatDefaults.AuthorizationEndpoint;
            TokenEndpoint = WeChatDefaults.TokenEndpoint;
            UserInformationEndpoint = WeChatDefaults.UserInformationEndpoint;
            StateAddition = "#wechat_redirect";

            //BaseScope ����������Ȩҳ�棬ֱ����ת��ֻ�ܻ�ȡ�û�openid����
            //InfoScope ��������Ȩҳ�棬��ͨ��openid�õ��ǳơ��Ա����ڵء����ң���ʹ��δ��ע������£�ֻҪ�û���Ȩ��Ҳ�ܻ�ȡ����Ϣ��
            //LoginScope (PC��ɨ���¼)
            WeChatScope = InfoScope;
        }

        /// <summary>
        /// access_type. Set to 'offline' to request a refresh token.
        /// </summary>
        public string AccessType { get; set; }

        public string OpenIdEndpoint { get; }

        public string AppId
        {
            get { return ClientId; }
            set { ClientId = value; }
        }

        public string AppKey
        {
            get { return ClientSecret; }
            set { ClientSecret = value; }
        }

        public string StateAddition { get; set; }

        public string AuthenticationScheme { get; set; }

        public string WeChatScope { get; set; }

        public string BaseScope = "snsapi_base";

        public string InfoScope = "snsapi_userinfo";

        public string LoginScope = "snsapi_login";

        /*
         * snsapi_login��snsapi_base ��snsapi_userinfo ����֮�������
         * �����߶��ǹ���ƽ̨�ṩ��ֻ�����ƶ���ʹ�õ�ɨ���ȡ�û���Ϣ����ֻ�� snsapi_login Ȩ���ܹ���PC��ȡ�û���Ϣ
         */

    }
}
