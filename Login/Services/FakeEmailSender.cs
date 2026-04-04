namespace AuthApi.Services;

public class FakeEmailSender : IEmailSender
{
    private readonly ILogger<FakeEmailSender> _logger;

    public FakeEmailSender(ILogger<FakeEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string message)
    {
        _logger.LogInformation("--- FAKE EMAIL SENT ---");
        _logger.LogInformation($"To: {toEmail}");
        _logger.LogInformation($"Subject: {subject}");
        _logger.LogInformation($"Body: {message}");
        _logger.LogInformation("-----------------------");
        
        return Task.CompletedTask;
    }
}
