using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DirectoryService.IntegrationTests;

internal sealed class DepartmentUpdateCommandInterceptor : DbCommandInterceptor
{
    private int _departmentUpdateCount;

    public int DepartmentUpdateCount => Volatile.Read(ref _departmentUpdateCount);

    public void Reset() => Interlocked.Exchange(ref _departmentUpdateCount, 0);

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.TrimStart().StartsWith(
                "UPDATE departments",
                StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _departmentUpdateCount);
        }

        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }
}
