using System;
using System.ComponentModel.DataAnnotations;

namespace A2_TechFest_2026.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Event Name is required")]
        [RegularExpression("^(Workshop|Competition|Talk|Booth)$", ErrorMessage = "Please select a valid event type")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client Name is required")]
        [MinLength(2, ErrorMessage = "Client Name must be at least 2 characters")]
        [MaxLength(50, ErrorMessage = "Client Name cannot exceed 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Client Name can only contain letters and spaces")]
        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email format")]
        public string ClientEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact Number is required")]
        [RegularExpression(@"^01[0-9]-\d{7,8}$", ErrorMessage = "Contact Number is invalid. Format: 01X-XXXXXXX or 01X-XXXXXXXX")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Please select a valid gender")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Day is required")]
        [Range(1, 3, ErrorMessage = "Day must be between 1 and 3")]
        public int Day { get; set; }

        [Required(ErrorMessage = "Ticket Type is required")]
        public string TicketType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Charges is required")]
        [Range(0.01, 10000, ErrorMessage = "Charges must be between RM 0.01 and RM 10,000")]
        public decimal Charges { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment Status is required")]
        [RegularExpression("^(Paid|Unpaid|Refunded)$", ErrorMessage = "Invalid payment status")]
        public string PaymentStatus { get; set; } = string.Empty;

        // FIXED: Updated to match the payment methods used in the views
        [RegularExpression("^(Credit/Debit Card|Touch 'n Go eWallet|Online Banking \\(FPX\\)|Online Banking)$",
            ErrorMessage = "Invalid payment method")]
        public string? PaymentMethod { get; set; }

        public decimal GetTicketPricePerDay()
        {
            return TicketType switch
            {
                "early-bird" => 120.00M,
                "regular" => 150.00M,
                "student" => 80.00M,
                _ => 0.00M
            };
        }

        public string GetTicketTypeDisplay()
        {
            return TicketType switch
            {
                "early-bird" => "Early Bird Ticket (RM120/day)",
                "regular" => "Regular Ticket (RM150/day)",
                "student" => "Student Pass (RM80/day)",
                _ => "Unknown"
            };
        }

        public int GetAge()
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}