using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using A2_TechFest_2026.Data;
using A2_TechFest_2026.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using A2_TechFest_2026.Services;
using Microsoft.AspNetCore.Identity;

namespace A2_TechFest_2026.Controllers
{
    public class BookingController : Controller
    {
        private ApplicationDbContext bookingContext;
        private readonly IEmailService _emailService;

        public BookingController(ApplicationDbContext bookingContext, IEmailService emailService)
        {
            this.bookingContext = bookingContext;
            _emailService = emailService;
        }

        // Index - All authenticated users can view
        [Authorize]
        public IActionResult Index(string searchTerm, string sortBy)
        {
            var filteredBookings = bookingContext.Bookings.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filteredBookings = filteredBookings.Where(b =>
                    b.ClientName.Contains(searchTerm) ||
                    b.ClientEmail.Contains(searchTerm) ||
                    b.EventName.Contains(searchTerm) ||
                    b.Status.Contains(searchTerm) ||
                    b.Gender.Contains(searchTerm));
            }

            filteredBookings = sortBy switch
            {
                "name" => filteredBookings.OrderBy(b => b.ClientName),
                "day" => filteredBookings.OrderBy(b => b.Day),
                "status" => filteredBookings.OrderBy(b => b.Status),
                "charges" => filteredBookings.OrderBy(b => b.Charges),
                "payment" => filteredBookings.OrderBy(b => b.PaymentStatus),
                "gender" => filteredBookings.OrderBy(b => b.Gender),
                "age" => filteredBookings.OrderBy(b => b.DateOfBirth),
                _ => filteredBookings.OrderBy(b => b.BookingId)
            };

            var bookingsList = filteredBookings.ToList();
            var allBookings = bookingContext.Bookings.ToList();

            ViewBag.TotalBookings = bookingsList.Count();
            ViewBag.TotalRevenue = allBookings.Sum(b => b.Charges);
            ViewBag.ConfirmedBookings = bookingsList.Count(b => b.Status.Trim() == "Confirmed");
            ViewBag.PendingBookings = bookingsList.Count(b => b.Status.Trim() == "Pending");
            ViewBag.CancelledBookings = bookingsList.Count(b => b.Status.Trim() == "Cancelled");

            ViewBag.EarlyBirdCount = bookingsList.Count(b => b.TicketType == "early-bird");
            ViewBag.RegularCount = bookingsList.Count(b => b.TicketType == "regular");
            ViewBag.StudentCount = bookingsList.Count(b => b.TicketType == "student");

            ViewBag.Day1Count = allBookings.Count(b => b.Day == 1);
            ViewBag.Day2Count = allBookings.Count(b => b.Day == 2);
            ViewBag.Day3Count = allBookings.Count(b => b.Day == 3);

            ViewBag.UnpaidCount = bookingsList.Count(b => b.PaymentStatus.Trim() == "Unpaid");
            ViewBag.PaidCount = bookingsList.Count(b => b.PaymentStatus.Trim() == "Paid");
            ViewBag.RefundedCount = bookingsList.Count(b => b.PaymentStatus.Trim() == "Refunded");

            ViewBag.MaleCount = bookingsList.Count(b => b.Gender == "Male");
            ViewBag.FemaleCount = bookingsList.Count(b => b.Gender == "Female");
            ViewBag.OtherGenderCount = bookingsList.Count(b => b.Gender == "Other");

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            return View(bookingsList);
        }

