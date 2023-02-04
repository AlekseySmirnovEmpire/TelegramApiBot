using Quartz;

namespace TelegramApiBot.Scheduler;

public abstract class BaseJob : IJob
{
    public IServiceScope ServiceScope { get; set; }

    public Task Execute(IJobExecutionContext context) => Execute();
    
    public abstract Task Execute();
    
    public abstract TriggerBuilder ScheduleJobTrigger { get; }

    public new abstract string ToString();
}