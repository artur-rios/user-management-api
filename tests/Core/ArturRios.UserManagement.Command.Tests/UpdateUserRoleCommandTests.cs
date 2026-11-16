using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Enums;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class UpdateUserRoleCommandTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly UpdateUserRoleCommandHandler _handler;

    public UpdateUserRoleCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new UpdateUserRoleCommandHandler(new UpdateUserRoleCommandValidator(), _repository.Object);
    }

    [UnitTheory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Test)]
    public void GivenAnotherValidRole_WhenChangingAnActiveUsersRole_ThenItIsStored(Roles role)
    {
        // When
        var result = _handler.Handle(new UpdateUserRoleCommand { UserId = _dataMock.ActiveIds.First(), NewRoleId = (int)role });

        // Then
        Assert.True(result.Success);
        Assert.Equal((int)role, result.Data?.UpdatedUserRoleId);
        Assert.Equal([$"User role updated to {(int)role}"], result.Messages);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => user.RoleId == (int)role)), Times.Once);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenChangingTheRole_ThenTheUserIsNotFound()
    {
        // When
        var result = _handler.Handle(new UpdateUserRoleCommand { UserId = _dataMock.NonexistentIds.First(), NewRoleId = (int)Roles.Test });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["User not found"], result.Errors);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenChangingTheRole_ThenItIsRefusedAndNothingIsStored()
    {
        // When
        var result = _handler.Handle(new UpdateUserRoleCommand { UserId = _dataMock.InactiveIds.First(), NewRoleId = (int)Roles.Test });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["Can't change role of inactive user"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(999)]
    public void GivenARoleThatDoesNotExist_WhenChangingTheRole_ThenItIsRefusedAsInvalid(int roleId)
    {
        // When
        var result = _handler.Handle(new UpdateUserRoleCommand { UserId = _dataMock.ActiveIds.First(), NewRoleId = roleId });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["Role should be valid"], result.Errors);
        _repository.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
    }

    [UnitFact]
    public void GivenTheUsersOwnRole_WhenChangingTheRole_ThenItMustBeDifferent()
    {
        // When
        var result = _handler.Handle(new UpdateUserRoleCommand { UserId = _dataMock.ActiveIds.First(), NewRoleId = (int)Roles.Regular });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["New role must be different from current role"], result.Errors);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenAnIdBelowOne_WhenChangingTheRole_ThenItIsRefusedAsInvalid(int id)
    {
        // When
        var result = _handler.Handle(new UpdateUserRoleCommand { UserId = id, NewRoleId = (int)Roles.Test });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
    }
}
