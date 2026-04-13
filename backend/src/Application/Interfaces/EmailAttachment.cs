namespace Joby.Application.Interfaces;

public sealed record EmailAttachment(string FileName, byte[] Content, string? ContentType);
