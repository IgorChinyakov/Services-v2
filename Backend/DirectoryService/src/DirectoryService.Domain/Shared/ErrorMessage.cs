using System.Text.Json.Serialization;

namespace DirectoryService.Domain.Shared;

public record ErrorMessage(string Code, string Message, string? InvalidField = null);