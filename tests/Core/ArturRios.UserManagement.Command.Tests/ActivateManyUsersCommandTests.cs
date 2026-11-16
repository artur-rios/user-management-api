using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class ActivateManyUsersCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly ActivateManyUsersCommandHandler _handler;

    public ActivateManyUsersCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new ActivateManyUsersCommandHandler(new ActivateManyUsersCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenInactiveUsers_WhenActivatingThem_ThenEachIsActivatedAndStored()
    {
        // When
        var result = _handler.Handle(new ActivateManyUsersCommand { UserIds = _dataMock.InactiveIds });

        // Then
        Assert.True(result.Success);
        Assert.Equal([$"{_dataMock.InactiveIds.Count} user(s) activated successfully"], result.Messages);
        Assert.NotNull(result.Data);
        Assert.Equal(_dataMock.InactiveIds, result.Data.ActivatedIds);
        Assert.Empty(result.Data.FailedActivationIds);
        Assert.Empty(result.Data.NotFoundIds);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => user.Active)), Times.Exactly(_dataMock.InactiveIds.Count));
    }

    [UnitFact]
    public void GivenUsersAlreadyActive_WhenActivatingThem_ThenEachFailureIsReported()
    {
        // Given
        var ids = _dataMock.ActiveIds;

        // When
        var result = _handler.Handle(new ActivateManyUsersCommand { UserIds = ids });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(
            [$"Users with IDs {string.Join(", ", ids)} cannot be activated", .. ids.Select(id => $"User with Id {id}: User already active")],
            result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenIdsNotOnTheDatabase_WhenActivatingThem_ThenTheyAreReportedAsNotFound()
    {
        // When
        var result = _handler.Handle(new ActivateManyUsersCommand { UserIds = _dataMock.NonexistentIds });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal([$"Users with IDs {string.Join(", ", _dataMock.NonexistentIds)} not found"], result.Errors);
    }

    [UnitFact]
    public void GivenSomeUsersThatCanBeActivated_WhenActivatingThem_ThenItSucceedsAndReportsTheOthers()
    {
        // Given
        var inactive = _dataMock.InactiveIds.First();
        var active = _dataMock.ActiveIds.First();
        var missing = _dataMock.NonexistentIds.First();

        // When
        var result = _handler.Handle(new ActivateManyUsersCommand { UserIds = [inactive, active, missing] });

        // Then
        Assert.True(result.Success);
        Assert.Equal(
            [
                "1 activations were successful",
                $"Users with IDs {missing} not found",
                $"Users with IDs {active} cannot be activated",
                $"User with Id {active}: User already active"
            ],
            result.Messages);
        Assert.NotNull(result.Data);
        Assert.Equal([inactive], result.Data.ActivatedIds);
        Assert.Equal([active], result.Data.FailedActivationIds);
        Assert.Equal([missing], result.Data.NotFoundIds);
    }

    [UnitFact]
    public void GivenNoIds_WhenActivating_ThenItIsRefusedAsInvalid()
    {
        // When
        var result = _handler.Handle(new ActivateManyUsersCommand { UserIds = [] });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnIdBelowOne_WhenActivating_ThenItIsRefusedAsInvalid()
    {
        // When
        var result = _handler.Handle(new ActivateManyUsersCommand { UserIds = [_dataMock.InactiveIds.First(), 0] });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }
}
