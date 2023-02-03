using Microsoft.EntityFrameworkCore;
using TelegramApiBot.Commands;
using TelegramApiBot.Commands.Callback;
using TelegramApiBot.Configuration;
using TelegramApiBot.Data;
using TelegramApiBot.Services;

var builder = WebApplication.CreateBuilder(args);
DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
if (env == Environments.Production)
{
    builder.Services.AddSwaggerGen();
}

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

var app = builder.Build();
if (env == Environments.Production)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var bot = app.Services.GetService<TelegramBot>();
bot?.Start();

app.Run();