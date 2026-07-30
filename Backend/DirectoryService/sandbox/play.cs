#pragma warning disable SA1400, SA1402, SA1513, SA1515, SA1519, SA1649

using System.Text;

Console.WriteLine("C# delegate playground");
Console.WriteLine("Run: dotnet run --file sandbox/play.cs");
Console.WriteLine();

PrintSeparator("00 Example Func");

// Example: Func takes input and returns output.
Func<int, int> increment = x => x + 1;
Run("00 example Func increment", () => Expect(6, increment(5)));

PrintSeparator("01 Func square");

// TODO 01: return x multiplied by itself.
Func<int, int> square = x => x * x;
Run("01 Func square", () => Expect(25, square(5)));

PrintSeparator("02 Func isEven");

// TODO 02: return true if number is even.
Func<int, bool> isEven = x => x % 2 == 0;
Run("02 Func isEven", () => Expect(true, isEven(10)));

PrintSeparator("03 Func add");

// TODO 03: return a + b.
Func<int, int, int> add = (a, b) => a + b;
Run("03 Func add", () => Expect(42, add(20, 22)));

PrintSeparator("04 Action writeUpperLog");

// Example: Action performs work and returns void.
var log = new List<string>();
Action<string> writeLog = message => log.Add(message);
writeLog("first message");

// TODO 04: add the upper-case message to upperLog.
var upperLog = new List<string>();
Action<string> writeUpperLog = message => { upperLog.Add(message.ToUpper()); };
writeUpperLog("hello");
Run("04 Action writeUpperLog", () => Expect("HELLO", upperLog.Single()));

PrintSeparator("05 ApplyTwice");
static int ApplyTwice(int value, Func<int, int> operation)
{
    // TODO 05: apply operation twice.
    return operation(operation(value));
}

Run("05 ApplyTwice", () => Expect(12, ApplyTwice(3, x => x * 2)));

PrintSeparator("06 ForEach with Action");
static void ForEach<T>(IEnumerable<T> items, Action<T> action)
{
    foreach (var item in items)
    {
        action(item);
    }
}

Run("06 ForEach with Action", () =>
{
    var result = new List<int>();

    ForEach([1, 2, 3], x => result.Add(x * 10));

    ExpectSequence([10, 20, 30], result);
});

PrintSeparator("07 Map with Func");
static IEnumerable<TResult> Map<TSource, TResult>(
    IEnumerable<TSource> items,
    Func<TSource, TResult> mapper)
{
    // TODO 07: return a new sequence where every item is transformed by mapper.
    var result = new List<TResult>();
    foreach (var item in items)
        result.Add(mapper(item));
    return result;
}

Run("07 Map with Func", () => ExpectSequence(["#1", "#2", "#3"], Map([1, 2, 3], x => $"#{x}")));

PrintSeparator("08 Filter with predicate");
static IEnumerable<T> Filter<T>(
    IEnumerable<T> items,
    Func<T, bool> predicate)
{
    // TODO 08: return only items where predicate returns true.
    var result = new List<T>();
    foreach (var item in items)
    {
        if (predicate(item))
        {
            result.Add(item);
        }
    }

    return result;
}

Run("08 Filter with predicate", () => ExpectSequence([2, 4], Filter([1, 2, 3, 4, 5], x => x % 2 == 0)));

PrintSeparator("09 ExecuteIf with Action");
static void ExecuteIf(bool condition, Action action)
{
    // TODO 09: execute action only when condition is true.
    if (condition)
        action();
}

Run("09 ExecuteIf with Action", () =>
{
    var calls = 0;

    ExecuteIf(false, () => calls++);
    ExecuteIf(true, () => calls += 10);

    Expect(10, calls);
});

PrintSeparator("10 CreateMultiplier returns Func");
static Func<int, int> CreateMultiplier(int multiplier)
{
    // TODO 10: return a Func that multiplies input by multiplier.
    return x => multiplier * x;
}

Run("10 CreateMultiplier returns Func", () =>
{
    var multiplyByThree = CreateMultiplier(3);

    Expect(21, multiplyByThree(7));
});

PrintSeparator("11 Compose two Func delegates");
static Func<int, int> Compose(Func<int, int> first, Func<int, int> second)
{
    // TODO 11: return a Func where first is applied, then second is applied.
    return x => second(first(x));
}

Run("11 Compose two Func delegates", () =>
{
    var operation = Compose(x => x + 2, x => x * 10);

    Expect(50, operation(3));
});

