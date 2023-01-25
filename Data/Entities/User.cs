using TelegramApiBot.Data.Types;

namespace TelegramApiBot.Data.Entities;

public class User
{
    public Guid Id { get; set; }

    public long Key { get; set; }

    public string Name { get; set; }

    public SubscribeTypeEnum SubscribeType { get; set; }

    public string? NickName { get; set; }

    public bool AgeConfirmed { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public List<QuestionsToUsers> QuestionsToUsers { get; set; }
    
    public SingleAnket? SingleAnket { get; set; }
}