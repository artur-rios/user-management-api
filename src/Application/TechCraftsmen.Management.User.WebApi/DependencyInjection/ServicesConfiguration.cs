﻿using TechCraftsmen.Core.WebApi.Security.Interfaces;
using TechCraftsmen.Management.User.Services;

namespace TechCraftsmen.Management.User.WebApi.DependencyInjection;

public static class ServicesConfiguration
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<UserService>();
    }
}