PrintSeparator("12 CountBy predicate");
static int CountBy<T>(IEnumerable<T> items, Func<T, bool> predicate)
{
    // TODO 12: count items where predicate returns true.
    var count = 0;
    foreach (var item in items)
    {
        if (predicate(item))
        {
            count++;
        }
    }

    return count;
}

Run("12 CountBy predicate", () =>
{
    var words = new[] { "apple", "pear", "avocado", "banana" };

    Expect(2, CountBy(words, word => word.StartsWith("a")));
});

PrintSeparator("13 Reduce with Func");
static TResult Reduce<TSource, TResult>(
    IEnumerable<TSource> items,
    TResult seed,
    Func<TResult, TSource, TResult> reducer)
{
    // TODO 13: start with seed and apply reducer to every item.
    var result = seed;
    foreach (var item in items)
    {
        result = reducer(result, item);
    }
    return result;
}

Run("13 Reduce with Func", () =>
{
    var sum = Reduce([1, 2, 3, 4], 0, (currentSum, item) => currentSum + item);

    Expect(10, sum);
});

PrintSeparator("14 FilterMap pipeline");
static IEnumerable<TResult> FilterMap<TSource, TResult>(
    IEnumerable<TSource> items,
    Func<TSource, bool> predicate,
    Func<TSource, TResult> mapper)
{
    // TODO 14: filter items by predicate, then map accepted items.
    var result = new List<TResult>();
    foreach (var item in items)
    {
        if (predicate(item))
        {
            result.Add(mapper(item));
        }
    }

    return result;
}

Run("14 FilterMap pipeline", () =>
{
    var result = FilterMap(
        ["cat", "horse", "dog", "tiger"],
        word => word.Length > 3,
        word => word.ToUpperInvariant());

    ExpectSequence(["HORSE", "TIGER"], result);
});

PrintSeparator("15 BuildValidator returns Func");
static Func<T, bool> BuildValidator<T>(params Func<T, bool>[] rules)
{
    // TODO 15: return a Func that is true only when all rules return true.
    return value =>
    {
        foreach (var rule in rules)
        {
            if (!rule(value))
                return false;
        }

        return true;
    };
}

Run("15 BuildValidator returns Func", () =>
{
    var isValidName = BuildValidator<string>(
        value => !string.IsNullOrWhiteSpace(value),
        value => value.Length >= 3,
        value => value.All(char.IsLetter));

    Expect(true, isValidName("Alex"));
    Expect(false, isValidName("A1"));
});

PrintSeparator("16 TryExecute with error Action");
static T TryExecute<T>(Func<T> operation, Action<Exception> onError, T fallback)
{
    // TODO 16: execute operation. If it throws, call onError and return fallback.
    try
    {
        return operation();
    }
    catch (Exception ex)
    {
        onError(ex);
    }

    return fallback;
}

Run("16 TryExecute with error Action", () =>
{
    var errors = new List<string>();

    var result = TryExecute(
        () => int.Parse("not-number"),
        exception => errors.Add(exception.GetType().Name),
        fallback: -1);

    Expect(-1, result);
    Expect("FormatException", errors.Single());
});

PrintSeparator("17 RunPipeline with Func steps");
static T RunPipeline<T>(T value, params Func<T, T>[] steps)
{
    // TODO 17: pass value through every step in order.
    foreach (var step in steps)
    {
        value = step(value);
    }

    return value;
}

Run("17 RunPipeline with Func steps", () =>
{
    var result = RunPipeline(
        3,
        x => x + 1,
        x => x * 10,
        x => x - 5);

    Expect(35, result);
});

PrintSeparator("18 RetryAsync with Func<Task<T>>");
static async Task<T> RetryAsync<T>(
    Func<Task<T>> operation,
    int attempts,
    Action<int> onFailedAttempt)
{
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attempts);

    // TODO 18: try operation up to attempts times.
    // If an attempt fails, call onFailedAttempt with the attempt number.
    // If every attempt fails, rethrow the last exception.
    for (var i = 1; i <= attempts; i++)
    {
        try
        {
            return await operation();
        }
        catch (Exception)
        {
            onFailedAttempt(i);

            if (i == attempts)
                throw;
        }
    }

    throw new InvalidOperationException("Unreachable code.");
}

