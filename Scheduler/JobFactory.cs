using Quartz;
using Quartz.Spi;
using TelegramApiBot.Services;

namespace TelegramApiBot.Scheduler;

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceCollection serviceCollection)
    {
        _serviceProvider = serviceCollection.BuildServiceProvider();
        using var sc = _serviceProvider.CreateScope();
        _ = sc.ServiceProvider.GetService<TaskManager>();
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
        if (!typeof(BaseJob).IsAssignableFrom(jobType))
        {
            throw new ApplicationException("JobFactory support only jobs of class BaseJob");
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();

            var jobList = scope.ServiceProvider.GetServices(typeof(BaseJob)).OfType<BaseJob>().ToList();
            var job = jobList.FirstOrDefault(j => j.GetType() == jobType);
            if (job is not null)
            {
                return job;
            }

            job = (BaseJob)scope.ServiceProvider.GetService(jobType);

            job.ServiceScope = scope;
            return job;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public void ReturnJob(IJob job)
    {
        var baseJob = (BaseJob)job;
        baseJob.ServiceScope?.Dispose();
    }
}