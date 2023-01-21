using Microsoft.EntityFrameworkCore;
using TelegramApiBot.Commands;
using TelegramApiBot.Configuration;
using TelegramApiBot.Data;
using TelegramApiBot.Services;

var builder = WebApplication.CreateBuilder(args);
DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("ASPNETCORE_ConnectionString__NpgsqlConnection")));

builder.Services.AddSingleton<TelegramBot>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<StartTelegramCommand>();

var app = builder.Build();

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (env != Environments.Production)
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