await RunAsync("18 RetryAsync with Func<Task<T>>", async () =>
{
    var calls = 0;
    var failedAttempts = new List<int>();

    var result = await RetryAsync(
        async () =>
        {
            await Task.Delay(1);
            calls++;

            if (calls < 3)
            {
                throw new InvalidOperationException("Temporary failure");
            }

            return "success";
        },
        attempts: 3,
        onFailedAttempt: attempt => failedAttempts.Add(attempt));

    Expect("success", result);
    ExpectSequence([1, 2], failedAttempts);
});

PrintSeparator("19 ExecuteWithScopeAsync<TResult>");
static async Task<TResult> ExecuteWithScopeAsync<TResult>(
    List<string> events,
    Func<FakeScope, Task<TResult>> action)
{
    // TODO 19: create FakeScope, pass it into action, return action result.
    // Important: scope must be disposed after action is finished.
    var scope = new FakeScope(events);

    var result = await action(scope);

    scope.DisposeAsync();
    return result;
}

await RunAsync("19 ExecuteWithScopeAsync<TResult>", async () =>
{
    var events = new List<string>();

    var result = await ExecuteWithScopeAsync(events, async scope =>
    {
        events.Add("action-start");
        await Task.Delay(1);

        return scope.Id;
    });

    Expect("scope-1", result);
    ExpectSequence(["scope-created", "action-start", "scope-disposed"], events);
});

PrintSeparator("20 ExecuteWithScopeWithoutResultAsync");
static async Task ExecuteWithScopeWithoutResultAsync(
    List<string> events,
    Func<FakeScope, Task> action)
{
    // TODO 20: reuse generic ExecuteWithScopeAsync<TResult>.
    // In a normal class this could be an overload with the same name.
    await ExecuteWithScopeAsync(events, async scope =>
    {
        await action(scope);
        return true;
    });
}

await RunAsync("20 ExecuteWithScopeWithoutResultAsync", async () =>
{
    var events = new List<string>();

    await ExecuteWithScopeWithoutResultAsync(events, async scope =>
    {
        await Task.Delay(1);
        events.Add($"inside:{scope.Id}");
    });

    ExpectSequence(["scope-created", "inside:scope-1", "scope-disposed"], events);
});

PrintSeparator("21 ExecuteDatabaseAsync<TResult>");
static async Task<TResult> ExecuteDatabaseAsync<TResult>(
    List<string> events,
    Func<FakeDatabase, Task<TResult>> action)
{
    // TODO 21: create scope via ExecuteWithScopeAsync, resolve database from scope.Services,
    // then pass database into action.
    return await ExecuteWithScopeAsync(events, async scope =>
    {
        var db = scope.Services.GetDatabase();

        var result = await action(db);

        return result;
    });
}

await RunAsync("21 ExecuteDatabaseAsync<TResult>", async () =>
{
    var events = new List<string>();

    var count = await ExecuteDatabaseAsync(events, async database =>
    {
        await database.AddAsync("department");
        await database.AddAsync("location");

        return await database.CountAsync();
    });

    Expect(2, count);
    ExpectSequence(
        ["scope-created", "db-resolved", "db-add:department", "db-add:location", "scope-disposed"],
        events);
});

PrintSeparator("22 ExecuteDatabaseWithoutResultAsync");
static async Task ExecuteDatabaseWithoutResultAsync(
    List<string> events,
    Func<FakeDatabase, Task> action)
{
    // TODO 22: reuse generic ExecuteDatabaseAsync<TResult>.
    // In a normal class this could be an overload with the same name.
    await ExecuteDatabaseAsync(events, async (db) =>
    {
        await action(db);

        return true;
    });
}

await RunAsync("22 ExecuteDatabaseWithoutResultAsync", async () =>
{
    var events = new List<string>();

    await ExecuteDatabaseWithoutResultAsync(events, async database =>
    {
        await database.AddAsync("position");
    });

    ExpectSequence(["scope-created", "db-resolved", "db-add:position", "scope-disposed"], events);
});

PrintSeparator("23 ExecuteHandlerAsync<TResult>");
static async Task<TResult> ExecuteHandlerAsync<TResult>(
    List<string> events,
    Func<FakeCreateHandler, Task<TResult>> action)
{
    // TODO 23: create scope, resolve handler from scope.Services, execute action(handler).
    var scope = new FakeScope(events);
    var handler = scope.Services.GetCreateHandler();
    var result = await action(handler);

    scope.DisposeAsync();
    return result;
}

