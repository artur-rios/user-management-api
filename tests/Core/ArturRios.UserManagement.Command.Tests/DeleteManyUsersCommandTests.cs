using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class DeleteManyUsersCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly DeleteManyUsersCommandHandler _handler;

    public DeleteManyUsersCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new DeleteManyUsersCommandHandler(new DeleteManyUsersCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenInactiveUsers_WhenDeletingThem_ThenEachIsDeleted()
    {
        // When
        var result = _handler.Handle(new DeleteManyUsersCommand { UserIds = _dataMock.InactiveIds });

        // Then
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Data);
        Assert.Equal(_dataMock.InactiveIds, result.Data.DeletedIds);
        Assert.Empty(result.Data.FailedDeletionIds);
        Assert.Empty(result.Data.NotFoundIds);
        _repository.Verify(repo => repo.Delete(It.IsAny<User>()), Times.Exactly(_dataMock.InactiveIds.Count));
    }

    [UnitFact]
    public void GivenActiveUsers_WhenDeletingThem_ThenEachFailureIsReportedAndNoneIsDeleted()
    {
        // Given
        var ids = _dataMock.ActiveIds;

        // When
        var result = _handler.Handle(new DeleteManyUsersCommand { UserIds = ids });

        // Then
        Assert.False(result.Success);
        Assert.Equal(
            [$"Users with IDs {string.Join(", ", ids)} cannot be deleted", .. ids.Select(id => $"User with Id {id}: Can't delete active user")],
            result.Errors);
        Assert.Equal(ids, result.Data?.FailedDeletionIds);
        _repository.Verify(repo => repo.Delete(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenIdsNotOnTheDatabase_WhenDeletingThem_ThenEachIsReportedAsNotFound()
    {
        // Given
        var ids = _dataMock.NonexistentIds;

        // When
        var result = _handler.Handle(new DeleteManyUsersCommand { UserIds = ids });

        // Then
        Assert.False(result.Success);
        Assert.Equal(
            [$"Users with IDs {string.Join(", ", ids)} not found", .. ids.Select(id => $"User with Id {id} not found")],
            result.Errors);
        Assert.Equal(ids, result.Data?.NotFoundIds);
    }

    [UnitFact]
    public void GivenSomeUsersThatCanBeDeleted_WhenDeletingThem_ThenTheyAreDeletedAndTheOthersFailTheCommand()
    {
        // Given
        var inactive = _dataMock.InactiveIds.First();
        var active = _dataMock.ActiveIds.First();
        var missing = _dataMock.NonexistentIds.First();

        // When
        var result = _handler.Handle(new DeleteManyUsersCommand { UserIds = [inactive, active, missing] });

        // Then
        Assert.False(result.Success);
        Assert.Equal(
            [
                $"Users with IDs {missing} not found; Users with IDs {active} cannot be deleted",
                $"User with Id {active}: Can't delete active user",
                $"User with Id {missing} not found"
            ],
            result.Errors);
        Assert.NotNull(result.Data);
        Assert.Equal([inactive], result.Data.DeletedIds);
        _repository.Verify(repo => repo.Delete(It.Is<User>(user => user.Id == inactive)), Times.Once);
    }

    [UnitFact]
    public void GivenNoIds_WhenDeleting_ThenItIsRefusedAsInvalid()
    {
        // When
        var result = _handler.Handle(new DeleteManyUsersCommand { UserIds = [] });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Null(result.Data);
    }

    [UnitFact]
    public void GivenAnIdBelowOne_WhenDeleting_ThenItIsRefusedAsInvalid()
    {
        // When
        var result = _handler.Handle(new DeleteManyUsersCommand { UserIds = [_dataMock.InactiveIds.First(), 0] });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.Delete(It.IsAny<User>()), Times.Never);
    }
}
