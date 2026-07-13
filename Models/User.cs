using System.ComponentModel.DataAnnotations.Schema;

namespace Bot.Models;
public class User
{
    public long Id {get;set;}
    public string TelegramUserName {get;set;} = string.Empty;
    public string PhoneNumber {get;set;} = string.Empty;
    public string SelectedService {get;set;} = string.Empty;
    public int? SelectedBarberId {get;set;}
    [ForeignKey("SelectedBarberId")]
    public Barber? SelectedBarber {get;set;}
    public string currentState {get;set;} = "None";
    public string SelectedTime {get;set;} = string.Empty;
    public DateTime? SelectedDay {get;set;}
}