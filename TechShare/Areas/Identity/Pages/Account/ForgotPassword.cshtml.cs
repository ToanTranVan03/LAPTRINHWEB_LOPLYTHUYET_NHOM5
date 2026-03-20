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
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lÃ²ng nháº­p email cá»§a báº¡n")]
            [EmailAddress(ErrorMessage = "Email khÃ´ng Ä‘Ãºng Ä‘á»‹nh dáº¡ng")]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    TempData["StatusMessage"] = "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n vá»›i email nÃ y.";
                    return RedirectToPage("./Login");
                }

                // 1. Váº«n táº¡o MÃ£ Token báº£o máº­t bÃ¬nh thÆ°á»ng
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                // 2. Váº«n táº¡o Ä‘Æ°á»ng link khÃ´i phá»¥c
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // 3. TRICK Táº I ÄÃ‚Y: KHÃ”NG Gá»¬I MAIL Ná»®A!
                // Hiá»‡n luÃ´n má»™t thÃ´ng bÃ¡o chá»©a nÃºt báº¥m Ä‘i tháº³ng Ä‘áº¿n trang Ä‘á»•i máº­t kháº©u
                TempData["StatusMessage"] = $"<a href='{callbackUrl}' class='fw-bold text-success text-decoration-underline'>Báº¤M VÃ€O ÄÃ‚Y</a> Ä‘á»ƒ Ä‘á»•i máº­t kháº©u má»›i cho {Input.Email}";
                
                return RedirectToPage("./Login");
            }
            return Page();
        }
    }
}
