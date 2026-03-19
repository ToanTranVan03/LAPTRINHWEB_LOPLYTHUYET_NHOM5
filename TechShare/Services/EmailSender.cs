using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace TechShare.Services;

public class EmailSender : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var mail = new MailMessage();
        
        // Thay bằng email Gmail của bạn (Dùng để gửi thư đi)
        string fromEmail = "ttoan17123@gmail.com"; 
        // Thay bằng Mật khẩu ứng dụng Gmail (Sẽ hướng dẫn lấy ở Bước 4)
        string appPassword = "abcdefghijklmnop"; 

        mail.From = new MailAddress(fromEmail, "TechShare System");
        mail.To.Add(email);
        mail.Subject = subject;
        mail.Body = htmlMessage;
        mail.IsBodyHtml = true; // Cho phép gửi giao diện HTML trong thư

        using var smtp = new SmtpClient("smtp.gmail.com", 587);
        smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
        smtp.EnableSsl = true;

        await smtp.SendMailAsync(mail);
    }
}