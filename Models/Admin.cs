namespace Bot.Models;
public class Admin
{
    public int id {get;set;}
    public string name {get;set;} = null!;
    public string PasswordHash {get;set;} = null!;
}