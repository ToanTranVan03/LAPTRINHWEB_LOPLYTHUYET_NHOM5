using Microsoft.AspNetCore.Identity.UI.Services;

namespace TechShare.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Đây là hàm giả: Không làm gì cả, chỉ trả về thành công để đánh lừa hệ thống
            return Task.CompletedTask;
        }
    }
}