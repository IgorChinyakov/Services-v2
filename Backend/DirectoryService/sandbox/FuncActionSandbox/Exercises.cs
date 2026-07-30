namespace FuncActionSandbox;

public static class Exercises
{
    public static Task Exercise01_FuncBasics()
    {
        Console.WriteLine();
        Console.WriteLine("Exercise 01: Func basics");
        
        Func<int>

        // TODO 01:
        // Create Func<int, int> named square.
        // It should return x * x.
        // Then print square(5). Expected output: 25.
        return Task.CompletedTask;
    }

    public static Task Exercise02_ActionBasics()
    {
        Console.WriteLine();
        Console.WriteLine("Exercise 02: Action basics");

        // TODO 02:
        // Create Action<string> named print.
        // It should write the passed text to console.
        // Then call print("Hello from Action").
        return Task.CompletedTask;
    }

    public static async Task Exercise03_AsyncFunc()
    {
        Console.WriteLine();
        Console.WriteLine("Exercise 03: async Func");

        // TODO 03:
        // Create Func<Task<int>> named getNumberAsync.
        // It should await Task.Delay(100), then return 10.
        // Then await it and print the result.
        await Task.CompletedTask;
    }

    public static async Task Exercise04_ExecuteAsync()
    {
        Console.WriteLine();
        Console.WriteLine("Exercise 04: ExecuteAsync helper");

        // TODO 04:
        // Use ExecuteAsync below.
        // Pass a lambda that awaits Task.Delay(100) and returns 42.
        // Print the result.
        await Task.CompletedTask;
    }

    public static async Task Exercise05_FakeDbContext()
    {
        Console.WriteLine();
        Console.WriteLine("Exercise 05: fake DbContext helper");

        // TODO 05:
        // Use ExecuteFakeDbContextAsync below.
        // Pass a lambda that returns dbContext.Departments.Count.
        // Print the result.
        await Task.CompletedTask;
    }

    private static async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> action)
    {
        return await action();
    }

    private static async Task<TResult> ExecuteFakeDbContextAsync<TResult>(
        Func<FakeDbContext, Task<TResult>> action)
    {
        await using var dbContext = new FakeDbContext();

        return await action(dbContext);
    }
}