        // Add - Only Admin can add
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.EarlyBirdDeadline = new DateTime(2026, 6, 30);
            ViewBag.IsEarlyBirdAvailable = DateTime.Now <= ViewBag.EarlyBirdDeadline;
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Booking model, int[] selectedDays)
        {
            ViewBag.EarlyBirdDeadline = new DateTime(2026, 6, 30);
            ViewBag.IsEarlyBirdAvailable = DateTime.Now <= ViewBag.EarlyBirdDeadline;

            // Validate selected days
            if (selectedDays == null || selectedDays.Length == 0)
            {
                ModelState.AddModelError("Day", "Please select at least one day.");
                return View(model);
            }

            // Remove Day validation since we're using selectedDays array
            ModelState.Remove("Day");
            ModelState.Remove("BookingId");
            ModelState.Remove("Charges");
            ModelState.Remove("DateOfBirth");
            ModelState.Remove("Gender");

            // Validate ticket type
            if (model.TicketType == "early-bird" && DateTime.Now > ViewBag.EarlyBirdDeadline)
            {
                ModelState.AddModelError("TicketType", "Early Bird tickets are no longer available.");
                return View(model);
            }

            // Calculate age - ONLY ONCE
            var age = DateTime.Today.Year - model.DateOfBirth.Year;
            if (model.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            // Validate minimum age (must be at least 10 years old)
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
                    ModelState.AddModelError("DateOfBirth", "Student tickets are only available for ages 16-25. Please select a different ticket type.");
                    return View(model);
                }
            }

            // Validate payment method when status is Paid
            if (model.PaymentStatus == "Paid")
            {
                if (string.IsNullOrWhiteSpace(model.PaymentMethod))
                {
                    ModelState.AddModelError("PaymentMethod", "Please select a payment method when payment status is Paid.");
                    return View(model);
                }
            }
            else
            {
                model.PaymentMethod = null;
                ModelState.Remove("PaymentMethod");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Calculate price per day
            decimal pricePerDay = model.TicketType switch
            {
                "early-bird" => 120.00M,
                "regular" => 150.00M,
                "student" => 80.00M,
                _ => 0.00M
            };

            // Create a booking for each selected day
            var bookingsCreated = 0;
            var duplicateDays = new List<int>();

            foreach (var day in selectedDays.OrderBy(d => d))
            {
                // Check for duplicates
                var existingBooking = await bookingContext.Bookings
                    .FirstOrDefaultAsync(b => b.ClientEmail.ToLower() == model.ClientEmail.ToLower().Trim() && b.Day == day);

                if (existingBooking != null)
                {
                    duplicateDays.Add(day);
                    continue;
                }

                var booking = new Booking
                {
                    EventName = model.EventName.Trim(),
                    ClientName = model.ClientName.Trim(),
                    ClientEmail = model.ClientEmail.Trim(),
                    ContactNumber = model.ContactNumber.Trim(),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender.Trim(),
                    Day = day,
                    TicketType = model.TicketType,
                    Charges = pricePerDay,
                    Status = model.Status?.Trim() ?? "Pending",
                    PaymentStatus = model.PaymentStatus?.Trim() ?? "Unpaid",
                    PaymentMethod = model.PaymentMethod?.Trim()
                };

                bookingContext.Bookings.Add(booking);
                bookingsCreated++;

                // Send booking confirmation email
                try
                {
                    await _emailService.SendBookingConfirmationAsync(booking.ClientEmail, booking);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send booking confirmation email: {ex.Message}");
                }
            }

            // Add these lines right before SaveChangesAsync()
            Console.WriteLine($"=== DEBUG: About to save {bookingsCreated} bookings ===");
            Console.WriteLine($"Connection String: {bookingContext.Database.GetConnectionString()}");

            await bookingContext.SaveChangesAsync();

            // Add this after SaveChanges
            var totalInDb = await bookingContext.Bookings.CountAsync();
            Console.WriteLine($"=== DEBUG: Total bookings in DB after save: {totalInDb} ===");

            // Show appropriate message
            if (duplicateDays.Any() && bookingsCreated > 0)
            {
                TempData["WarningMessage"] = $"Day {string.Join(", ", duplicateDays)} already booked for this client. Successfully created {bookingsCreated} other booking(s).";
            }
            else if (duplicateDays.Any() && bookingsCreated == 0)
            {
                TempData["ErrorMessage"] = $"All selected days (Day {string.Join(", ", duplicateDays)}) are already booked for this client.";
                return View(model);
            }
            else
            {
                TempData["SuccessMessage"] = $"Successfully created {bookingsCreated} booking(s)!";
            }

            return RedirectToAction("Index");
        }

        // Edit - Admin and Staff can edit
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var booking = bookingContext.Bookings.FirstOrDefault(c => c.BookingId == id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Index");
            }
            ViewBag.EarlyBirdDeadline = new DateTime(2026, 6, 30);
            ViewBag.IsEarlyBirdAvailable = DateTime.Now <= new DateTime(2026, 6, 30);
            return View(booking);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Booking editedBooking)
        {
            var booking = bookingContext.Bookings.Find(editedBooking.BookingId);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Index");
            }

            ViewBag.EarlyBirdDeadline = new DateTime(2026, 6, 30);
            ViewBag.IsEarlyBirdAvailable = DateTime.Now <= ViewBag.EarlyBirdDeadline;

            if (editedBooking.TicketType == "early-bird" && DateTime.Now > ViewBag.EarlyBirdDeadline)
            {
                ModelState.AddModelError("TicketType", "Early Bird tickets are no longer available. Please select Regular or Student ticket.");
                ViewBag.IsEarlyBirdAvailable = false;
                return View(editedBooking);
            }

            // Calculate age
            var age = DateTime.Today.Year - editedBooking.DateOfBirth.Year;
            if (editedBooking.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;

            // Validate minimum age (must be at least 10 years old)
            if (age < 10)
            {
                ModelState.AddModelError("DateOfBirth", "Participant must be at least 10 years old.");
                return View(editedBooking);
            }

            // Student ticket age validation (16-25 years old)
            if (editedBooking.TicketType == "student")
            {
                if (age < 16 || age > 25)
                {
                    ModelState.AddModelError("TicketType", "Student tickets are only available for ages 16-25. Current age is " + age + ". Please select a different ticket type.");
                    return View(editedBooking);
                }
            }

            if (editedBooking.PaymentStatus == "Paid" && string.IsNullOrWhiteSpace(editedBooking.PaymentMethod))
            {
                ModelState.AddModelError("PaymentMethod", "Please select a payment method when payment status is Paid.");
                return View(editedBooking);
            }

            if (editedBooking.PaymentStatus != "Paid")
            {
                editedBooking.PaymentMethod = null;
                ModelState.Remove("PaymentMethod");
            }

            if (!ModelState.IsValid)
            {
                return View(editedBooking);
            }

            // Check if payment status changed to Paid BEFORE updating
            bool paymentJustCompleted = booking.PaymentStatus != "Paid" && editedBooking.PaymentStatus == "Paid";

            booking.EventName = editedBooking.EventName?.Trim();
            booking.ClientName = editedBooking.ClientName?.Trim();
            booking.ClientEmail = editedBooking.ClientEmail?.Trim();
            booking.ContactNumber = editedBooking.ContactNumber?.Trim();
            booking.DateOfBirth = editedBooking.DateOfBirth;
            booking.Gender = editedBooking.Gender?.Trim();
            booking.Day = Math.Clamp(editedBooking.Day, 1, 3);
            booking.TicketType = editedBooking.TicketType?.Trim();

            // Each booking is for ONE day, so charges = price per day
            booking.Charges = booking.GetTicketPricePerDay();

            booking.Status = editedBooking.Status?.Trim();
            booking.PaymentStatus = editedBooking.PaymentStatus?.Trim();
            booking.PaymentMethod = editedBooking.PaymentMethod?.Trim();

            bookingContext.SaveChanges();

            // Send payment confirmation email if payment was just completed
            if (paymentJustCompleted)
            {
                try
                {
                    await _emailService.SendPaymentConfirmationAsync(booking.ClientEmail, booking);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send payment confirmation email: {ex.Message}");
                }
            }

            TempData["SuccessMessage"] = "Booking updated successfully!";
            return RedirectToAction("Index");
        }

        // Delete - Only Admin can delete
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var booking = bookingContext.Bookings.AsNoTracking().FirstOrDefault(c => c.BookingId == id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Index");
            }
            return View(booking);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int bookingid)
        {
            var booking = bookingContext.Bookings.Find(bookingid);
            if (booking != null)
            {
                bookingContext.Bookings.Remove(booking);
                bookingContext.SaveChanges();
                TempData["SuccessMessage"] = "Booking deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error: Booking could not be deleted. Record not found.";
            }
            return RedirectToAction("Index");
        }

        // Invoice - All authenticated users can view invoice
        [Authorize]
        [HttpGet]
        public IActionResult Invoice(int id)
        {
            var booking = bookingContext.Bookings.FirstOrDefault(b => b.BookingId == id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Index");
            }

            if (booking.PaymentStatus != "Paid")
            {
                TempData["ErrorMessage"] = "Invoice is only available for paid bookings.";
                return RedirectToAction("Index");
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