using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using TechShare.Models;

namespace TechShare.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lÃ²ng nháº­p email")]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lÃ²ng nháº­p máº­t kháº©u má»›i")]
            [StringLength(100, ErrorMessage = "Máº­t kháº©u tá»‘i thiá»ƒu {2} kÃ½ tá»±.", MinimumLength = 3)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Máº­t kháº©u xÃ¡c nháº­n khÃ´ng khá»›p.")]
            public string? ConfirmPassword { get; set; }

            [Required]
            public string Code { get; set; } = string.Empty;
        }

        public IActionResult OnGet(string? code = null)
        {
            if (code == null) return BadRequest("Lá»—i: YÃªu cáº§u mÃ£ xÃ¡c thá»±c Ä‘á»ƒ Ä‘áº·t láº¡i máº­t kháº©u.");
            
            Input = new InputModel { Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)) };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Báº£o máº­t: Váº«n bÃ¡o thÃ nh cÃ´ng dÃ¹ email khÃ´ng tá»“n táº¡i
                TempData["StatusMessage"] = "Máº­t kháº©u Ä‘Ã£ Ä‘Æ°á»£c khÃ´i phá»¥c thÃ nh cÃ´ng. Vui lÃ²ng Ä‘Äƒng nháº­p!";
                return RedirectToPage("./Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Máº­t kháº©u Ä‘Ã£ Ä‘Æ°á»£c khÃ´i phá»¥c thÃ nh cÃ´ng. Vui lÃ²ng Ä‘Äƒng nháº­p!";
                return RedirectToPage("./Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}
