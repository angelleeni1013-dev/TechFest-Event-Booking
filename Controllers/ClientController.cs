using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using A2_TechFest_2026.Data;
using A2_TechFest_2026.Models;
using A2_TechFest_2026.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace A2_TechFest_2026.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public ClientController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // My Bookings - View only their bookings
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            var userEmail = user?.Email;

            var bookings = await _context.Bookings
                .Where(b => b.ClientEmail == userEmail)
                .OrderByDescending(b => b.BookingId)
                .ToListAsync();

            return View(bookings);
        }

        // Create Booking - GET
        [HttpGet]
        public IActionResult CreateBooking()
        {
            ViewBag.EarlyBirdDeadline = new DateTime(2026, 6, 30);
            ViewBag.IsEarlyBirdAvailable = DateTime.Now <= ViewBag.EarlyBirdDeadline;

            var model = new ClientBookingViewModel
            {
                ClientEmail = User.Identity?.Name ?? ""
            };

            return View(model);
        }

        // Create Booking - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(ClientBookingViewModel model)
        {
            ViewBag.EarlyBirdDeadline = new DateTime(2026, 6, 30);
            ViewBag.IsEarlyBirdAvailable = DateTime.Now <= ViewBag.EarlyBirdDeadline;

            if (model.TicketType == "early-bird" && DateTime.Now > ViewBag.EarlyBirdDeadline)
            {
                ModelState.AddModelError("TicketType", "Early Bird tickets are no longer available.");
                return View(model);
            }

            // --- NEW: Validate Age (Must be at least 10 years old) ---
            var age = DateTime.Today.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            if (age < 10)
            {
                ModelState.AddModelError("DateOfBirth", "Participant must be at least 10 years old.");
                return View(model);
            }

            // Student ticket age validation (16-25 years old)
            if (model.TicketType == "student")
            {
                if (age < 16 || age > 25)
                {
                    ModelState.AddModelError("TicketType",
                        "Student tickets are only available for ages 16-25. Your current age is " + age + ". Please select a different ticket type.");
                    return View(model);
                }
            }

            if (model.SelectedDays == null || !model.SelectedDays.Any())
            {
                ModelState.AddModelError("SelectedDays", "Please select at least one day.");
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create booking for each selected day
            var bookingIds = new List<int>();
            var duplicateDays = new List<int>();

            foreach (var day in model.SelectedDays.OrderBy(d => d))
            {
                // Check if already booked this day
                var existingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.ClientEmail == model.ClientEmail && b.Day == day);

                if (existingBooking != null)
                {
                    duplicateDays.Add(day);
                    continue; // Skip this day
                }

                decimal pricePerDay = model.TicketType switch
                {
                    "early-bird" => 120.00M,
                    "regular" => 150.00M,
                    "student" => 80.00M,
                    _ => 0.00M
                };

                var booking = new Booking
                {
                    EventName = model.EventName.Trim(),
                    ClientName = model.ClientName.Trim(),
                    ClientEmail = model.ClientEmail.Trim(),
                    ContactNumber = model.ContactNumber.Trim(),
                    Day = day,
                    TicketType = model.TicketType,
                    Charges = pricePerDay,
                    Status = "Pending",
                    PaymentStatus = "Unpaid",
                    PaymentMethod = null // Will be set after payment
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                bookingIds.Add(booking.BookingId);

                // Send booking confirmation email
                try
                {
                    await _emailService.SendBookingConfirmationAsync(booking.ClientEmail, booking);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send booking confirmation: {ex.Message}");
                }
            }

            // Show error for duplicate days
            if (duplicateDays.Any())
            {
                TempData["ErrorMessage"] = $"You have already booked Day {string.Join(", ", duplicateDays)}.";
            }

            if (bookingIds.Count == 0)
            {
                // All days were duplicates
                return View(model);
            }

            TempData["SuccessMessage"] = $"Booking created successfully for {bookingIds.Count} day(s)! Please complete payment.";

            // Always redirect to payment details
            return RedirectToAction("PaymentDetails", new { bookingId = bookingIds.First() });
        }

        // Payment Details - GET
        [HttpGet]
        public async Task<IActionResult> PaymentDetails(int bookingId)
        {
            var firstBooking = await _context.Bookings.FindAsync(bookingId);

            if (firstBooking == null || firstBooking.ClientEmail != User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            // Get ALL bookings for this event + user that are unpaid
            var allBookings = await _context.Bookings
                .Where(b => b.ClientEmail == firstBooking.ClientEmail
                            && b.EventName == firstBooking.EventName
                            && b.PaymentStatus == "Unpaid")
                .OrderBy(b => b.Day)
                .ToListAsync();

            if (!allBookings.Any())
            {
                TempData["ErrorMessage"] = "No unpaid bookings found.";
                return RedirectToAction("MyBookings");
            }

            ViewBag.Bookings = allBookings;
            ViewBag.DaysBooked = string.Join(", ", allBookings.Select(b => b.Day));
            ViewBag.TotalCharges = allBookings.Sum(b => b.Charges);

            var model = new PaymentDetailsViewModel
            {
                BookingId = bookingId // still pass the first booking
            };

            return View(model);
        }


        // Payment Details - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentDetails(PaymentDetailsViewModel model)
        {
            var firstBooking = await _context.Bookings.FindAsync(model.BookingId);

            if (firstBooking == null || firstBooking.ClientEmail != User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            // Get all unpaid bookings for this client + event
            var allBookings = await _context.Bookings
                .Where(b => b.ClientEmail == firstBooking.ClientEmail
                            && b.EventName == firstBooking.EventName
                            && b.PaymentStatus == "Unpaid")
                .OrderBy(b => b.Day)
                .ToListAsync();

            if (!allBookings.Any())
            {
                TempData["ErrorMessage"] = "No unpaid bookings found.";
                return RedirectToAction("MyBookings");
            }

            // Remove validation based on method
            if (model.PaymentMethod == "Credit/Debit Card")
            {
                ModelState.Remove("PhoneNumber");
                ModelState.Remove("BankName");
                ModelState.Remove("AccountNumber");
            }
            else if (model.PaymentMethod == "Touch 'n Go eWallet")
            {
                ModelState.Remove("CardNumber");
                ModelState.Remove("CardHolderName");
                ModelState.Remove("ExpiryDate");
                ModelState.Remove("CVV");
                ModelState.Remove("BankName");
                ModelState.Remove("AccountNumber");
            }
            else if (model.PaymentMethod == "Online Banking")
            {
                ModelState.Remove("CardNumber");
                ModelState.Remove("CardHolderName");
                ModelState.Remove("ExpiryDate");
                ModelState.Remove("CVV");
                ModelState.Remove("PhoneNumber");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Bookings = allBookings;
                ViewBag.DaysBooked = string.Join(", ", allBookings.Select(b => b.Day));
                ViewBag.TotalCharges = allBookings.Sum(b => b.Charges);
                return View(model);
            }

            // Generate OTP
            var otpCode = new Random().Next(100000, 999999).ToString();

            var verification = new PaymentVerification
            {
                BookingId = firstBooking.BookingId,  // key booking
                PaymentMethod = model.PaymentMethod,
                CardNumber = model.CardNumber ?? "",
                CardHolderName = model.CardHolderName ?? "",
                ExpiryDate = model.ExpiryDate ?? "",
                CVV = model.CVV ?? "",
                PhoneNumber = model.PhoneNumber ?? "",
                BankName = model.BankName ?? "",
                AccountNumber = model.AccountNumber ?? "",
                OtpCode = otpCode,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10),
                IsVerified = false
            };

            _context.PaymentVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // Send OTP
            await _emailService.SendPaymentOtpAsync(firstBooking.ClientEmail, otpCode, $"Booking Payment");

            return RedirectToAction("VerifyPayment", new { verificationId = verification.Id });
        }


        // Verify Payment OTP - GET
        [HttpGet]
        public async Task<IActionResult> VerifyPayment(int verificationId)
        {
            var verification = await _context.PaymentVerifications
                .FirstOrDefaultAsync(v => v.Id == verificationId);

            if (verification == null)
            {
                TempData["ErrorMessage"] = "Verification not found.";
                return RedirectToAction("MyBookings");
            }

            var booking = await _context.Bookings.FindAsync(verification.BookingId);
            if (booking == null || booking.ClientEmail != User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "Unauthorized access.";
                return RedirectToAction("MyBookings");
            }

            ViewBag.Booking = booking;
            ViewBag.VerificationId = verificationId;
            return View();
        }

        // Verify Payment OTP - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int verificationId, string otpCode)
        {
            var verification = await _context.PaymentVerifications.FindAsync(verificationId);

            if (verification == null)
            {
                TempData["ErrorMessage"] = "Verification not found.";
                return RedirectToAction("MyBookings");
            }

            var firstBooking = await _context.Bookings.FindAsync(verification.BookingId);
            if (firstBooking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (DateTime.Now > verification.ExpiresAt)
            {
                TempData["ErrorMessage"] = "OTP has expired.";
                return RedirectToAction("PaymentDetails", new { bookingId = firstBooking.BookingId });
            }

            if (verification.OtpCode.Trim() != otpCode.Trim())
            {
                TempData["ErrorMessage"] = "Invalid OTP code.";
                return RedirectToAction("VerifyPayment", new { verificationId });
            }

            // MARK AS VERIFIED
            verification.IsVerified = true;

            // UPDATE ALL BOOKINGS in the group
            var allBookings = await _context.Bookings
                .Where(b => b.ClientEmail == firstBooking.ClientEmail
                            && b.EventName == firstBooking.EventName
                            && b.PaymentStatus == "Unpaid")
                .ToListAsync();

            foreach (var b in allBookings)
            {
                b.PaymentStatus = "Paid";
                b.PaymentMethod = verification.PaymentMethod;
                b.Status = "Confirmed";
            }

            await _context.SaveChangesAsync();

            // Send one confirmation email
            await _emailService.SendPaymentConfirmationAsync(firstBooking.ClientEmail, firstBooking);

            TempData["SuccessMessage"] = "Payment completed successfully!";
            return RedirectToAction("MyBookings");
        }


        // Invoice
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null || booking.ClientEmail != User.Identity?.Name)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (booking.PaymentStatus != "Paid")
            {
                TempData["ErrorMessage"] = "Invoice is only available for paid bookings.";
                return RedirectToAction("MyBookings");
            }

            ViewBag.TransactionId = $"TFX-{GenerateTransactionId()}";
            return View(booking);
        }

        private string GenerateTransactionId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 9)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}