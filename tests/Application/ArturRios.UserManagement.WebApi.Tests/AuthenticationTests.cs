using System.Net;
using ArturRios.Common.Configuration.Enums;
using ArturRios.Common.Output;
using ArturRios.Common.Test;
using ArturRios.Common.Test.Attributes;
using ArturRios.Common.Web.Security.Records;
using ArturRios.UserManagement.Domain.Aggregates;
using ArturRios.UserManagement.Test.Fixture;
using ArturRios.UserManagement.Test.Mock;

namespace ArturRios.UserManagement.WebApi.Tests;

public class AuthenticationTests(DatabaseFixture fixture, EnvironmentType environment = EnvironmentType.Local)
    : WebApiTest<Program>(environment), IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private const string AuthenticationRoute = "/Authentication";

    private User _testUser = new();

    public Task InitializeAsync()
    {
        _testUser = fixture.CreateUsers().First();

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;


    [FunctionalFact]
    public async Task GivenAUsersEmailAndPassword_WhenAuthenticating_ThenAValidTokenIsReturned()
    {
        var credentials = CredentialsMock.New
            .WithEmail(_testUser.Email)
            .WithPassword(fixture.GetPassword(_testUser.Id))
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.OK, output.StatusCode);
        Assert.NotNull(output.Body?.Data);
        CustomAssert.NotNullOrWhiteSpace(output.Body.Data?.Token);
        Assert.True(output.Body.Data?.Valid);
        Assert.True(output.Body.Success);
    }

    [FunctionalFact]
    public async Task GivenAWrongPassword_WhenAuthenticating_ThenAnswers401()
    {
        var credentials = CredentialsMock.New
            .WithEmail(_testUser.Email)
            .WithPassword("wrong-password")
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalFact]
    public async Task GivenAnUnknownEmail_WhenAuthenticating_ThenAnswers401()
    {
        var credentials = CredentialsMock.New
            .WithEmail("wrong@mail.com")
            .WithPassword(fixture.GetPassword(_testUser.Id))
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalFact]
    public async Task GivenAnUnknownEmailAndAWrongPassword_WhenAuthenticating_ThenAnswers401()
    {
        var credentials = CredentialsMock.New
            .WithEmail("wrong@mail.com")
            .WithPassword("wrong-password")
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalTheory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    public async Task GivenAnInvalidEmail_WhenAuthenticating_ThenAnswers401(string email)
    {
        var credentials = CredentialsMock.New
            .WithEmail(email)
            .WithPassword(fixture.GetPassword(_testUser.Id))
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalFact]
    public async Task GivenNoEmail_WhenAuthenticating_ThenAnswers400()
    {
        var credentials = CredentialsMock.New
            .WithEmail(null!)
            .WithPassword(fixture.GetPassword(_testUser.Id))
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.BadRequest, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalTheory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GivenABlankPassword_WhenAuthenticating_ThenAnswers401(string password)
    {
        var credentials = CredentialsMock.New
            .WithEmail(_testUser.Email)
            .WithPassword(password)
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalFact]
    public async Task GivenNoPassword_WhenAuthenticating_ThenAnswers400()
    {
        var credentials = CredentialsMock.New
            .WithEmail(_testUser.Email)
            .WithPassword(null!)
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.BadRequest, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalTheory]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("invalid-email", "")]
    public async Task GivenInvalidCredentials_WhenAuthenticating_ThenAnswers401(string email, string password)
    {
        var credentials = CredentialsMock.New
            .WithEmail(email)
            .WithPassword(password)
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.Unauthorized, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }

    [FunctionalFact]
    public async Task GivenNoCredentials_WhenAuthenticating_ThenAnswers400()
    {
        var credentials = CredentialsMock.New
            .WithEmail(null!)
            .WithPassword(null!)
            .Generate();

        var output = await Gateway.PostAsync<DataOutput<Authentication>>(AuthenticationRoute, credentials);

        Assert.Equal(HttpStatusCode.BadRequest, output.StatusCode);
        Assert.NotNull(output.Body);
        Assert.Null(output.Body.Data);
        Assert.False(output.Body.Success);
    }
}
