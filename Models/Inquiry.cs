using System;
using System.ComponentModel.DataAnnotations;

namespace A2_TechFest_2026.Models
{
    public class Inquiry
    {
        public int InquiryId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string ClientEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public string? AdminReply { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Responded, Solved

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}