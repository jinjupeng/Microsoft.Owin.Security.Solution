using Microsoft.AspNetCore.Authentication.WeChat.MiniProgram;
using OpenApiTest.Common;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenApiTest.Handlers
{
    public class MiniProgramLoginHandler : IMiniProgramLoginHandler
    {
        /// <summary>
        /// 解析小程序登录用户信息
        /// </summary>
        /// <param name="context">保存小程序用户登录信息</param>
        /// <returns></returns>
        public Task<bool> ExcuteAsync(MiniProgramLoginContext context)
        {
            try
            {
                var session_key = context.MiniProgramUser?.session_key;
                var encryptedData = context.MiniProgramUser.Input.GetProperty("encryptedData").GetString();
                var iv = context.MiniProgramUser.Input.GetProperty("iv").GetString();
                var miniProgramUserInfoStr = WXBizDataCrypt.AESDecrypt(encryptedData, session_key, iv);
                var miniProgramUserInfoDict = JsonSerializer.Deserialize<Dictionary<string, object>>(miniProgramUserInfoStr);
                foreach (var item in miniProgramUserInfoDict)
                {
                    Console.WriteLine($"{item.Key}：{item.Value}");
                }

                // TODO：业务逻辑处理

                return Task.FromResult(true);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }
    }
}
