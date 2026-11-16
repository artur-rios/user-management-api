using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Command.Commands;
using ArturRios.UserManagement.Command.Validators;
using FluentValidation;

namespace ArturRios.UserManagement.Command.Tests.Validators;

/// <summary>
/// The validators that check only which users a command names. Their messages are FluentValidation's defaults, in the
/// current culture's language, so the tests check which property failed rather than the text.
/// </summary>
public class UserIdValidatorsTests
{
    public static TheoryData<IValidator, object> SingleIdCommandsWith(int id) => new()
    {
        { new ActivateUserCommandValidator(), new ActivateUserCommand { UserId = id } },
        { new DeactivateUserCommandValidator(), new DeactivateUserCommand { UserId = id } },
        { new DeleteUserCommandValidator(), new DeleteUserCommand { UserId = id } }
    };

    public static TheoryData<IValidator, object> ManyIdsCommandsWith(params int[] ids) => new()
    {
        { new ActivateManyUsersCommandValidator(), new ActivateManyUsersCommand { UserIds = ids } },
        { new DeactivateManyUsersCommandValidator(), new DeactivateManyUsersCommand { UserIds = ids } },
        { new DeleteManyUsersCommandValidator(), new DeleteManyUsersCommand { UserIds = ids } }
    };

    public static TheoryData<IValidator, object> PositiveIds => SingleIdCommandsWith(1);

    public static TheoryData<IValidator, object> ZeroIds => SingleIdCommandsWith(0);

    public static TheoryData<IValidator, object> NegativeIds => SingleIdCommandsWith(-5);

    public static TheoryData<IValidator, object> SeveralPositiveIds => ManyIdsCommandsWith(1, 2, 3);

    public static TheoryData<IValidator, object> NoIds => ManyIdsCommandsWith();

    public static TheoryData<IValidator, object> OneIdBelowOne => ManyIdsCommandsWith(1, 0, 3);

    private static IList<FluentValidation.Results.ValidationFailure> Failures(IValidator validator, object command) =>
        validator.Validate(new ValidationContext<object>(command)).Errors;

    [UnitTheory]
    [MemberData(nameof(PositiveIds))]
    public void GivenAPositiveId_WhenValidated_ThenItPasses(IValidator validator, object command)
    {
        // When
        var failures = Failures(validator, command);

        // Then
        Assert.Empty(failures);
    }

    [UnitTheory]
    [MemberData(nameof(ZeroIds))]
    [MemberData(nameof(NegativeIds))]
    public void GivenAnIdBelowOne_WhenValidated_ThenTheIdIsRefused(IValidator validator, object command)
    {
        // When
        var failures = Failures(validator, command);

        // Then
        Assert.Equal(["UserId"], failures.Select(failure => failure.PropertyName));
    }

    [UnitTheory]
    [MemberData(nameof(SeveralPositiveIds))]
    public void GivenSeveralPositiveIds_WhenValidated_ThenTheyPass(IValidator validator, object command)
    {
        // When
        var failures = Failures(validator, command);

        // Then
        Assert.Empty(failures);
    }

    [UnitTheory]
    [MemberData(nameof(NoIds))]
    public void GivenNoIds_WhenValidated_ThenTheListIsRefused(IValidator validator, object command)
    {
        // When
        var failures = Failures(validator, command);

        // Then
        Assert.Equal(["UserIds"], failures.Select(failure => failure.PropertyName));
    }

    [UnitTheory]
    [MemberData(nameof(OneIdBelowOne))]
    public void GivenOneIdBelowOne_WhenValidated_ThenOnlyThatIdIsRefused(IValidator validator, object command)
    {
        // When
        var failures = Failures(validator, command);

        // Then
        Assert.Equal(["UserIds[1]"], failures.Select(failure => failure.PropertyName));
    }
}
