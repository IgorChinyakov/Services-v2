using FuncActionSandbox;

Console.WriteLine("Func/Action sandbox");
Console.WriteLine("===================");

await Exercises.Exercise01_FuncBasics();
await Exercises.Exercise02_ActionBasics();
await Exercises.Exercise03_AsyncFunc();
await Exercises.Exercise04_ExecuteAsync();
await Exercises.Exercise05_FakeDbContext();

Console.WriteLine();
Console.WriteLine("Sandbox finished.");
