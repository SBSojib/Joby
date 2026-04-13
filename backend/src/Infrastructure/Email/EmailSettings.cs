namespace Joby.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Joby";
    public SmtpSettings Smtp { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
}
