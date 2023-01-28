namespace TelegramApiBot.Data.Entities;

public class BlackList
{
    public Guid UserId { get; set; }
    
    public User User { get; set; }
    
    public long BlockedUserKey { get; set; }
    
    public DateTime CreatedAt { get; set; }
}