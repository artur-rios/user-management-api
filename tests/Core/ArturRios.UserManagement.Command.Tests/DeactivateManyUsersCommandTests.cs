using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class DeactivateManyUsersCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly DeactivateManyUsersCommandHandler _handler;

    public DeactivateManyUsersCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new DeactivateManyUsersCommandHandler(new DeactivateManyUsersCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenActiveUsers_WhenDeactivatingThem_ThenEachIsDeactivatedAndStored()
    {
        // When
        var result = _handler.Handle(new DeactivateManyUsersCommand { UserIds = _dataMock.ActiveIds });

        // Then
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Data);
        Assert.Equal(_dataMock.ActiveIds, result.Data.DeactivatedIds);
        Assert.Empty(result.Data.FailedDeactivationIds);
        Assert.Empty(result.Data.NotFoundIds);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => !user.Active)), Times.Exactly(_dataMock.ActiveIds.Count));
    }

    [UnitFact]
    public void GivenUsersAlreadyInactive_WhenDeactivatingThem_ThenEachFailureIsReported()
    {
        // Given
        var ids = _dataMock.InactiveIds;

        // When
        var result = _handler.Handle(new DeactivateManyUsersCommand { UserIds = ids });

        // Then
        Assert.False(result.Success);
        Assert.Equal(
            [$"Users with IDs {string.Join(", ", ids)} cannot be deactivated", .. ids.Select(id => $"User with Id {id}: User already inactive")],
            result.Errors);
        Assert.Equal(ids, result.Data?.FailedDeactivationIds);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenIdsNotOnTheDatabase_WhenDeactivatingThem_ThenEachIsReportedAsNotFound()
    {
        // Given
        var ids = _dataMock.NonexistentIds;

        // When
        var result = _handler.Handle(new DeactivateManyUsersCommand { UserIds = ids });

        // Then
        Assert.False(result.Success);
        Assert.Equal(
            [$"Users with IDs {string.Join(", ", ids)} not found", .. ids.Select(id => $"User with Id {id} not found")],
            result.Errors);
        Assert.Equal(ids, result.Data?.NotFoundIds);
    }

    [UnitFact]
    public void GivenSomeUsersThatCanBeDeactivated_WhenDeactivatingThem_ThenTheyAreDeactivatedAndTheOthersFailTheCommand()
    {
        // Given
        var active = _dataMock.ActiveIds.First();
        var inactive = _dataMock.InactiveIds.First();
        var missing = _dataMock.NonexistentIds.First();

        // When
        var result = _handler.Handle(new DeactivateManyUsersCommand { UserIds = [active, inactive, missing] });

        // Then
        Assert.False(result.Success);
        Assert.Equal(
            [
                $"Users with IDs {missing} not found. Users with IDs {inactive} cannot be deactivated",
                $"User with Id {inactive}: User already inactive",
                $"User with Id {missing} not found"
            ],
            result.Errors);
        Assert.NotNull(result.Data);
        Assert.Equal([active], result.Data.DeactivatedIds);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => user.Id == active)), Times.Once);
    }

    [UnitFact]
    public void GivenNoIds_WhenDeactivating_ThenItIsRefusedAsInvalid()
    {
        // When
        var result = _handler.Handle(new DeactivateManyUsersCommand { UserIds = [] });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Null(result.Data);
    }

    [UnitFact]
    public void GivenAnIdBelowOne_WhenDeactivating_ThenItIsRefusedAsInvalid()
    {
        // When
        var result = _handler.Handle(new DeactivateManyUsersCommand { UserIds = [_dataMock.ActiveIds.First(), -1] });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }
}
