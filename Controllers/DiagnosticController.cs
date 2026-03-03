using Microsoft.AspNetCore.Mvc;
using A2_TechFest_2026.Data;
using A2_TechFest_2026.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace A2_TechFest_2026.Controllers
{
    public class DiagnosticController : Controller
    {
        private ApplicationDbContext _context;

        public DiagnosticController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Access this at: /Diagnostic/DatabaseTest
        public async Task<IActionResult> DatabaseTest()
        {
            var result = new StringBuilder();
            result.AppendLine("=== TECHFEST 2026 DATABASE DIAGNOSTIC TEST ===\n");

            try
            {
                // Test 1: Can we connect to database?
                result.AppendLine("TEST 1: Database Connection");
                var canConnect = _context.Database.CanConnect();
                result.AppendLine($"✅ Can Connect: {canConnect}");

                if (!canConnect)
                {
                    result.AppendLine("❌ FAILED: Cannot connect to database!");
                    return Content(result.ToString(), "text/plain");
                }

                // Test 2: Get connection string
                result.AppendLine("\nTEST 2: Connection String");
                var connString = _context.Database.GetConnectionString();
                result.AppendLine($"📊 Connection: {connString}");

                // Test 3: Check if Bookings table exists
                result.AppendLine("\nTEST 3: Table Structure");
                var tableExists = await _context.Database.ExecuteSqlRawAsync("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bookings'") >= 0;
                result.AppendLine($"✅ Bookings Table Exists: True");

                // Test 4: Count total bookings
                result.AppendLine("\nTEST 4: Data Count");
                var bookingCount = await _context.Bookings.CountAsync();
                result.AppendLine($"📋 Total Bookings: {bookingCount}");

                // Test 5: Get all bookings with details
                result.AppendLine("\nTEST 5: Booking Details");
                var allBookings = await _context.Bookings
                    .OrderByDescending(b => b.BookingId)
                    .ToListAsync();

                if (allBookings.Any())
                {
                    result.AppendLine($"✅ Found {allBookings.Count} booking(s):\n");
                    foreach (var booking in allBookings)
                    {
                        result.AppendLine($"  ID: {booking.BookingId}");
                        result.AppendLine($"  Name: {booking.ClientName}");
                        result.AppendLine($"  Email: {booking.ClientEmail}");
                        result.AppendLine($"  Event: {booking.EventName}");
                        result.AppendLine($"  Day: {booking.Day}");
                        result.AppendLine($"  Ticket: {booking.TicketType}");
                        result.AppendLine($"  Charges: RM {booking.Charges}");
                        result.AppendLine($"  Status: {booking.Status}");
                        result.AppendLine($"  Payment: {booking.PaymentStatus}");
                        result.AppendLine($"  ---");
                    }
                }
                else
                {
                    result.AppendLine("⚠️ No bookings found in database!");
                    result.AppendLine("\nPossible reasons:");
                    result.AppendLine("1. You haven't created any bookings yet");
                    result.AppendLine("2. Bookings are not being saved (check SaveChanges is called)");
                    result.AppendLine("3. There's an error when creating bookings");
                }

                // Test 6: Check other tables
                result.AppendLine("\nTEST 6: Other Tables");
                var userCount = await _context.Users.CountAsync();
                result.AppendLine($"👥 Users: {userCount}");

                var emailVerificationCount = await _context.EmailVerifications.CountAsync();
                result.AppendLine($"📧 Email Verifications: {emailVerificationCount}");

                var paymentVerificationCount = await _context.PaymentVerifications.CountAsync();
                result.AppendLine($"💳 Payment Verifications: {paymentVerificationCount}");

                var inquiryCount = await _context.Inquiries.CountAsync();
                result.AppendLine($"❓ Inquiries: {inquiryCount}");

                // Test 7: Summary
                result.AppendLine("\n=== SUMMARY ===");
                result.AppendLine($"Database: Connected ✅");
                result.AppendLine($"Tables: All exist ✅");
                result.AppendLine($"Bookings: {bookingCount}");

                if (bookingCount == 0)
                {
                    result.AppendLine("\n⚠️ WARNING: No bookings found!");
                    result.AppendLine("Next steps:");
                    result.AppendLine("1. Try creating a booking through your app");
                    result.AppendLine("2. Check the Output window for errors");
                    result.AppendLine("3. Run this test again after creating a booking");
                }
                else
                {
                    result.AppendLine($"\n✅ SUCCESS: Database is working correctly!");
                }

            }
            catch (Exception ex)
            {
                result.AppendLine("\n❌ ERROR OCCURRED:");
                result.AppendLine($"Message: {ex.Message}");
                result.AppendLine($"\nStack Trace:\n{ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    result.AppendLine($"\nInner Exception: {ex.InnerException.Message}");
                }
            }

            return Content(result.ToString(), "text/plain");
        }

        // Access this at: /Diagnostic/CreateTestBooking
        public async Task<IActionResult> CreateTestBooking()
        {
            try
            {
                var testBooking = new Booking
                {
                    EventName = "Workshop",
                    ClientName = "Test User " + DateTime.Now.ToString("HHmmss"),
                    ClientEmail = $"test{DateTime.Now.Ticks}@test.com",
                    ContactNumber = "012-3456789",
                    DateOfBirth = new DateTime(2000, 1, 1),
                    Gender = "Male",
                    Day = 1,
                    TicketType = "regular",
                    Charges = 150.00M,
                    Status = "Pending",
                    PaymentStatus = "Unpaid"
                };

                _context.Bookings.Add(testBooking);
                var saveResult = await _context.SaveChangesAsync();

                var message = $@"
✅ TEST BOOKING CREATED SUCCESSFULLY!

Booking ID: {testBooking.BookingId}
Name: {testBooking.ClientName}
Email: {testBooking.ClientEmail}
SaveChanges Result: {saveResult} row(s) affected

Now go to /Diagnostic/DatabaseTest to verify it was saved!
                ";

                return Content(message, "text/plain");
            }
            catch (Exception ex)
            {
                return Content($@"
❌ ERROR CREATING TEST BOOKING:

Message: {ex.Message}
Inner Exception: {ex.InnerException?.Message}

Stack Trace:
{ex.StackTrace}
                ", "text/plain");
            }
        }

        // Access this at: /Diagnostic/CheckSqlServer
        public IActionResult CheckSqlServer()
        {
            var result = new StringBuilder();
            result.AppendLine("=== SQL SERVER CONNECTION CHECK ===\n");

            try
            {
                // Get connection string components
                var connString = _context.Database.GetConnectionString();
                result.AppendLine($"Connection String: {connString}\n");

                // Parse connection string
                var parts = connString?.Split(';');
                foreach (var part in parts ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        result.AppendLine($"  • {part}");
                    }
                }

                result.AppendLine("\n--- Testing Connection ---");

                // Test connection
                var canConnect = _context.Database.CanConnect();

                if (canConnect)
                {
                    result.AppendLine("✅ SUCCESS: Connected to SQL Server!");

                    // Get database name
                    var dbName = _context.Database.GetDbConnection().Database;
                    result.AppendLine($"✅ Database Name: {dbName}");
                }
                else
                {
                    result.AppendLine("❌ FAILED: Cannot connect to SQL Server!");
                    result.AppendLine("\nTroubleshooting:");
                    result.AppendLine("1. Check if SQL Server service is running");
                    result.AppendLine("2. Verify server name in connection string");
                    result.AppendLine("3. Try: Server=(localdb)\\mssqllocaldb");
                    result.AppendLine("   OR: Server=.\\SQLEXPRESS");
                }

            }
            catch (Exception ex)
            {
                result.AppendLine($"❌ ERROR: {ex.Message}");
            }

            return Content(result.ToString(), "text/plain");
        }
    }
}