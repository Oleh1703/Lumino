using Lumino.Api.Application.DTOs;
using Lumino.Api.Application.Services;
using Lumino.Api.Application.Validators;
using Lumino.Api.Domain.Entities;
using Lumino.Api.Domain.Enums;
using Lumino.Api.Utils;
using System;
using Xunit;

namespace Lumino.Tests;

public class UserAccountServiceDeleteAccountTests
{
    [Fact]
    public void DeleteAccount_WithWrongPassword_ShouldThrow()
    {
        var dbContext = TestDbContextFactory.Create();
        var hasher = new PasswordHasher();

        var user = new User
        {
            Email = "test@mail.com",
            PasswordHash = hasher.Hash("123456"),
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            HeartsUpdatedAtUtc = DateTime.UtcNow,
            Theme = "light"
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new UserAccountService(
            dbContext,
            new ChangePasswordRequestValidator(),
            new DeleteAccountRequestValidator(),
            hasher);

        Assert.Throws<UnauthorizedAccessException>(() =>
        {
            service.DeleteAccount(user.Id, new DeleteAccountRequest
            {
                Password = "wrong"
            });
        });
    }

    [Fact]
    public void DeleteAccount_WithValidPassword_ShouldRemoveUser()
    {
        var dbContext = TestDbContextFactory.Create();
        var hasher = new PasswordHasher();

        var user = new User
        {
            Email = "test@mail.com",
            PasswordHash = hasher.Hash("123456"),
            Role = Role.User,
            CreatedAt = DateTime.UtcNow,
            HeartsUpdatedAtUtc = DateTime.UtcNow,
            Theme = "light"
        };

        dbContext.Users.Add(user);
        dbContext.SaveChanges();

        var service = new UserAccountService(
            dbContext,
            new ChangePasswordRequestValidator(),
            new DeleteAccountRequestValidator(),
            hasher);

        service.DeleteAccount(user.Id, new DeleteAccountRequest
        {
            Password = "123456"
        });

        var deleted = dbContext.Users.FirstOrDefault(x => x.Id == user.Id);
        Assert.True(deleted == null);
    }
}