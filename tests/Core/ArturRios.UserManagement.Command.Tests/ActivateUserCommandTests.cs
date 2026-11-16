using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class ActivateUserCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly ActivateUserCommandHandler _handler;

    public ActivateUserCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new ActivateUserCommandHandler(new ActivateUserCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenActivating_ThenItIsActivatedAndStored()
    {
        // Given
        var id = _dataMock.InactiveIds.First();

        // When
        var result = _handler.Handle(new ActivateUserCommand { UserId = id });

        // Then
        Assert.True(result.Success);
        Assert.Equal(["User activated successfully"], result.Messages);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => user.Id == id && user.Active)), Times.Once);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenActivating_ThenTheUserIsNotFound()
    {
        // When
        var result = _handler.Handle(new ActivateUserCommand { UserId = _dataMock.NonexistentIds.First() });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["User not found"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenActivating_ThenItIsRefusedAndNothingIsStored()
    {
        // When
        var result = _handler.Handle(new ActivateUserCommand { UserId = _dataMock.ActiveIds.First() });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["User already active"], result.Errors);
        Assert.Empty(result.Messages);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenAnIdBelowOne_WhenActivating_ThenItIsRefusedBeforeLookingTheUserUp(int id)
    {
        // When
        var result = _handler.Handle(new ActivateUserCommand { UserId = id });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
    }
}
