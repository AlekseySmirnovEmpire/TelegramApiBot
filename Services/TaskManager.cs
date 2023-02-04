using Quartz;
using TelegramApiBot.Scheduler;

namespace TelegramApiBot.Services;

public class TaskManager
{
    private readonly IScheduler _scheduler;
    private readonly ILogger<TaskManager> _logger;
    private static bool jobsCreate;

    public TaskManager(ILogger<TaskManager> logger, IScheduler scheduler, IEnumerable<BaseJob> jobList)
    {
        _logger = logger;
        _scheduler = scheduler;
        if (jobsCreate)
        {
            return;
        }
        
        jobList.ToList().ForEach(Add);
        jobsCreate = true;
    }

    private void Add(BaseJob job)
    {
        var jobKey = new JobKey(job.ToString());
        try
        {
            if (_scheduler.CheckExists(jobKey).Result)
            {
                _logger.LogInformation($"Current job \"{job}\" is already in map!");
                return;
            }

            var jobDetail = JobBuilder.Create(job.GetType())
                .WithIdentity(jobKey)
                .Build();

            if (job.ScheduleJobTrigger is not null)
            {
                _scheduler.ScheduleJob(jobDetail, job.ScheduleJobTrigger.Build());
                return;
            }

            _scheduler.AddJob(jobDetail, true, true);
        }
        catch (Exception ex)
        {
            _logger.LogError($"TaskManager.Add adding job \"{job}\" ended with error: {ex.Message}");
        }
    }
}