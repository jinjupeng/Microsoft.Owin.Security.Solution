using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace OpenApiTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MiniProgramController : ControllerBase
    {
        /// <summary>
        /// 微信小程序登录
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("signin")]
        public async Task<IActionResult> SigninAsync()
        {
            return Ok(await Task.FromResult("微信小程序登录成功！"));
        }
    }
}
