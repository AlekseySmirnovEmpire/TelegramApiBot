namespace TelegramApiBot.Data.Entities;

public class SingleAnket
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public Guid? UserId { get; set; }

    public User User { get; set; }
    
    public byte[] Data { get; set; }

    public DateTime CreatedAt { get; set; }
}