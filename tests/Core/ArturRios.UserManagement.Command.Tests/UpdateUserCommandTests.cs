using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class UpdateUserCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new UpdateUserCommandHandler(new UpdateUserCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenRenaming_ThenItIsStoredAndReturnedWithTheNewName()
    {
        // Given
        var user = _dataMock.ActiveUsers.First();

        // When
        var result = _handler.Handle(new UpdateUserCommand { Id = user.Id, Name = "New Name" });

        // Then
        Assert.True(result.Success);
        Assert.Equal(["User updated with success"], result.Messages);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal("New Name", result.Data.Name);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.Equal(user.RoleId, result.Data.RoleId);
        Assert.Equal(user.CreatedAt, result.Data.CreatedAt);
        Assert.True(result.Data.Active);
        _repository.Verify(repo => repo.Update(It.Is<User>(stored => stored.Name == "New Name")), Times.Once);
    }

    [UnitTheory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenABlankName_WhenRenaming_ThenItIsRefusedAsInvalid(string name)
    {
        // When
        var result = _handler.Handle(new UpdateUserCommand { Id = _dataMock.ActiveIds.First(), Name = name });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Name should be valid"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenRenaming_ThenItIsRefusedAndNothingIsStored()
    {
        // When
        var result = _handler.Handle(new UpdateUserCommand { Id = _dataMock.InactiveIds.First(), Name = "New Name" });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Can't update inactive user"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenRenaming_ThenTheUserIsNotFound()
    {
        // When
        var result = _handler.Handle(new UpdateUserCommand { Id = _dataMock.NonexistentIds.First(), Name = "New Name" });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["User not found"], result.Errors);
    }
}
