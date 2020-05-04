using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmailReminder.shared.Models;
using EmailReminder.WebApi.Services;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmailReminder.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationSercice _authentificationSercice;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public AuthenticationController(
            IAuthenticationSercice authentificationSercice,
            IBackgroundJobClient backgroundJobClient
        )
        {
            _authentificationSercice = authentificationSercice;
            _backgroundJobClient = backgroundJobClient;
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmUser([FromBody]EmailConfirmation confirmation)
        {
            var result = await _authentificationSercice.ConfirmUserAsync(confirmation);

            if (!result)
                return BadRequest("Could not confirm the user");

            return NoContent();
        }

        [HttpGet("loginToken")]
        public async Task<IActionResult> GetLoginToken(string email)
        {
            string loginToken = "";

            try
            {
                loginToken = await _authentificationSercice.GenerateLoginToken(email);
            }
            catch
            {
                return BadRequest("Could not generate login token");
            }

            _backgroundJobClient.Enqueue<IMailSender>(x => x.SendLoginToken(email, loginToken));

            return NoContent();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmailConfirmation confirmation)
        {
            var result = await _authentificationSercice.IsUserTokenValid(confirmation);

            if (!result)
                return BadRequest("Could not login the user.");

            var jwt = _authentificationSercice.GenerateJwt(confirmation.Email);

            return Ok(jwt);
        }
    }
}