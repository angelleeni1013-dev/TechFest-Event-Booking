namespace A2_TechFest_2026.Models
{
    public class EmailVerification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsVerified { get; set; }
    }
}