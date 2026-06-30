using CSharpFunctionalExtensions;
using DirectoryService.Domain.Shared;

namespace DirectoryService.Application.Abstractions.Handlers;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<UnitResult<Error>> Handle(
        TCommand command,
        CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand
{
    Task<Result<TResponse, Error>> Handle(
        TCommand command,
        CancellationToken cancellationToken);
}
