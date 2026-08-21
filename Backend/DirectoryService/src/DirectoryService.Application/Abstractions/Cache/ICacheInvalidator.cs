namespace DirectoryService.Application.Abstractions.Cache;

public interface ICacheInvalidator
{
    Task InvalidateAsync(IReadOnlyCollection<string> tags);
}
