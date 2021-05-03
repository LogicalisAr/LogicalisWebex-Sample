using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SallyBot.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SallyBot.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserServices _userServices;

        public UserController(ILogger<UserController> logger,
                                UserServices userServices)
        {
            _logger = logger;
            _userServices = userServices;
        }

        [HttpGet]
        [Route("oauth")]
        public async Task<ContentResult> oauth(string code, string state)
        {
            //Retrieves oauth code to generate tokens for users
            _logger.LogInformation("Webex oauth endpoint start");
            _logger.LogInformation("OAuth code: " + code);
            _logger.LogInformation("OAuth state: " + state);

            bool success = false;

            if (String.Compare(state, "set_state_here") == 0)
                success = await _userServices.generateWebexToken(code);

            if (success)
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = "<div>Gracias por aceptar los permisos</div>"
                };
            }
            else
            {
                return new ContentResult
                {
                    ContentType = "text/html",
                    Content = "<div>Ah ocurrido un error, hable con el administrador o intente m&aacute;s tarde</div>"
                };
            }
        }

        [HttpGet]
        [Route("refreshWebexoAuth")]
        public async Task<IActionResult> refreshWebexoAuth()
        {
            bool successCleanRefreshWebexTokensExpired = await _userServices.cleanRefreshWebexTokensExpired();

            bool successRefreshWebexToken = await _userServices.refreshWebexToken();

            return Ok(new
            {
                cleanRefreshWebexTokensExpired = successCleanRefreshWebexTokensExpired,
                refreshWebexToken = successRefreshWebexToken
            });
        }
    }
}
