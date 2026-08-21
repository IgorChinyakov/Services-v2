using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Exceptions;

public sealed class QueryCacheException : Exception
{
    public QueryCacheException(Error error)
        : base(error.GetMessage())
    {
        Error = error;
    }

    public Error Error { get; }
}
