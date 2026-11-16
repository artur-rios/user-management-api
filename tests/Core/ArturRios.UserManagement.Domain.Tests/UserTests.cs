using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Domain.Enums;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.Domain.Tests;

public class UserTests
{
    private static User Active(Roles role = Roles.Regular) => UserMock.New.Active().WithRole(role).WithEmail("user@mail.com").Generate();

    private static User Inactive(Roles role = Roles.Regular) => UserMock.New.Inactive().WithRole(role).WithEmail("user@mail.com").Generate();

    [UnitFact]
    public void GivenAnEmailANameAndARole_WhenCreated_ThenTheUserIsActiveAndCreatedNow()
    {
        // Given
        var before = DateTime.UtcNow;

        // When
        var user = new User("user@mail.com", "Name", (int)Roles.Regular);

        // Then
        Assert.Equal("user@mail.com", user.Email);
        Assert.Equal("Name", user.Name);
        Assert.Equal((int)Roles.Regular, user.RoleId);
        Assert.True(user.Active);
        Assert.InRange(user.CreatedAt, before, DateTime.UtcNow);
        Assert.Empty(user.Password);
        Assert.Empty(user.Salt);
    }

    [UnitFact]
    public void GivenEveryField_WhenCreated_ThenTheUserHoldsThem()
    {
        // Given
        var createdAt = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        // When
        var user = new User(7, "user@mail.com", "Name", (int)Roles.Admin, createdAt, active: false);

        // Then
        Assert.Equal(7, user.Id);
        Assert.Equal(createdAt, user.CreatedAt);
        Assert.False(user.Active);
    }

