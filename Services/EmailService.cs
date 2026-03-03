using System.Net;
using System.Net.Mail;
using A2_TechFest_2026.Models;
using Humanizer;
using Microsoft.CodeAnalysis;

namespace A2_TechFest_2026.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];

                using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("====================================");
                Console.WriteLine("EMAIL SEND FAILED!");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("====================================");
                throw; // Re-throw to see in RegisterModel
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Welcome to TechFest 2026! 🎉";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                   color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to TechFest 2026!</h1>
        </div>
        <div class='content'>
            <h2>Hello {userName}!</h2>
            <p>Thank you for registering with TechFest 2026. We're excited to have you join us!</p>
            
            <p><strong>What's Next?</strong></p>
            <ul>
                <li>Browse our exciting events and workshops</li>
                <li>Register for your preferred sessions</li>
                <li>Complete your payment to secure your spot</li>
            </ul>
            
            <p><strong>Event Dates:</strong> July 24-26, 2026</p>
            <p><strong>Venue:</strong> UOW Malaysia KDU</p>
            
            <p>If you have any questions, feel free to contact us at techfest2026@uow.edu.my</p>
            
            <p>See you at TechFest!</p>
            <p><strong>The TechFest 2026 Team</strong></p>
        </div>
        <div class='footer'>
            <p>© 2026 TechFest - UOW Malaysia KDU. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendBookingConfirmationAsync(string toEmail, Booking booking)
        {
            var subject = $"Booking Confirmed - {booking.EventName}";

            var ticketTypeName = booking.TicketType switch
            {
                "early-bird" => "Early Bird",
                "regular" => "Regular",
                "student" => "Student",
                _ => booking.TicketType
            };

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                   color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .booking-details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
        .total {{ font-size: 24px; color: #667eea; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Booking Confirmed!</h1>
        </div>
        <div class='content'>
            <h2>Hello {booking.ClientName}!</h2>
            <p>Your booking for <strong>{booking.EventName}</strong> has been confirmed.</p>
            
            <div class='booking-details'>
                <h3>Booking Details:</h3>
                <div class='detail-row'><strong>Booking ID:</strong> #{booking.BookingId}</div>
                <div class='detail-row'><strong>Ticket Type:</strong> {ticketTypeName}</div>
                <div class='detail-row'><strong>Days:</strong> {booking.Day} day(s)</div>
                <div class='detail-row'><strong>Status:</strong> {booking.Status}</div>
                <div class='detail-row'><strong>Payment:</strong> {booking.PaymentStatus}</div>
                <div style='margin-top: 20px;'><strong>Total:</strong> <span class='total'>RM {booking.Charges:N2}</span></div>
            </div>
            
            <p>We look forward to seeing you at TechFest 2026!</p>
            <p><strong>The TechFest Team</strong></p>
        </div>
        <div class='footer'>
            <p>© 2026 TechFest - UOW Malaysia KDU. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendPaymentConfirmationAsync(string toEmail, Booking booking)
        {
            var subject = "Payment Received - Thank You!";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #27ae60 0%, #2ecc71 100%); 
                   color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .amount {{ font-size: 32px; color: #27ae60; font-weight: bold; text-align: center; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>💳 Payment Confirmed!</h1>
        </div>
        <div class='content'>
            <h2>Hello {booking.ClientName}!</h2>
            <p>We have received your payment for <strong>{booking.EventName}</strong>.</p>
            
            <div class='amount'>RM {booking.Charges:N2}</div>
            <p style='text-align: center;'><strong>Payment Method:</strong> {booking.PaymentMethod}</p>
            
            <p>Thank you for choosing TechFest 2026!</p>
            <p><strong>The TechFest Team</strong></p>
        </div>
        <div class='footer'>
            <p>© 2026 TechFest - UOW Malaysia KDU. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendVerificationCodeAsync(string toEmail, string verificationCode)
        {
            string subject = "Verify Your Email - TechFest 2026";
            string body = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
            .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
            .code-box {{ background: white; border: 2px dashed #667eea; padding: 20px; margin: 20px 0; text-align: center; border-radius: 8px; }}
            .code {{ font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: 8px; }}
            .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
            .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h1>📧 Email Verification</h1>
                <p>TechFest 2026</p>
            </div>
            <div class='content'>
                <h2>Hello!</h2>
                <p>Thank you for registering with TechFest 2026. To complete your registration, please verify your email address using the code below:</p>
                
                <div class='code-box'>
                    <p style='margin: 0; color: #666; font-size: 14px;'>Your Verification Code</p>
                    <div class='code'>{verificationCode}</div>
                    <p style='margin: 10px 0 0 0; color: #666; font-size: 12px;'>Valid for 15 minutes</p>
                </div>

                <div class='warning'>
                    <strong>⚠️ Security Notice:</strong>
                    <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                        <li>Never share this code with anyone</li>
                        <li>TechFest will never ask for this code via phone or email</li>
                        <li>This code expires in 15 minutes</li>
                    </ul>
                </div>

                <p>If you didn't request this verification, please ignore this email.</p>
                
                <p style='margin-top: 30px;'>Best regards,<br><strong>TechFest 2026 Team</strong></p>
            </div>
            <div class='footer'>
                <p>This is an automated email. Please do not reply.</p>
                <p>&copy; 2026 TechFest. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }


        public async Task SendPaymentOtpAsync(string toEmail, string otpCode, string bookingReference)
        {
            string subject = "Payment Verification OTP - TechFest 2026";
            string body = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
            .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
            .otp-box {{ background: white; border: 3px dashed #667eea; padding: 25px; margin: 20px 0; text-align: center; border-radius: 10px; }}
            .otp {{ font-size: 36px; font-weight: bold; color: #667eea; letter-spacing: 10px; }}
            .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
            .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h1>💳 Payment Verification</h1>
                <p>TechFest 2026</p>
            </div>
            <div class='content'>
                <h2>One-Time Password (OTP)</h2>
                <p>You are completing payment for booking: <strong>{bookingReference}</strong></p>
                <p>Please enter the OTP code below to verify your payment:</p>
                
                <div class='otp-box'>
                    <p style='margin: 0; color: #666; font-size: 14px;'>Your OTP Code</p>
                    <div class='otp'>{otpCode}</div>
                    <p style='margin: 10px 0 0 0; color: #666; font-size: 12px;'>Valid for 10 minutes</p>
                </div>

                <div class='warning'>
                    <strong>⚠️ Security Notice:</strong>
                    <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                        <li>Never share this OTP with anyone</li>
                        <li>TechFest staff will never ask for this code</li>
                        <li>This code expires in 10 minutes</li>
                        <li>Only enter this code on the TechFest payment page</li>
                    </ul>
                </div>

                <p>If you didn't request this payment, please ignore this email and contact us immediately.</p>
                
                <p style='margin-top: 30px;'>Best regards,<br><strong>TechFest 2026 Team</strong></p>
            </div>
            <div class='footer'>
                <p>This is an automated email. Please do not reply.</p>
                <p>&copy; 2026 TechFest - UOW Malaysia KDU. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }


        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            string subject = "Reset Your Password - TechFest 2026";
            string body = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
            .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
            .button {{ display: inline-block; padding: 15px 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; text-decoration: none; border-radius: 8px; font-weight: bold; margin: 20px 0; }}
            .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
            .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h1>🔐 Password Reset Request</h1>
                <p>TechFest 2026</p>
            </div>
            <div class='content'>
                <h2>Reset Your Password</h2>
                <p>We received a request to reset your password for your TechFest 2026 account.</p>
                <p>Click the button below to reset your password:</p>
                
                <div style='text-align: center;'>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                </div>

                <p style='margin-top: 20px;'><small>Or copy and paste this link into your browser:</small></p>
                <p style='word-break: break-all; background: white; padding: 10px; border-radius: 5px; font-size: 12px;'>{resetLink}</p>

                <div class='warning'>
                    <strong>⚠️ Security Notice:</strong>
                    <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                        <li>This link expires in 1 hour</li>
                        <li>If you didn't request this, please ignore this email</li>
                        <li>Never share this link with anyone</li>
                    </ul>
                </div>

                <p style='margin-top: 30px;'>Best regards,<br><strong>TechFest 2026 Team</strong></p>
            </div>
            <div class='footer'>
                <p>This is an automated email. Please do not reply.</p>
                <p>&copy; 2026 TechFest - UOW Malaysia KDU. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInquiryConfirmationAsync(string toEmail, Inquiry inquiry)
        {
            string subject = "We Received Your Inquiry - TechFest 2026";
            string body = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
            .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h1>Inquiry Received</h1>
            </div>
            <div class='content'>
                <h2>Thank you for contacting us!</h2>
                <p>Dear Customer,</p>
                <p>We have received your inquiry and our team will get back to you within 24-48 hours.</p>
                <h3>Your Inquiry:</h3>
                <p><strong>Subject:</strong> {inquiry.Subject}</p>
                <p><strong>Message:</strong> {inquiry.Message}</p>
                <p><strong>Inquiry ID:</strong> #{inquiry.InquiryId}</p>
                <p style='margin-top: 30px;'>Best regards,<br><strong>TechFest 2026 Team</strong></p>
            </div>
        </div>
    </body>
    </html>
    ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendInquiryNotificationAsync(Inquiry inquiry)
        {
            string subject = $"New Inquiry - {inquiry.Subject}";
            string body = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <h2>New Inquiry Received</h2>
            <p><strong>From:</strong> {inquiry.ClientEmail}</p>
            <p><strong>Subject:</strong> {inquiry.Subject}</p>
            <p><strong>Message:</strong> {inquiry.Message}</p>
            <p><strong>ID:</strong> #{inquiry.InquiryId}</p>
            <p><strong>Date:</strong> {inquiry.CreatedAt:dd/MM/yyyy HH:mm}</p>
        </div>
    </body>
    </html>
    ";

            await SendEmailAsync("techfestevent2026@gmail.com", subject, body);
        }
    }
}