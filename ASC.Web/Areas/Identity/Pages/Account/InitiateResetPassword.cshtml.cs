<<<<<<< HEAD
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using ASC.Web.Services;

namespace ASC.Web.Areas.Identity.Pages.Account
{
    [Authorize]
=======
using ASC.Utilities;
using ASC.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ASC.Web.Areas.Identity.Pages.Account
{
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
    public class InitiateResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

<<<<<<< HEAD
        public InitiateResetPasswordModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender)
=======
        public InitiateResetPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

<<<<<<< HEAD
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code, email = user.Email },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Reset Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");

            return RedirectToPage("/Account/ResetPasswordEmailConfirmation", new { area = "Identity" });
=======
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Find User
            var userEmail = HttpContext.User.GetCurrentUserDetails().Email;
            var user = await _userManager.FindByEmailAsync(userEmail);

            // Generate User code
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new
                {
                    userId = user.Id,
                    code = code
                },
                protocol: Request.Scheme);

            // Send Email
            await _emailSender.SendEmailAsync(
                userEmail,
                "Reset Password",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

            return RedirectToPage("./ResetPasswordEmailConfirmation");
>>>>>>> 8da259071b53eaf611f1701a7493e18be3d08c90
        }
    }
}