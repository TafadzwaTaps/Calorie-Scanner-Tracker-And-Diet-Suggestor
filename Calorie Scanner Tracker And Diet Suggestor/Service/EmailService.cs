using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Calorie_Scanner_Tracker_And_Diet_Suggestor.Service
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendMealReminder(string email, string mealName, string mealType)
        {
            var smtpClient = new SmtpClient(_configuration["Smtp:Host"])
            {
                Port = int.Parse(_configuration["Smtp:Port"]),
                Credentials = new NetworkCredential(_configuration["Smtp:Username"], _configuration["Smtp:Password"]),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Smtp:FromEmail"]),
                Subject = "Meal Reminder",
                Body = $"Hello, it's time for your {mealType} ({mealName})!",
                IsBodyHtml = false
            };
            message.To.Add(email);

            await smtpClient.SendMailAsync(message);
        }
    }
}
