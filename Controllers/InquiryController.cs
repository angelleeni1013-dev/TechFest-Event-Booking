using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using A2_TechFest_2026.Data;
using A2_TechFest_2026.Models;
using A2_TechFest_2026.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace A2_TechFest_2026.Controllers
{
    public class InquiryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public InquiryController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // =============== CLIENT =======================

        [Authorize(Roles = "Client")]
        public IActionResult Submit()
        {
            var model = new Inquiry
            {
                ClientEmail = User.Identity?.Name ?? ""
            };
            return View(model);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Inquiry inquiry)
        {
            var user = await _userManager.GetUserAsync(User);
            inquiry.ClientEmail = user?.Email ?? "";
            inquiry.CreatedAt = DateTime.Now;
            inquiry.Status = "Pending";

            ModelState.Remove("ClientEmail");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                return View(inquiry);
            }

            _context.Inquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            // Send confirmation email to client
            try
            {
                await _emailService.SendInquiryConfirmationAsync(inquiry.ClientEmail, inquiry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send inquiry confirmation: {ex.Message}");
            }

            // Send notification to admin
            try
            {
                await _emailService.SendInquiryNotificationAsync(inquiry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send inquiry notification: {ex.Message}");
            }

            TempData["SuccessMessage"] = "Inquiry submitted successfully! We'll get back to you soon.";
            return RedirectToAction("MyInquiries");
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyInquiries()
        {
            var user = await _userManager.GetUserAsync(User);
            var inquiries = await _context.Inquiries
                .Where(i => i.ClientEmail == user.Email)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(inquiries);
        }

        // =============== ADMIN =======================

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Manage()
        {
            var inquiries = await _context.Inquiries
                .OrderBy(i => i.Status)
                .ThenByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.PendingCount = inquiries.Count(i => i.Status == "Pending");
            ViewBag.RespondedCount = inquiries.Count(i => i.Status == "Responded");
            ViewBag.SolvedCount = inquiries.Count(i => i.Status == "Solved");

            return View(inquiries);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> Reply(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null)
            {
                TempData["ErrorMessage"] = "Inquiry not found.";
                return RedirectToAction("Manage");
            }
            return View(inquiry);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string adminReply)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null)
            {
                TempData["ErrorMessage"] = "Inquiry not found.";
                return RedirectToAction("Manage");
            }

            inquiry.AdminReply = adminReply;
            inquiry.Status = "Responded";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reply sent successfully!";
            return RedirectToAction("Manage");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> MarkSolved(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null)
            {
                TempData["ErrorMessage"] = "Inquiry not found.";
                return RedirectToAction("Manage");
            }

            inquiry.Status = "Solved";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Inquiry marked as solved!";
            return RedirectToAction("Manage");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null)
            {
                TempData["ErrorMessage"] = "Inquiry not found.";
                return RedirectToAction("Manage");
            }

            _context.Inquiries.Remove(inquiry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Inquiry deleted successfully!";
            return RedirectToAction("Manage");
        }
    }
}