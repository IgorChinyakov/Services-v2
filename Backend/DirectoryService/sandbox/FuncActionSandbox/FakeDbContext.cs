namespace FuncActionSandbox;

public sealed class FakeDbContext : IAsyncDisposable
{
    public List<string> Departments { get; } =
    [
        "Sales",
        "Development",
        "Support",
    ];

    public ValueTask DisposeAsync()
    {
        Console.WriteLine("FakeDbContext disposed");
        return ValueTask.CompletedTask;
    }
}
