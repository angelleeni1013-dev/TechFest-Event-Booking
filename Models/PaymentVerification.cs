using System.ComponentModel.DataAnnotations;

namespace A2_TechFest_2026.Models
{
    public class PaymentVerification
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsVerified { get; set; }
    }

    public class PaymentDetailsViewModel
    {
        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; } = string.Empty;

        // Credit/Debit Card Fields
        [Required(ErrorMessage = "Card number is required")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be 16 digits")]
        public string? CardNumber { get; set; }

        [Required(ErrorMessage = "Card holder name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? CardHolderName { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry date must be in MM/YY format")]
        public string? ExpiryDate { get; set; }

        [Required(ErrorMessage = "CVV is required")]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "CVV must be 3 digits")]
        public string? CVV { get; set; }

        // Touch 'n Go E-Wallet Field
        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Phone number must be 10-11 digits")]
        public string? PhoneNumber { get; set; }

        // Online Banking Fields
        [Required(ErrorMessage = "Bank name is required")]
        public string? BankName { get; set; }

        [Required(ErrorMessage = "Account number is required")]
        [RegularExpression(@"^\d{10,14}$", ErrorMessage = "Account number must be 10-14 digits")]
        public string? AccountNumber { get; set; }

        public int BookingId { get; set; }
    }

    public class ClientBookingViewModel
    {
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string ClientEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [RegularExpression(@"^01[0-9]-\d{7,8}$", ErrorMessage = "Contact Number is invalid. Format: 01X-XXXXXXX or 01X-XXXXXXXX")]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select at least one day")]
        public List<int> SelectedDays { get; set; } = new List<int>();

        [Required(ErrorMessage = "Ticket type is required")]
        public string TicketType { get; set; } = string.Empty;

        public decimal TotalCharges { get; set; }
    }
}