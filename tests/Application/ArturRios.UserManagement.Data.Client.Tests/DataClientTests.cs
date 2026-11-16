using ArturRios.Common.Configuration.Enums;
using ArturRios.Common.Test.Attributes;

namespace ArturRios.UserManagement.Data.Client.Tests;

public class DataClientTests
{
    private readonly DataClient _dataClient = new(EnvironmentType.Local);

    [FunctionalFact]
    public void GivenTheFirstSeededUser_WhenReadByItsId_ThenItIsReturned()
    {
        var user = _dataClient.UserRepository.GetById(1);

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
    }
}
