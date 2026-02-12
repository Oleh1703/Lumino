﻿using Lumino.Api.Application.Services;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Xunit;

namespace Lumino.Tests;

public class AdminUserServiceTests
{
    [Fact]
    public void GetAll_ReturnsUsersOrderedByCreatedAtDesc()
    {
        var dbContext = TestDbContextFactory.Create();

        dbContext.Users.AddRange(
            new User
            {
                Id = 1,
                Email = "old@mail.com",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Email = "new@mail.com",
                PasswordHash = "hash",
                Role = Role.Admin,
                CreatedAt = new DateTime(2026, 2, 12, 10, 0, 0, DateTimeKind.Utc)
            }
        );

        dbContext.SaveChanges();

        var service = new AdminUserService(dbContext);

        var result = service.GetAll();

        Assert.Equal(2, result.Count);

        Assert.Equal(2, result[0].Id);
        Assert.Equal("new@mail.com", result[0].Email);
        Assert.Equal("Admin", result[0].Role);

        Assert.Equal(1, result[1].Id);
        Assert.Equal("old@mail.com", result[1].Email);
        Assert.Equal("User", result[1].Role);
    }
}
