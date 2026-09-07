using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Application.Common.Interfaces;

namespace PharmacyAvailability.Infrastructure.Security;

public class SmtpVerificationSender : IVerificationSender
{
    private static readonly HttpClient HttpClient = new();
    private readonly EmailSmtpOptions _emailOptions;
    private readonly SmsOptions _smsOptions;
    private readonly ILogger<SmtpVerificationSender> _logger;

    public SmtpVerificationSender(
        IOptions<EmailSmtpOptions> emailOptions,
        IOptions<SmsOptions> smsOptions,
        ILogger<SmtpVerificationSender> logger)
    {
        _emailOptions = emailOptions.Value;
        _smsOptions = smsOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailCodeAsync(string email, string code, CancellationToken ct = default)
    {
        if (_emailOptions.IsConfigured)
        {
            try
            {
                using var client = new SmtpClient(_emailOptions.Host, _emailOptions.Port)
                {
                    EnableSsl = _emailOptions.EnableSsl,
                    Credentials = new NetworkCredential(_emailOptions.UserName, _emailOptions.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 15000,
                };

                var fromAddress = new MailAddress(_emailOptions.FromEmail, _emailOptions.FromName, Encoding.UTF8);
                var toAddress = new MailAddress(email);

                using var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = $"رمز التحقق الخاص بك في منصة فارما فايندر | PharmaFinder Code: {code}",
                    Body = BuildHtmlEmailBody(email, code),
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                await client.SendMailAsync(message, ct);
                _logger.LogInformation("Real email verification code successfully sent to {Email}", email);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to deliver real SMTP email to {Email}. Verify Email:Smtp settings in appsettings.json. Verification Code was: {Code}",
                    email, code);
            }
        }
        else
        {
            _logger.LogInformation(
                "\n=======================================================\n" +
                "[REAL EMAIL READY] To: {Email}\n" +
                "Verification Code: {Code}\n" +
                "Expires in: 10 minutes\n" +
                "[CONFIGURATION NOTICE] SMTP is not yet configured with credentials. " +
                "Add Host, UserName, and App Password under 'Email:Smtp' in appsettings.json to send real live emails.\n" +
                "=======================================================",
                email, code);
        }
    }

    public async Task SendPhoneCodeAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        if (_smsOptions.IsConfigured)
        {
            try
            {
                var twilioUrl = $"https://api.twilio.com/2010-04-01/Accounts/{_smsOptions.AccountSid}/Messages.json";

                var request = new HttpRequestMessage(HttpMethod.Post, twilioUrl);
                var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_smsOptions.AccountSid}:{_smsOptions.AuthToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                var messageBody = $"رمز التحقق الخاص بك في منصة فارما فايندر هو: {code}. صالح لمدة 10 دقائق.\nPharmaFinder verification code: {code}";

                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("To", phoneNumber),
                    new KeyValuePair<string, string>("From", _smsOptions.FromPhoneNumber),
                    new KeyValuePair<string, string>("Body", messageBody),
                });

                var response = await HttpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Real SMS verification code successfully dispatched to {PhoneNumber}", phoneNumber);
                    return;
                }

                var errorResponse = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Twilio SMS dispatch failed with status {StatusCode}: {Response}. Code was: {Code}",
                    response.StatusCode, errorResponse, code);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to deliver real SMS to {PhoneNumber}. Verify Sms settings in appsettings.json. Verification Code was: {Code}",
                    phoneNumber, code);
            }
        }
        else
        {
            _logger.LogInformation(
                "\n=======================================================\n" +
                "[REAL SMS READY] To: {PhoneNumber}\n" +
                "Verification Code: {Code}\n" +
                "Expires in: 10 minutes\n" +
                "[CONFIGURATION NOTICE] SMS gateway is not yet configured. " +
                "Add AccountSid and AuthToken under 'Sms:Twilio' in appsettings.json to send live SMS.\n" +
                "=======================================================",
                phoneNumber, code);
        }
    }

    private static string BuildHtmlEmailBody(string email, string code)
    {
        return $@"
<!DOCTYPE html>
<html dir=""rtl"" lang=""ar"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>رمز التحقق من الحساب | فارما فايندر</title>
</head>
<body style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #1e293b;"">
  <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 580px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06); border: 1px solid #e2e8f0;"">
    <!-- Header -->
    <tr>
      <td style=""background-color: #0F3D64; padding: 28px; text-align: center;"">
        <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;"">
          منصة فارما فايندر
        </h1>
        <p style=""color: #93c5fd; margin: 6px 0 0; font-size: 13px;"">
          PharmaFinder Medicine Availability Platform
        </p>
      </td>
    </tr>

    <!-- Body -->
    <tr>
      <td style=""padding: 32px 28px;"">
        <h2 style=""font-size: 18px; color: #0F3D64; margin-top: 0;"">
          مرحباً بك،
        </h2>
        <p style=""font-size: 14px; line-height: 1.6; color: #334155; margin-bottom: 24px;"">
          لقد تلقينا طلباً لتأكيد وتفعيل حسابك على منصة <strong>فارما فايندر</strong> المرتبط بالبريد الإلكتروني: <span style=""color: #0F3D64; font-weight: 600;"">{email}</span>.
        </p>

        <p style=""font-size: 13px; color: #64748b; margin-bottom: 12px; text-align: center;"">
          استخدم رمز التحقق السري التالي لإتمام عملية التفعيل:
        </p>

        <!-- OTP Code Box -->
        <table align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px auto;"">
          <tr>
            <td style=""background-color: #f0fdf4; border: 2px dashed #059669; border-radius: 12px; padding: 16px 36px; text-align: center;"">
              <span style=""font-family: monospace, Courier, monospace; font-size: 34px; font-weight: bold; color: #059669; letter-spacing: 10px; display: inline-block;"">
                {code}
              </span>
            </td>
          </tr>
        </table>

        <p style=""font-size: 12px; color: #d97706; text-align: center; margin-top: 16px; font-weight: 500;"">
          ⏱️ تنتهي صلاحية هذا الرمز خلال <strong>10 دقائق</strong>.
        </p>

        <hr style=""border: none; border-top: 1px solid #f1f5f9; margin: 28px 0;"">

        <p style=""font-size: 12px; line-height: 1.5; color: #94a3b8; margin-bottom: 0;"">
          🛡️ <strong>تنبيه أمان:</strong> لا تشارك هذا الرمز مع أي شخص. إذا لم تكن أنت من قام بإنشاء هذا الطلب، يمكنك تجاهل هذه الرسالة بأمان.
        </p>
      </td>
    </tr>

    <!-- Footer -->
    <tr>
      <td style=""background-color: #f8fafc; padding: 18px 28px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 11px; color: #94a3b8;"">
        منصة فارما فايندر © {DateTime.UtcNow.Year} — نظام توفر وحجز الأدوية بالصيدليات.
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
