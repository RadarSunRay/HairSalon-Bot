namespace Bot.Models;
public class Books
{
    public int Id {get;set;}
    public long UserId {get;set;}
    public User? User {get;set;}
    public int BarberId {get;set;}
    public Barber? Barber {get;set;}
    public DateTime BookTime {get;set;}
    public DateTime BookDay {get;set;}
}