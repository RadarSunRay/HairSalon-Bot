using Bot.Models;
public class UserDTO
{
    public string TelegramUserName {get;set;} = string.Empty;
    public string PhoneNumber {get;set;} = string.Empty;
    public string SelectedService {get;set;} = string.Empty;
    public Barber? SelectedBarber {get;set;}
    public string SelectedTime {get;set;} = string.Empty;
    public DateTime? SelectedDay {get;set;}
    public long Id {get;set;}
}