using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class DeactivateUserCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new DeactivateUserCommandHandler(new DeactivateUserCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenDeactivating_ThenItIsDeactivatedAndStored()
    {
        // Given
        var id = _dataMock.ActiveIds.First();

        // When
        var result = _handler.Handle(new DeactivateUserCommand { UserId = id });

        // Then
        Assert.True(result.Success);
        Assert.Equal(["User deactivated successfully"], result.Messages);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => user.Id == id && !user.Active)), Times.Once);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenDeactivating_ThenTheUserIsNotFound()
    {
        // When
        var result = _handler.Handle(new DeactivateUserCommand { UserId = _dataMock.NonexistentIds.First() });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["User not found"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenDeactivating_ThenItIsRefusedAndNothingIsStored()
    {
        // When
        var result = _handler.Handle(new DeactivateUserCommand { UserId = _dataMock.InactiveIds.First() });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["User already inactive"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenAnIdBelowOne_WhenDeactivating_ThenItIsRefusedBeforeLookingTheUserUp(int id)
    {
        // When
        var result = _handler.Handle(new DeactivateUserCommand { UserId = id });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
    }
}
