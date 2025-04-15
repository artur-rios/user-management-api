﻿using Microsoft.EntityFrameworkCore;
using TechCraftsmen.Core.Extensions;
using TechCraftsmen.Management.User.Domain.Aggregates;
using TechCraftsmen.Management.User.Domain.Enums;

namespace TechCraftsmen.Management.User.Data.DataSeed;

public static class RoleSeeder
{
    public static void EnsureRolesExist(DbContext context)
    {
        if (context.Set<Role>().Any())
        {
            return;
        }

        var roles = Enum.GetValues(typeof(Roles))
            .Cast<Roles>()
            .Select(role => new Role
            {
                Id = (int)role,
                Name = role.ToString(),
                Description = role.GetDescription()!
            })
            .ToList();

        context.Set<Role>().AddRange(roles);
        context.SaveChanges();
    }
}
