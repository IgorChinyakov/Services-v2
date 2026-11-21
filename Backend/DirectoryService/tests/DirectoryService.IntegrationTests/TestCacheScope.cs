using System.Globalization;

namespace DirectoryService.IntegrationTests;

internal sealed class TestCacheScope
{
    private readonly string _id = Guid.NewGuid().ToString("N");
    private long _generation;

    public string Prefix => $"test:{_id}:{Volatile.Read(ref _generation).ToString(CultureInfo.InvariantCulture)}:";

    public void Reset() => Interlocked.Increment(ref _generation);
}