    [UnitFact]
    public void GivenACreationDateAndNoActiveFlag_WhenCreated_ThenTheUserIsActive()
    {
        // When
        var user = new User("user@mail.com", "Name", (int)Roles.Regular, new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        // Then
        Assert.True(user.Active);
    }

    [UnitFact]
    public void GivenAHashAndASalt_WhenSettingThePassword_ThenTheUserHoldsBoth()
    {
        // Given
        var user = Active();

        // When
        user.SetPassword([1, 2, 3], [4, 5]);

        // Then
        Assert.Equal([1, 2, 3], user.Password);
        Assert.Equal([4, 5], user.Salt);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenActivating_ThenItIsActive()
    {
        // Given
        var user = Inactive();

        // When
        var result = user.Activate();

        // Then
        Assert.True(result.Success);
        Assert.True(user.Active);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenActivating_ThenItIsRefused()
    {
        // Given
        var user = Active();

        // When
        var result = user.Activate();

        // Then
        Assert.Equal(["User already active"], result.Errors);
        Assert.True(user.Active);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenDeactivating_ThenItIsInactive()
    {
        // Given
        var user = Active();

        // When
        var result = user.Deactivate();

        // Then
        Assert.True(result.Success);
        Assert.False(user.Active);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenDeactivating_ThenItIsRefused()
    {
        // Given
        var user = Inactive();

        // When
        var result = user.Deactivate();

        // Then
        Assert.Equal(["User already inactive"], result.Errors);
        Assert.False(user.Active);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenAskedIfItCanBeDeleted_ThenItCan()
    {
        // When
        var result = Inactive().CanDelete();

        // Then
        Assert.True(result.Success);
        Assert.Empty(result.Errors);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenAskedIfItCanBeDeleted_ThenItCannot()
    {
        // Given
        var user = Active();

        // When
        var result = user.CanDelete();

        // Then
        Assert.Equal(["Can't delete active user"], result.Errors);
        Assert.True(user.Active);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenRenamed_ThenItHasTheNewName()
    {
        // Given
        var user = Active();

        // When
        var result = user.Update("New Name");

        // Then
        Assert.True(result.Success);
        Assert.Equal("New Name", user.Name);
    }

    [UnitTheory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenAnActiveUser_WhenRenamedToBlank_ThenItIsRefusedAndKeepsItsName(string name)
    {
        // Given
        var user = Active();
        var previous = user.Name;

        // When
        var result = user.Update(name);

        // Then
        Assert.Equal(["Name cannot be empty"], result.Errors);
        Assert.Equal(previous, user.Name);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenRenamed_ThenItIsRefusedAndKeepsItsName()
    {
        // Given
        var user = Inactive();
        var previous = user.Name;

        // When
        var result = user.Update("New Name");

        // Then
        Assert.Equal(["Can't update inactive user"], result.Errors);
        Assert.Equal(previous, user.Name);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenRenamedToBlank_ThenBothProblemsAreReported()
    {
        // When
        var result = Inactive().Update(string.Empty);

        // Then
        Assert.Equivalent(new[] { "Can't update inactive user", "Name cannot be empty" }, result.Errors, strict: true);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenItsEmailChanges_ThenItHasTheNewEmail()
    {
        // Given
        var user = Active();

        // When
        var result = user.UpdateEmail("new@mail.com");

        // Then
        Assert.True(result.Success);
        Assert.Equal("new@mail.com", user.Email);
    }

    [UnitTheory]
    [InlineData("", "Email cannot be empty")]
    [InlineData("  ", "Email cannot be empty")]
    [InlineData("user@mail.com", "New email must be different from current email")]
    public void GivenAnActiveUser_WhenItsEmailChangesToAnUnusableOne_ThenItIsRefusedAndKeepsItsEmail(string email, string error)
    {
        // Given
        var user = Active();

        // When
        var result = user.UpdateEmail(email);

        // Then
        Assert.Equal([error], result.Errors);
        Assert.Equal("user@mail.com", user.Email);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenItsEmailChanges_ThenItIsRefusedAndKeepsItsEmail()
    {
        // Given
        var user = Inactive();

        // When
        var result = user.UpdateEmail("new@mail.com");

        // Then
        Assert.Equal(["Can't change email of inactive user"], result.Errors);
        Assert.Equal("user@mail.com", user.Email);
    }

    [UnitTheory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Test)]
    public void GivenAnActiveUser_WhenItsRoleChangesToAnotherRole_ThenItHasTheNewRole(Roles role)
    {
        // Given
        var user = Active(Roles.Regular);

        // When
        var result = user.UpdateRole((int)role);

        // Then
        Assert.True(result.Success);
        Assert.Equal((int)role, user.RoleId);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(999)]
    public void GivenAnActiveUser_WhenItsRoleChangesToOneThatDoesNotExist_ThenItIsRefusedAndKeepsItsRole(int roleId)
    {
        // Given
        var user = Active(Roles.Regular);

        // When
        var result = user.UpdateRole(roleId);

        // Then
        Assert.Equal(["Role should be valid"], result.Errors);
        Assert.Equal((int)Roles.Regular, user.RoleId);
    }

    [UnitFact]
    public void GivenAnActiveUser_WhenItsRoleChangesToTheSameRole_ThenItIsRefused()
    {
        // Given
        var user = Active(Roles.Regular);

        // When
        var result = user.UpdateRole((int)Roles.Regular);

        // Then
        Assert.Equal(["New role must be different from current role"], result.Errors);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenItsRoleChanges_ThenItIsRefusedAndKeepsItsRole()
    {
        // Given
        var user = Inactive(Roles.Regular);

        // When
        var result = user.UpdateRole((int)Roles.Admin);

        // Then
        Assert.Equal(["Can't change role of inactive user"], result.Errors);
        Assert.Equal((int)Roles.Regular, user.RoleId);
    }

    [UnitFact]
    public void GivenAnInactiveUser_WhenItsRoleChangesToOneThatDoesNotExist_ThenBothProblemsAreReported()
    {
        // When
        var result = Inactive().UpdateRole(999);

        // Then
        Assert.Equivalent(new[] { "Role should be valid", "Can't change role of inactive user" }, result.Errors, strict: true);
    }
}
