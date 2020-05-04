using EmailReminder.shared.Models;
using EmailReminder.WebApi.Data;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EmailReminder.WebApi.Services
{
    public class OvhMailSender : IMailSender
    {
        private readonly IOptions<EmailCredentials> _emailCredentials;
        private readonly IAuthenticationSercice _authentificationSercice;

        public OvhMailSender(IOptions<EmailCredentials> emailCredentials, IAuthenticationSercice authentificationSercice)
        {
            _emailCredentials = emailCredentials;
            _authentificationSercice = authentificationSercice;
        }

        private string BuildConfirmationLink(String page, String email, string token)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder
                .Append($"https://localhost:44331/{page}?email=")
                .Append(HttpUtility.UrlEncode(email))
                .Append("&token=")
                .Append(HttpUtility.UrlEncode(token));

            return stringBuilder.ToString();
        }

        private void SendMail(string subject, string body, string to)
        {
            using var client = new SmtpClient("ssl0.ovh.net", 587)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailCredentials.Value.User, _emailCredentials.Value.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                EnableSsl = true
            };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(_emailCredentials.Value.User),
                To = { to },
                Subject = subject,
                Body = body,

            };

            client.Send(mailMessage);
        }

        public async void SendReminderAsync(Reminder reminder)
        {
            var isUserConfirmes = await _authentificationSercice.IsUserConfirmedAsync(reminder.EmailAddress);

            if (!isUserConfirmes)
                return;

            SendMail(
                $"Your reminder for {reminder.DateTime.ToShortDateString()}",
                reminder.Message,
                reminder.EmailAddress
            );
        }

        public void SendVerification(string email, string token)
        {
            SendMail(
                $"Your email confirmation",
                BuildConfirmationLink("confirme", email, token),
                email
            );
        }

        public void SendLoginToken(string email, string token)
        {
            SendMail(
                $"Your email confirmation",
                BuildConfirmationLink("login", email, token),
                email
            );
        }
    }
}
