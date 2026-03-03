using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using A2_TechFest_2026.Data;
using A2_TechFest_2026.Services;
using Microsoft.EntityFrameworkCore;

namespace A2_TechFest_2026.Areas.Identity.Pages.Account
{
    public class VerifyEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerifyEmailModel> _logger;

        public VerifyEmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<VerifyEmailModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        public string VerificationCode { get; set; } = string.Empty;

        public IActionResult OnGet(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("./Register");
            }

            Email = email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Find the verification record
            var verification = await _context.EmailVerifications
                .Where(v => v.Email == Email && v.VerificationCode == VerificationCode && !v.IsVerified)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code or code already used.");
                return Page();
            }

            // Check if code has expired
            if (DateTime.Now > verification.ExpiresAt)
            {
                ModelState.AddModelError(string.Empty, "Verification code has expired. Please request a new one.");
                return Page();
            }

            // Mark as verified FIRST
            verification.IsVerified = true;
            await _context.SaveChangesAsync();

            // Update user's EmailConfirmed status
            var user = await _userManager.FindByEmailAsync(Email);
            if (user != null)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                // Send welcome email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(Email, Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to send welcome email: {ex.Message}");
                }

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                _logger.LogInformation("User email verified successfully.");

                return RedirectToPage("/Account/RegisterConfirmation", new { area = "Identity", email = Email });
            }

            ModelState.AddModelError(string.Empty, "Error verifying email.");
            return Page();
        }

        public async Task<IActionResult> OnPostResendCodeAsync()
        {
            // Find user
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                return RedirectToPage("./Register");
            }

            // Generate new verification code
            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString();

            // Save new verification code
            var emailVerification = new A2_TechFest_2026.Models.EmailVerification
            {
                UserId = user.Id,
                Email = Email,
                VerificationCode = verificationCode,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(15),
                IsVerified = false
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // Send new verification email
            try
            {
                await _emailService.SendVerificationCodeAsync(Email, verificationCode);
                _logger.LogInformation("New verification code sent successfully");

                TempData["StatusMessage"] = "A new verification code has been sent to your email.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send verification email: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Failed to resend code. Please try again.");
            }

            return Page();
        }
    }
}