using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using EmailReminder.shared.Models;
using EmailReminder.WebApi.Data;
using EmailReminder.WebApi.Services;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmailReminder.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemindersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IAuthenticationSercice _authentificationSercice;

        public RemindersController(
            ApplicationDbContext context, 
            IBackgroundJobClient backgroundJobClient,
            IAuthenticationSercice authentificationSercice
        )
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _authentificationSercice = authentificationSercice;
        }

        [HttpGet("all")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetUserReminders()
        {
            var claim = User
                .Claims
                .Where(c => c.Type == JwtRegisteredClaimNames.Sub)
                .FirstOrDefault();

            if (claim == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == claim.Value);

            if (user == null)
                return Unauthorized();

            var reminders = await _context
                .Reminders
                .Where(r => r.EmailAddress == user.Email && r.DateTime > DateTime.Now.Date)
                .ToListAsync();

            return Ok(reminders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateReminder([FromBody]Reminder reminder)
        {
            try
            {
                _context.Add(reminder);

                await _context.SaveChangesAsync();
            }
            catch
            {
                return BadRequest("Could not save reminder");
            }

            var exists = await _authentificationSercice.DoesUserExistAsync(reminder.EmailAddress);

            if (!exists)
            {
                await _authentificationSercice.RegisterUserAsync(reminder.EmailAddress);
            }

            _backgroundJobClient.Schedule<IMailSender>(x => x.SendReminderAsync(reminder), new DateTimeOffset(reminder.DateTime));

            return Ok(reminder);
        }
    }
}