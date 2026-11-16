using ArturRios.Common.Test.Attributes;
using ArturRios.Common.Util.Hashing;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Enums;
using ArturRios.UserManagement.Domain.Repositories;
using ArturRios.UserManagement.Test.Mock;
using Moq;

namespace ArturRios.UserManagement.Command.Tests;

[Collection(PasswordHashingCollection.Name)]
public class CreateUserCommandTests
{
    private const string Password = "Valid1Password";

    private readonly DataMock _dataMock = DataMock.New.Build();

    private CreateUserCommandHandler Handler(IUserRepository? repository = null) => new(
        new CreateUserCommandValidator(),
        repository ?? MockRepositoryFactory.CreateUserRepositoryInstance(_dataMock.AllUsers));

    private static CreateUserCommand Command(string email = "new.user@mail.com") => new()
    {
        Name = "New User",
        Email = email,
        Password = Password,
        RoleId = (int)Roles.Regular
    };

    [UnitFact]
    public void GivenAValidNewUser_WhenCreating_ThenItIsStoredWithAHashOfItsPassword()
    {
        // Given
        User? stored = null;
        var repository = new Mock<IUserRepository>();
        repository.Setup(repo => repo.GetAll()).Returns(_dataMock.AllUsers.AsQueryable());
        repository.Setup(repo => repo.Create(It.IsAny<User>())).Callback<User>(user => stored = user).Returns(42);

        // When
        var result = Handler(repository.Object).Handle(Command());

        // Then
        Assert.True(result.Success);
        Assert.Equal(42, result.Data?.CreatedUserId);
        Assert.Equal(["User created successfully"], result.Messages);
        Assert.NotNull(stored);
        Assert.Equal("New User", stored.Name);
        Assert.Equal("new.user@mail.com", stored.Email);
        Assert.Equal((int)Roles.Regular, stored.RoleId);
        Assert.True(stored.Active);
        Assert.True(Hash.NewFromBytes(stored.Password, stored.Salt).TextMatches(Password));
    }

    [UnitFact]
    public void GivenAnInvalidEmail_WhenCreating_ThenNothingIsStored()
    {
        // Given
        var repository = new Mock<IUserRepository>();

        // When
        var result = Handler(repository.Object).Handle(Command(email: string.Empty));

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Email should be valid"], result.Errors);
        repository.Verify(repo => repo.Create(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnEmailAlreadyRegistered_WhenCreating_ThenItIsRefusedAndNothingIsStored()
    {
        // Given
        var repository = new Mock<IUserRepository>();
        repository.Setup(repo => repo.GetAll()).Returns(_dataMock.AllUsers.AsQueryable());

        // When
        var result = Handler(repository.Object).Handle(Command(email: _dataMock.ActiveEmail));

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["E-mail already registered"], result.Errors);
        repository.Verify(repo => repo.Create(It.IsAny<User>()), Times.Never);
    }

    [UnitFact]
    public void GivenAnEmailOfAnInactiveUser_WhenCreating_ThenItIsStillRefused()
    {
        // Given
        var inactive = _dataMock.AllUsers.First(user => !user.Active);

        // When
        var result = Handler().Handle(Command(email: inactive.Email));

        // Then
        Assert.Equal(["E-mail already registered"], result.Errors);
    }

    [UnitFact]
    public void GivenEveryFieldInvalid_WhenCreating_ThenEachIsReported()
    {
        // Given
        var command = new CreateUserCommand { Name = string.Empty, Email = "not-an-email", Password = "short", RoleId = 0 };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains("Email should be valid", result.Errors);
        Assert.Contains("Password must contain at least 8 characters, a number, a lower char and an upper char", result.Errors);
        Assert.Contains("Role should be valid", result.Errors);
    }
}
