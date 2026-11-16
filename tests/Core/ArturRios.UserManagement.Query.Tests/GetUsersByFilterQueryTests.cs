using ArturRios.Common.Test.Attributes;
using ArturRios.UserManagement.Domain.Enums;
using ArturRios.UserManagement.Query.Handlers;
using ArturRios.UserManagement.Query.Queries;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.Query.Tests;

public class GetUsersByFilterQueryTests
{
    private const string NothingFound = "No users found for the specified filters";

    private readonly DataMock _dataMock = DataMock.New.Build();
    private readonly GetUsersByFilterQueryHandler _handler;

    public GetUsersByFilterQueryTests() =>
        _handler = new GetUsersByFilterQueryHandler(MockRepositoryFactory.CreateUserReadOnlyRepositoryInstance(_dataMock.AllUsers));

    private IEnumerable<int> IdsFor(GetUsersByFilterQuery query) => _handler.Handle(query).Data!.Select(user => user.Id);

    [UnitFact]
    public void GivenNoFilter_WhenQueried_ThenEveryUserIsReturnedOnTheFirstPage()
    {
        // Given
        var query = new GetUsersByFilterQuery();

        // When
        var result = _handler.Handle(query);

        // Then
        Assert.True(result.Success);
        Assert.Equal(_dataMock.AllUsers.Select(user => user.Id), result.Data!.Select(user => user.Id));
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(["Returning page 1 of users with page size 100"], result.Messages);
    }

    [UnitTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenAnActiveFilter_WhenQueried_ThenOnlyUsersInThatStateAreReturned(bool active)
    {
        // When
        var ids = IdsFor(new GetUsersByFilterQuery { Active = active });

        // Then
        Assert.Equal(active ? _dataMock.ActiveIds : _dataMock.InactiveIds, ids);
    }

    [UnitFact]
    public void GivenANameFilter_WhenQueried_ThenOnlyUsersWithExactlyThatNameAreReturned()
    {
        // Given
        var user = _dataMock.AllUsers[3];

        // When
        var ids = IdsFor(new GetUsersByFilterQuery { Name = user.Name });

        // Then
        Assert.Contains(user.Id, ids);
        Assert.All(ids, id => Assert.Equal(user.Name, _dataMock.AllUsers.Single(candidate => candidate.Id == id).Name));
    }

    [UnitTheory]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenABlankNameFilter_WhenQueried_ThenTheNameIsNotFilteredOn(string name)
    {
        // When
        var ids = IdsFor(new GetUsersByFilterQuery { Name = name });

        // Then
        Assert.Equal(_dataMock.AllUsers.Count, ids.Count());
    }

    [UnitTheory]
    [InlineData(Roles.Regular, 10)]
    [InlineData(Roles.Admin, 0)]
    public void GivenARoleFilter_WhenQueried_ThenOnlyUsersWithThatRoleAreReturned(Roles role, int expected)
    {
        // When
        var result = _handler.Handle(new GetUsersByFilterQuery { RoleId = (int)role });

        // Then
        Assert.Equal(expected, result.Data!.Count);
    }

    [UnitFact]
    public void GivenACreationDateFilter_WhenQueried_ThenOnlyUsersCreatedAtThatMomentAreReturned()
    {
        // Given
        var user = _dataMock.AllUsers[2];

        // When
        var result = _handler.Handle(new GetUsersByFilterQuery { CreatedAt = user.CreatedAt });

        // Then
        Assert.All(result.Data!, found => Assert.Equal(user.CreatedAt, found.CreatedAt));
        Assert.Contains(user.Id, result.Data!.Select(found => found.Id));
    }

    [UnitFact]
    public void GivenFiltersNoUserMatchesTogether_WhenQueried_ThenNoUserIsReturnedAndItSaysSo()
    {
        // Given
        var inactive = _dataMock.AllUsers.First(user => !user.Active);

        // When
        var result = _handler.Handle(new GetUsersByFilterQuery { Name = inactive.Name, Active = true, CreatedAt = inactive.CreatedAt });

        // Then
        Assert.True(result.Success);
        Assert.Empty(result.Data!);
        Assert.Equal([NothingFound], result.Messages);
    }

    [UnitFact]
    public void GivenAPageSize_WhenQueryingTheSecondPage_ThenTheNextUsersAreReturned()
    {
        // Given
        var query = new GetUsersByFilterQuery { PageNumber = 2, PageSize = 3 };

        // When
        var result = _handler.Handle(query);

        // Then
        Assert.Equal([4, 5, 6], result.Data!.Select(user => user.Id));
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(["Returning page 2 of users with page size 3"], result.Messages);
    }

    [UnitFact]
    public void GivenAPageBeyondTheLast_WhenQueried_ThenNoUserIsReturnedAndItSaysSo()
    {
        // When
        var result = _handler.Handle(new GetUsersByFilterQuery { PageNumber = 5, PageSize = 3 });

        // Then
        Assert.Empty(result.Data!);
        Assert.Equal([NothingFound], result.Messages);
    }

    [UnitTheory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenAPageNumberBelowOne_WhenQueried_ThenTheFirstPageIsReturned(int pageNumber)
    {
        // When
        var result = _handler.Handle(new GetUsersByFilterQuery { PageNumber = pageNumber, PageSize = 2 });

        // Then
        Assert.Equal([1, 2], result.Data!.Select(user => user.Id));
    }
}
