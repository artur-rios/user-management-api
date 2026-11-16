using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

public class UpdateUserEmailCommandTests
{
    private const string NewEmail = "updated@mail.com";

    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly Mock<IUserRepository> _repository;
    private readonly UpdateUserEmailCommandHandler _handler;

    public UpdateUserEmailCommandTests()
    {
        _repository = MockRepositoryFactory.CreateUserRepositoryMock(_dataMock.AllUsers);
        _handler = new UpdateUserEmailCommandHandler(new UpdateUserEmailCommandValidator(), _repository.Object);
    }

    [UnitFact]
    public void GivenAFreeEmail_WhenChangingAnActiveUsersEmail_ThenItIsStored()
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand { UserId = _dataMock.ActiveUserId, Email = NewEmail });

        // Then
        Assert.True(result.Success);
        Assert.Equal(NewEmail, result.Data?.UpdatedEmail);
        Assert.Equal([$"User e-mail updated to {NewEmail}"], result.Messages);
        _repository.Verify(repo => repo.Update(It.Is<User>(user => user.Email == NewEmail)), Times.Once);
    }

    [UnitTheory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void GivenAnInvalidEmail_WhenChangingIt_ThenItIsRefusedAsInvalid(string email)
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand { UserId = _dataMock.ActiveUserId, Email = email });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Email should be valid"], result.Errors);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenChangingTheEmail_ThenTheUserIsNotFound()
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand { UserId = _dataMock.NonexistentIds.First(), Email = NewEmail });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["User not found"], result.Errors);
    }

    [UnitFact]
    public void GivenAnEmailAnotherUserHas_WhenChangingTheEmail_ThenItIsRefusedAsInUse()
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand
        {
            UserId = _dataMock.ActiveUserId,
            Email = _dataMock.ActiveUsers[1].Email
        });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Email is already in use"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenTheUsersOwnEmail_WhenChangingTheEmail_ThenItMustBeDifferent()
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand { UserId = _dataMock.ActiveUserId, Email = _dataMock.ActiveEmail });

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["New email must be different from current email"], result.Errors);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenChangingTheEmail_ThenItIsRefusedAndNothingIsStored()
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand { UserId = _dataMock.InactiveIds.First(), Email = NewEmail });

        // Then
        Assert.False(result.Success);
        Assert.Equal(["Can't change email of inactive user"], result.Errors);
        _repository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenAnIdBelowOne_WhenChangingTheEmail_ThenItIsRefusedAsInvalid(int id)
    {
        // When
        var result = _handler.Handle(new UpdateUserEmailCommand { UserId = id, Email = NewEmail });

        // Then
        Assert.False(result.Success);
        Assert.Single(result.Errors);
        _repository.Verify(repo => repo.GetById(It.IsAny<int>()), Times.Never);
    }
}
