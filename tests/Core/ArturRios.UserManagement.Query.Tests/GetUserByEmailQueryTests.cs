using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Query.Handlers;
using ArturRios.UserManagement.Query.Queries;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.Query.Tests;

public class GetUserByEmailQueryTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly GetUserByEmailQueryHandler _handler;

    public GetUserByEmailQueryTests() =>
        _handler = new GetUserByEmailQueryHandler(MockRepositoryFactory.CreateUserReadOnlyRepositoryInstance(_dataMock.AllUsers));

    [UnitFact]
    public void GivenTheEmailOfAUser_WhenQueried_ThenEveryFieldButThePasswordIsReturned()
    {
        // Given
        var user = _dataMock.ActiveUsers[1];

        // When
        var result = _handler.Handle(new GetUserByEmailQuery { Email = user.Email });

        // Then
        Assert.True(result.Success);
        Assert.Equal([$"User with email '{user.Email}' found"], result.Messages);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(user.Name, result.Data.Name);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.Equal(user.RoleId, result.Data.RoleId);
        Assert.Equal(user.CreatedAt, result.Data.CreatedAt);
        Assert.True(result.Data.Active);
    }

    [UnitFact]
    public void GivenTheEmailOfAnInactiveUser_WhenQueried_ThenItIsReturnedAsInactive()
    {
        // Given
        var user = _dataMock.AllUsers.First(candidate => !candidate.Active);

        // When
        var result = _handler.Handle(new GetUserByEmailQuery { Email = user.Email });

        // Then
        Assert.Equal(user.Id, result.Data?.Id);
        Assert.False(result.Data?.Active);
    }

    [UnitFact]
    public void GivenAnEmailNotOnTheDatabase_WhenQueried_ThenNoUserIsReturnedAndItSaysSo()
    {
        // When
        var result = _handler.Handle(new GetUserByEmailQuery { Email = DataMock.NonexistentEmail });

        // Then
        Assert.True(result.Success);
        Assert.Null(result.Data);
        Assert.Equal([$"User with email '{DataMock.NonexistentEmail}' not found"], result.Messages);
    }
}
