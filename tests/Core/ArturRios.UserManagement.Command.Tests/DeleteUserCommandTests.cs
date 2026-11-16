using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class DeleteUserCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new DeleteUserCommandHandler(new DeleteUserCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenDeleting_ThenItIsDeleted()
    {
        // Given
        var id = _dataMock.InactiveIds.First();

        // When
        var result = _handler.Handle(new DeleteUserCommand { UserId = id });

        // Then
        Assert.True(result.Success);
        Assert.Equal(["User deleted successfully"], result.Messages);
        _repository.Verify(repo => repo.Delete(It.Is<User>(user => user.Id == id)), Times.Once);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenDeleting_ThenItIsRefusedAndKept()
    {
        // When
        var result = _handler.Handle(new DeleteUserCommand { UserId = _dataMock.ActiveIds.First() });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["Can't delete active user"], result.Errors);
        _repository.Verify(repo => repo.Delete(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenDeleting_ThenTheUserIsNotFound()
    {
        // When
        var result = _handler.Handle(new DeleteUserCommand { UserId = _dataMock.NonexistentIds.First() });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["User not found"], result.Errors);
        _repository.Verify(repo => repo.Delete(It.IsAny<User>()), Times.Never);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenAnIdBelowOne_WhenDeleting_ThenItIsRefusedBeforeLookingTheUserUp(int id)
    {
        // When
        var result = _handler.Handle(new DeleteUserCommand { UserId = id });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
    }
}