await RunAsync("23 ExecuteHandlerAsync<TResult>", async () =>
{
    var events = new List<string>();

    var result = await ExecuteHandlerAsync(events, handler => handler.HandleAsync("alex"));

    Expect("created:alex", result);
    ExpectSequence(["scope-created", "handler-resolved", "db-add:alex", "scope-disposed"], events);
});

PrintSeparator("24 RunAsyncPipeline with Func steps");
static async Task<T> RunAsyncPipeline<T>(T value, params Func<T, Task<T>>[] steps)
{
    // TODO 24: pass value through every async step in order.
    foreach (var step in steps)
    {
        value = await step(value);
    }
    return value;
}

await RunAsync("24 RunAsyncPipeline with Func steps", async () =>
{
    var result = await RunAsyncPipeline(
        "start",
        async value =>
        {
            await Task.Delay(1);

            return $"{value}-load";
        },
        async value =>
        {
            await Task.Delay(1);

            return value.ToUpperInvariant();
        },
        async value =>
        {
            await Task.Delay(1);

            return $"[{value}]";
        });

    Expect("[START-LOAD]", result);
});

PrintSeparator("25 ExecuteWithAsyncLoader nested Func");
static async Task<TResult> ExecuteWithAsyncLoader<TResult>(
    Func<Func<string, Task<string>>, Task<TResult>> action)
{
    // TODO 25: create async loader Func<string, Task<string>>.
    // Loader should return $"loaded:{key}" after small delay.
    // Then pass loader into action and return action result.
    Func<string, Task<string>> loader = async (key) =>
    {
        await Task.Delay(1);

        return $"loaded:{key}";
    };

    return await action(loader);
}

await RunAsync("25 ExecuteWithAsyncLoader nested Func", async () =>
{
    var result = await ExecuteWithAsyncLoader(async load =>
    {
        var department = await load("department");
        var location = await load("location");

        return $"{department}|{location}";
    });

    Expect("loaded:department|loaded:location", result);
});

Console.WriteLine();
Console.WriteLine("Edit TODOs, run again, repeat. Small steps win.");

static void PrintSeparator(string title)
{
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine(title);
    Console.WriteLine("----------------------------------------");
}

static void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

static async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"FAIL {name}: {exception.Message}");
    }
}

static void Expect<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected {expected}, actual {actual}");
    }
}

static void ExpectSequence<T>(IEnumerable<T> expected, IEnumerable<T> actual)
{
    var expectedArray = expected.ToArray();
    var actualArray = actual.ToArray();

    if (!expectedArray.SequenceEqual(actualArray))
    {
        throw new InvalidOperationException(
            $"expected [{Format(expectedArray)}], actual [{Format(actualArray)}]");
    }
}

static string Format<T>(IEnumerable<T> values)
{
    var builder = new StringBuilder();

    foreach (var value in values)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(value);
    }

    return builder.ToString();
}

sealed class FakeScope : IAsyncDisposable
{
    private readonly List<string> _events;

    public FakeScope(List<string> events)
    {
        _events = events;
        Services = new FakeServices(events);

        _events.Add("scope-created");
    }

    public string Id => "scope-1";

    public FakeServices Services { get; }

    public ValueTask DisposeAsync()
    {
        _events.Add("scope-disposed");

        return ValueTask.CompletedTask;
    }
}

sealed class FakeServices
{
    private readonly List<string> _events;

    public FakeServices(List<string> events)
    {
        _events = events;
        Database = new FakeDatabase(events);
    }

    public FakeDatabase Database { get; }

    public FakeDatabase GetDatabase()
    {
        _events.Add("db-resolved");

        return Database;
    }

    public FakeCreateHandler GetCreateHandler()
    {
        _events.Add("handler-resolved");

        return new FakeCreateHandler(Database);
    }
}

sealed class FakeDatabase
{
    private readonly List<string> _events;
    private readonly List<string> _items = [];

    public FakeDatabase(List<string> events)
    {
        _events = events;
    }

    public async Task AddAsync(string value)
    {
        await Task.Delay(1);

        _items.Add(value);
        _events.Add($"db-add:{value}");
    }

    public async Task<int> CountAsync()
    {
        await Task.Delay(1);

        return _items.Count;
    }
}

sealed class FakeCreateHandler
{
    private readonly FakeDatabase _database;

    public FakeCreateHandler(FakeDatabase database)
    {
        _database = database;
    }

    public async Task<string> HandleAsync(string name)
    {
        await _database.AddAsync(name);

        return $"created:{name}";
    }
}
