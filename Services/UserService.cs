using Microsoft.EntityFrameworkCore;
using TelegramApiBot.Data;
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
        user.QuestionsToUsers = new List<QuestionsToUsers>();
        user.BlackList = new List<BlackList>();
        user.PairAnkets = new List<PairAnket>();
        return user;
    }

    public void UpdateUser(User user)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        dbContext.Database.ExecuteSqlRaw(
            $@"UPDATE ""Users"" SET ""SubscribeType"" = {Convert.ToInt32(user.SubscribeType)}, ""AgeConfirmed"" = {user.AgeConfirmed} WHERE ""Id"" = '{user.Id}'");
    }

    public User? FindByKey(long userKey)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        return dbContext?.Users
            .Include(u => u.QuestionsToUsers)
                .ThenInclude(qtu => qtu.Question)
            .Include(u => u.SingleAnket)
            .Include(u => u.BlackList)
            .Include(u => u.PairAnkets)
            .FirstOrDefault(u => u.Key == userKey);
    }

    public List<User> FindAllUsers()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        return dbContext.Users
            .Include(u => u.QuestionsToUsers)
                .ThenInclude(qtu => qtu.Question)
            .Include(u => u.SingleAnket)
            .Include(u => u.BlackList)
            .Include(u => u.PairAnkets)
            .ToList();
    }
}