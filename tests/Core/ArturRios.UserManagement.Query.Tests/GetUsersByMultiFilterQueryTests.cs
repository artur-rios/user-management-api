using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Domain.Enums;
using ArturRios.UserManagement.Query.Handlers;
using ArturRios.UserManagement.Query.Queries;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.Query.Tests;

public class GetUsersByMultiFilterQueryTests
{
    private const string NothingFound = "No users found for the specified filters";

    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly GetUsersByMultiFilterQueryHandler _handler;

    public GetUsersByMultiFilterQueryTests() =>
        _handler = new GetUsersByMultiFilterQueryHandler(MockRepositoryFactory.CreateUserReadOnlyRepositoryInstance(_dataMock.AllUsers));

    private IEnumerable<int> IdsFor(GetUsersByMultiFilterQuery query) => _handler.Handle(query).Data!.Select(user => user.Id);

    [UnitFact]
    public void GivenSomeIds_WhenQueried_ThenThoseUsersAreReturned()
    {
        // Given
        var query = new GetUsersByMultiFilterQuery { Ids = _dataMock.ActiveIds };

        // When
        var result = _handler.Handle(query);

        // Then
        Assert.True(result.Success);
        Assert.Equal(_dataMock.ActiveIds, result.Data!.Select(user => user.Id));
        Assert.Equal(["Returning page 1 of users with page size 100"], result.Messages);
    }

    [UnitFact]
    public void GivenIdsNotOnTheDatabase_WhenQueried_ThenNoUserIsReturnedAndItSaysSo()
    {
        // When
        var result = _handler.Handle(new GetUsersByMultiFilterQuery { Ids = _dataMock.NonexistentIds });

        // Then
        Assert.True(result.Success);
        Assert.Empty(result.Data!);
        Assert.Equal([NothingFound], result.Messages);
    }

    [UnitFact]
    public void GivenSomeEmails_WhenQueried_ThenTheUsersWithThoseEmailsAreReturned()
    {
        // Given
        var emails = new[] { _dataMock.AllUsers[0].Email, _dataMock.AllUsers[7].Email, DataMock.NonexistentEmail };

        // When
        var ids = IdsFor(new GetUsersByMultiFilterQuery { Emails = emails });

        // Then
        Assert.Equal([_dataMock.AllUsers[0].Id, _dataMock.AllUsers[7].Id], ids);
    }

    [UnitFact]
    public void GivenSomeNames_WhenQueried_ThenTheUsersWithThoseNamesAreReturned()
    {
        // Given
        var names = new[] { _dataMock.AllUsers[1].Name, _dataMock.AllUsers[8].Name };

        // When
        var ids = IdsFor(new GetUsersByMultiFilterQuery { Names = names }).ToList();

        // Then
        Assert.Contains(_dataMock.AllUsers[1].Id, ids);
        Assert.Contains(_dataMock.AllUsers[8].Id, ids);
        Assert.All(ids, id => Assert.Contains(_dataMock.AllUsers.Single(user => user.Id == id).Name, names));
    }

    [UnitTheory]
    [InlineData(new[] { Roles.Regular }, 10)]
    [InlineData(new[] { Roles.Admin, Roles.Test }, 0)]
    [InlineData(new[] { Roles.Admin, Roles.Regular }, 10)]
    public void GivenSomeRoles_WhenQueried_ThenTheUsersWithAnyOfThemAreReturned(Roles[] roles, int expected)
    {
        // When
        var ids = IdsFor(new GetUsersByMultiFilterQuery { RoleIds = roles.Select(role => (int)role) });

        // Then
        Assert.Equal(expected, ids.Count());
    }

    [UnitFact]
    public void GivenIdsAndEmails_WhenQueried_ThenOnlyUsersMatchingBothAreReturned()
    {
        // Given
        var query = new GetUsersByMultiFilterQuery
        {
            Ids = [1, 2, 3],
            Emails = [_dataMock.AllUsers[1].Email, _dataMock.AllUsers[6].Email]
        };

        // When
        var ids = IdsFor(query);

        // Then
        Assert.Equal([2], ids);
    }

    [UnitFact]
    public void GivenEmptyFilterLists_WhenQueried_ThenNothingIsFilteredOn()
    {
        // When
        var ids = IdsFor(new GetUsersByMultiFilterQuery { Ids = [], Names = [], Emails = [], RoleIds = [] });

        // Then
        Assert.Equal(_dataMock.AllUsers.Select(user => user.Id), ids);
    }

    [UnitFact]
    public void GivenAPageSize_WhenQueryingTheSecondPage_ThenTheNextMatchingUsersAreReturned()
    {
        // When
        var result = _handler.Handle(new GetUsersByMultiFilterQuery { Ids = _dataMock.InactiveIds, PageNumber = 2, PageSize = 2 });

        // Then
        Assert.Equal(_dataMock.InactiveIds.Skip(2).Take(2), result.Data!.Select(user => user.Id));
        Assert.Equal(["Returning page 2 of users with page size 2"], result.Messages);
    }
}
