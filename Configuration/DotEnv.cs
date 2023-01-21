using System.Text.RegularExpressions;

namespace TelegramApiBot.Configuration;

public class DotEnv
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        File.ReadAllLines(filePath)
            .Select(l => new Regex(@"^([\w_]+)[\s]?=[\s""]?([^""]*)[""]?$")
                .Matches(l)
                .Select(m => m.Groups.Cast<Group>()
                    .Select(e => e.Value).Skip(1))
                .First())
            .Where(e => e.Count() == 2)
            .ToList()
            .ForEach(e => Environment.SetEnvironmentVariable(e.First(), e.Last()));
    }
}