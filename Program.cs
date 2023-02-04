using System.Text;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Targets;
using NLog.Web;
using Quartz;
using Quartz.Impl;
using TelegramApiBot.Commands;
using TelegramApiBot.Commands.Callback;
using TelegramApiBot.Configuration;
using TelegramApiBot.Data;
using TelegramApiBot.Scheduler;
using TelegramApiBot.Services;

var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName("file");
    var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
    var fileName = fileTarget.FileName.Render(logEventInfo);
    Directory.CreateDirectory(Path.GetDirectoryName(fileName)!);
    
    logger.Debug($"Set logging path to \"{fileName}\"");

    logger.Debug($"Environment is {env}");

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    builder.Host.UseNLog();

    if (env == Environments.Development)
    {
        builder.Host.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }

    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(Environment.GetEnvironmentVariable("ASPNETCORE_ConnectionString__NpgsqlConnection")));

    builder.Services.AddSingleton<TelegramBot>();
    builder.Services.AddSingleton<UserService>();
    builder.Services.AddSingleton<QuestionsService>();
    builder.Services.AddSingleton<AnketService>();
    builder.Services.AddSingleton<PairService>();
    builder.Services.AddSingleton<BlackListService>();

    builder.Services.AddSingleton<ITelegramCommand, StartTelegramCommand>();
    builder.Services.AddSingleton<ITelegramCommand, MainMenuTelegramCommand>();

    builder.Services.AddSingleton<ICallbackCommand, AgeConfirmTelegramCommand>();
    builder.Services.AddSingleton<ICallbackCommand, QuestionsCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, AnketCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, FindPairCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, MainMenuCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, RedactCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, PagerCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, RewriterCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, PairCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, BlackListCallbackCommand>();
    builder.Services.AddSingleton<ICallbackCommand, SubPairsCallbackCommand>();

    //JOBS
    builder.Services.AddScoped<BaseJob, SubscribeJob>();

    builder.Services.AddScoped<TaskManager>();

    var factory = new StdSchedulerFactory();
    var scheduler = factory.GetScheduler().Result;
    builder.Services.AddSingleton(typeof(IScheduler), scheduler);

    scheduler.JobFactory = new JobFactory(builder.Services);
    scheduler.Start();

    var app = builder.Build();
    if (env == Environments.Production)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (env == Environments.Production)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    var bot = app.Services.GetService<TelegramBot>();
    bot?.Start();

    // await scheduler.ScheduleJob(
    //     JobBuilder.Create<SubscribeJob>().Build(),
    //     .Build());

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
}
finally
{
    LogManager.Shutdown();
}