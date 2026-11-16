using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Query.Handlers;
using ArturRios.UserManagement.Query.Queries;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.Query.Tests;

public class GetUserByIdQueryTests
{
    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryTests() =>
        _handler = new GetUserByIdQueryHandler(MockRepositoryFactory.CreateUserReadOnlyRepositoryInstance(_dataMock.AllUsers));

    [UnitTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenTheIdOfAUser_WhenQueried_ThenEveryFieldButThePasswordIsReturned(bool active)
    {
        // Given
        var user = _dataMock.AllUsers.First(candidate => candidate.Active == active);

        // When
        var result = _handler.Handle(new GetUserByIdQuery { Id = user.Id });

        // Then
        Assert.True(result.Success);
        Assert.Equal([$"User with Id '{user.Id}' found"], result.Messages);
        Assert.NotNull(result.Data);
        Assert.Equal(user.Id, result.Data.Id);
        Assert.Equal(user.Name, result.Data.Name);
        Assert.Equal(user.Email, result.Data.Email);
        Assert.Equal(user.RoleId, result.Data.RoleId);
        Assert.Equal(user.CreatedAt, result.Data.CreatedAt);
        Assert.Equal(active, result.Data.Active);
    }

    [UnitFact]
    public void GivenAnIdNotOnTheDatabase_WhenQueried_ThenNoUserIsReturnedAndItSaysSo()
    {
        // Given
        var id = _dataMock.NonexistentIds.First();

        // When
        var result = _handler.Handle(new GetUserByIdQuery { Id = id });

        // Then
        Assert.True(result.Success);
        Assert.Null(result.Data);
        Assert.Equal([$"User with Id '{id}' not found"], result.Messages);
    }
}
