using Quartz;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Types;
using TelegramApiBot.Services;

namespace TelegramApiBot.Scheduler;

public class SubscribeJob : BaseJob
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SubscribeJob> _logger;

    public SubscribeJob(IServiceScopeFactory serviceScopeFactory, ILogger<SubscribeJob> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override Task Execute()
    {
        _logger.LogInformation("Starting to update subscribes for users.");
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetService<TelegramBot>();
            var userService = scope.ServiceProvider.GetService<UserService>();

            var users = client.GetAllUsersInSession();
            users.ForEach(user =>
            {
                if (!user.SubscribeEndedAt.HasValue)
                {
                    return;
                }

                if (DateTime.Now.Subtract(user.SubscribeEndedAt.Value).TotalDays.CompareTo(30) <= 0)
                {
                    return;
                }

                user.SubscribeType = SubscribeTypeEnum.None;
                user.SubscribeEndedAt = null;
                userService.UpdateUser(user);
                client.UpdateUserInSession(user);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Updating subscribes ended with error: {ex.Message}");
        }
        
        _logger.LogInformation("Updating users subscribe ended successfully!");
        
        return Task.CompletedTask;
    }

    public override TriggerBuilder ScheduleJobTrigger => TriggerBuilder.Create().StartNow()
        .WithDailyTimeIntervalSchedule(s => s
            .WithIntervalInHours(24)
            .OnEveryDay()
            .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(1, 0))).StartNow();

    public override string ToString() => nameof(SubscribeJob);
}