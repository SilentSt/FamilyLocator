using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FamilyLocator.DataLayer.DataBase;
using FamilyLocator.DataLayer.DataBase.Entities;
using MailKit.Net.Smtp;

using MimeKit;

namespace FamilyLocator.Api
{
    public static class ConfirmationEmail
    {
        /* Creating a new instance of the SmtpClient class. */
        private static readonly SmtpClient _mail;
        /* A constant string that is used to send emails. */
        private const string NoReplyMail = "no-reply@sbeusilent.space";

        /* A static constructor. It is called once when the class is first loaded. */
        static ConfirmationEmail()
        {
            var mail = new SmtpClient();

            _mail = mail;
        }

        /// <summary>
        /// It sends a confirmation email to the user with a confirmation code
        /// </summary>
        /// <param name="user">The user model</param>
        /// <param name="context">The database context</param>
        /// <returns>
        /// The confirmation.Id is being returned.
        /// </returns>
        public static async Task<string> SendConfirmationEmail(this XIdentityUser user, ApiDbContext context)
        {
            if (!_mail.IsConnected || !_mail.IsAuthenticated)
            {
                _mail.Connect("sbeusilent.space", 587, true);
                _mail.AuthenticationMechanisms.Remove("XOAUTH2");
                _mail.Authenticate(NoReplyMail, "1mynewHome1_nrp");
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new Exception("Email not found.");
            }

            var toEmail = user.Email;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("No-Reply", NoReplyMail));
            message.To.Add(new MailboxAddress(user.UserName, toEmail));
            message.Subject = "Confirmation message";
            var confirmation = new XIdentityUserConfirm()
            {
                Id = Guid.NewGuid().ToString(),
                User = user,
                HashCode = GenerateEncryptedString()
            };
            await context.UserConfirmations.AddAsync(confirmation);
            var code = confirmation.MailCode;
            message.Body = new TextPart("plain") { Text = $"Confirmation code: \n\n {code}" };
            await _mail.SendAsync(message);
            await context.SaveChangesAsync();
            await _mail.DisconnectAsync(true);
            return confirmation.HashCode;
        }

        private static string GenerateEncryptedString()
        {
            var secret = Guid.NewGuid().ToString() + Guid.NewGuid();
            var salt = Guid.NewGuid().ToString().Substring(0, 16);
            var sha = Aes.Create();
            var preHash = Encoding.UTF32.GetBytes(secret + salt);
            var hash = sha.EncryptCbc(preHash, Encoding.UTF8.GetBytes(salt));
            var result = Convert.ToBase64String(hash);
            return result;
        }
    }
}
