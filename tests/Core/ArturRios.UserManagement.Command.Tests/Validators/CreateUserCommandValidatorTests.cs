using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using ArturRios.UserManagement.Domain.Enums;

namespace ArturRios.UserManagement.Command.Tests.Validators;

public class CreateUserCommandValidatorTests
{
    private const string PasswordRule =
        "Password must contain at least 8 characters, a number, a lower char and an upper char";

    private static CreateUserCommand Command(
        string name = "New User", string email = "new.user@mail.com", string password = "Valid1Pass", int roleId = (int)Roles.Regular) =>
        new() { Name = name, Email = email, Password = password, RoleId = roleId };

    private static List<string> Errors(CreateUserCommand command) =>
        new CreateUserCommandValidator().Validate(command).Errors.Select(error => error.ErrorMessage).ToList();

    [UnitFact]
    public void GivenAValidCommand_WhenValidated_ThenItPasses()
    {
        // When
        var errors = Errors(Command());

        // Then
        Assert.Empty(errors);
    }

    [UnitTheory]
    [InlineData("Abcdefg1")]
    [InlineData("A1bcdefghijklmnop")]
    [InlineData("1aB!@#$%")]
    public void GivenAPasswordWithEveryRequiredKindOfCharacter_WhenValidated_ThenItPasses(string password)
    {
        // When
        var errors = Errors(Command(password: password));

        // Then
        Assert.Empty(errors);
    }

    [UnitTheory]
    [InlineData("Abcdef1")]
    [InlineData("abcdefg1")]
    [InlineData("ABCDEFG1")]
    [InlineData("Abcdefgh")]
    [InlineData("")]
    public void GivenAPasswordMissingARule_WhenValidated_ThenItIsRefused(string password)
    {
        // When
        var errors = Errors(Command(password: password));

        // Then
        Assert.Equal([PasswordRule], errors);
    }

    [UnitTheory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    public void GivenAnInvalidEmail_WhenValidated_ThenItIsRefused(string email)
    {
        // When
        var errors = Errors(Command(email: email));

        // Then
        Assert.Equal(["Email should be valid"], errors);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void GivenARoleThatDoesNotExist_WhenValidated_ThenItIsRefused(int roleId)
    {
        // When
        var errors = Errors(Command(roleId: roleId));

        // Then
        Assert.Equal(["Role should be valid"], errors);
    }

    [UnitTheory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Regular)]
    [InlineData(Roles.Test)]
    public void GivenEveryExistingRole_WhenValidated_ThenItPasses(Roles role)
    {
        // When
        var errors = Errors(Command(roleId: (int)role));

        // Then
        Assert.Empty(errors);
    }

    [UnitTheory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenABlankName_WhenValidated_ThenItIsRefused(string name)
    {
        // When
        var result = new CreateUserCommandValidator().Validate(Command(name: name));

        // Then
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateUserCommand.Name), error.PropertyName);
    }
}
