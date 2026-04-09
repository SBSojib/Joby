using Joby.Application.DTOs.Profile;

namespace Joby.Application.Interfaces;

public interface IResumeParser
{
    Task<(string text, ParsedResumeData data)> ParseAsync(Stream fileStream, string fileName, string contentType);
}





