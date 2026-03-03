using A2_TechFest_2026.Models;

namespace A2_TechFest_2026.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendBookingConfirmationAsync(string toEmail, Booking booking);
        Task SendPaymentConfirmationAsync(string toEmail, Booking booking);
        Task SendVerificationCodeAsync(string toEmail, string verificationCode);
        Task SendPaymentOtpAsync(string toEmail, string otpCode, string bookingReference);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendInquiryConfirmationAsync(string toEmail, Inquiry inquiry);
        Task SendInquiryNotificationAsync(Inquiry inquiry);

    }
}