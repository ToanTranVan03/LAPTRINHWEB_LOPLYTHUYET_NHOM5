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
                    TempData["StatusMessage"] = "Không tìm thấy tài khoản với email này.";
                    return RedirectToPage("./Login");
                }

                // 1. Vẫn tạo Mã Token bảo mật bình thường
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                // 2. Vẫn tạo đường link khôi phục
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // 3. TRICK TẠI ĐÂY: KHÔNG GỬI MAIL NỮA!
                // Hiện luôn một thông báo chứa nút bấm đi thẳng đến trang đổi mật khẩu
                TempData["StatusMessage"] = $"<a href='{callbackUrl}' class='fw-bold text-success text-decoration-underline'>BẤM VÀO ĐÂY</a> để đổi mật khẩu mới cho {Input.Email}";
                
                return RedirectToPage("./Login");
            }
            return Page();
        }
    }
}