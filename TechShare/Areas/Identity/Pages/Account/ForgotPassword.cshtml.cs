using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TechShare.Models;

namespace TechShare.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email của bạn")]
            [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    TempData["StatusMessage"] = "Vui lòng kiểm tra hộp thư email của bạn để đặt lại mật khẩu.";
                    return RedirectToPage("./Login");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Đặt lại mật khẩu - TechShare",
                    $"Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>bấm vào đường link này</a> để tạo mật khẩu mới.");

                TempData["StatusMessage"] = "Vui lòng kiểm tra hộp thư email của bạn để đặt lại mật khẩu.";
                return RedirectToPage("./Login");
            }
            return Page();
        }
    }
}