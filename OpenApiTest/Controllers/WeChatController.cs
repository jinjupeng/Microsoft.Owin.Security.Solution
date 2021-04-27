using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace OpenApiTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeChatController : ControllerBase
    {
        [HttpGet]
        [Route("signin")]
        public async Task<IActionResult> SignIn()
        {
            return Ok(await Task.FromResult("微信扫码登录回调成功！"));
        }
    }
}
