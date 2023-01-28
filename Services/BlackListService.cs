using TelegramApiBot.Data;
using TelegramApiBot.Data.Entities;

namespace TelegramApiBot.Services;

public class BlackListService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BlackListService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void AddUserToBlackList(User user, User blockedUser)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        
        var blackList = new BlackList
        {
            UserId = user.Id,
            CreatedAt = DateTime.Now,
            BlockedUserKey = blockedUser.Key
        };

        dbContext.BlackLists.Add(blackList);
        dbContext.SaveChanges();
        user.BlackList.Add(blackList);
    }
}