﻿using TelegramApiBot.Data;
using TelegramApiBot.Data.Entities;

namespace TelegramApiBot.Services;

public class UserService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public UserService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public User Create(User user)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext == null)
        {
            return null;
        }

        dbContext.Users.Add(user);
        dbContext.SaveChanges();
        return user;
    }

    public void UpdateUser(User user)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        dbContext.Users.Update(user);
    }

    public User? FindByKey(long userKey)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        return dbContext?.Users.FirstOrDefault(u => u.Key == userKey);
    }

    public List<User> FindAllUsers()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        return dbContext.Users.ToList();
    }
}