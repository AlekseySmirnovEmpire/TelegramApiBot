using BotanIO.Api;

namespace TelegramApiBot.Services;

public class BotanService
{
    private readonly Botan? _botan;
    private readonly ILogger<BotanService> _logger;

    public BotanService(ILogger<BotanService> logger)
    {
        _logger = logger;
        try
        {
            // _botan = new Botan(Environment.GetEnvironmentVariable("ASPNETCORE_BotanApiKey"));
            _botan = null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Cannot connect to Botan.io! Error: \"{ex.Message}\"");
        }
    }

    public async Task Track(string command, long chatId, string? message = null)
    {
        try
        {
            if (_botan == null)
            {
                return;
            }
            
            if (!string.IsNullOrEmpty(message))
            {
                await _botan.TrackAsync(
                    command,
                    new
                    {
                        dateTime = DateTime.Now.ToString("g"),
                        message
                    },
                    chatId.ToString());
                return;
            }

            await _botan.TrackAsync(command, DateTime.Now, chatId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Cannot track message from \"{command}\" command in chat \"{chatId}\" to Botan.io! Error: \"{ex.Message}\"");
        }
    }
}