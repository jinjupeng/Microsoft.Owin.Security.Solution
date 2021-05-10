using System;
using System.Security.Cryptography;
using System.Text;

namespace OpenApiTest.Common
{
    /// <summary>
    /// 实现微信AES-128-CBC加密数据的解密
    /// </summary>
    public class WXBizDataCrypt
    {
        /*
         * [C#实现微信AES-128-CBC加密数据的解密](https://www.cnblogs.com/jetz/p/6384809.html)
         */

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="text">带解密文本</param>
        /// <param name="aesKey">密钥</param>
        /// <param name="aesIV">16位初始向量</param>
        /// <returns></returns>
        public static string AESDecrypt(string text, string aesKey, string aesIV)
        {
            try
            {
                //16进制数据转换成byte
                byte[] encryptedData = Convert.FromBase64String(text);  // strToToHexByte(text);
                RijndaelManaged rijndaelCipher = new RijndaelManaged
                {
                    Key = Convert.FromBase64String(aesKey), // Encoding.UTF8.GetBytes(AesKey);
                    IV = Convert.FromBase64String(aesIV),// Encoding.UTF8.GetBytes(AesIV);
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                };
                ICryptoTransform transform = rijndaelCipher.CreateDecryptor();
                byte[] plainText = transform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
                string result = Encoding.Default.GetString(plainText);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
