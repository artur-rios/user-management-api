using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.CommandHandlers;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.Command.Tests;

/// <summary>The users are built once for the class: the one who signs in has a real password hash, which is costly.</summary>
public sealed class AuthenticationData
{
    public DataMock Data { get; } = DataMock.New.WithHashedActivePassword().Build();
}

[Collection(PasswordHashingCollection.Name)]
public class AuthenticateUserCommandTests(AuthenticationData authentication) : IClassFixture<AuthenticationData>
{
    private readonly DataMock _dataMock = authentication.Data;

    private AuthenticateUserCommandHandler Handler() => new(
        new AuthenticateUserCommandValidator(),
        MockRepositoryFactory.CreateUserReadOnlyRepositoryInstance(_dataMock.AllUsers));

    [UnitFact]
    public void GivenTheRightEmailAndPassword_WhenAuthenticating_ThenTheUserAndItsRoleAreReturned()
    {
        // Given
        var command = new AuthenticateUserCommand { Email = _dataMock.ActiveEmail, Password = _dataMock.ActivePassword };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(_dataMock.ActiveUserId, result.Data.Id);
        Assert.Equal(_dataMock.ActiveRoleId, result.Data.RoleId);
        Assert.Equal(["User authenticated successfully"], result.Messages);
    }

    [UnitFact]
    public void GivenAnEmptyEmail_WhenAuthenticating_ThenItIsRefusedAsInvalid()
    {
        // Given
        var command = new AuthenticateUserCommand { Email = string.Empty, Password = _dataMock.ActivePassword };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Email should be valid"], result.Errors);
    }

    [UnitFact]
    public void GivenAnEmptyPassword_WhenAuthenticating_ThenItIsRefusedAsInvalid()
    {
        // Given
        var command = new AuthenticateUserCommand { Email = _dataMock.ActiveEmail, Password = string.Empty };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Password should be valid"], result.Errors);
    }

    [UnitFact]
    public void GivenAnEmptyEmailAndPassword_WhenAuthenticating_ThenBothAreReported()
    {
        // Given
        var command = new AuthenticateUserCommand { Email = string.Empty, Password = string.Empty };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Email should be valid", "Password should be valid"], result.Errors);
    }

    [UnitFact]
    public void GivenAnUnknownEmail_WhenAuthenticating_ThenTheCredentialsAreInvalid()
    {
        // Given
        var command = new AuthenticateUserCommand { Email = DataMock.NonexistentEmail, Password = _dataMock.ActivePassword };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Invalid credentials"], result.Errors);
    }

    [UnitFact]
    public void GivenAWrongPassword_WhenAuthenticating_ThenTheCredentialsAreInvalid()
    {
        // Given
        var command = new AuthenticateUserCommand { Email = _dataMock.ActiveEmail, Password = DataMock.NonexistentPassword };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal(["Invalid credentials"], result.Errors);
    }

    [UnitFact]
    public void GivenAnUnknownEmailAndAWrongPassword_WhenAuthenticating_ThenTheCredentialsAreInvalid()
    {
        // Given
        var command = new AuthenticateUserCommand
        {
            Email = DataMock.NonexistentEmail,
            Password = DataMock.NonexistentPassword
        };

        // When
        var result = Handler().Handle(command);

        // Then
        Assert.False(result.Success);
        Assert.Equal(["Invalid credentials"], result.Errors);
    }
}
