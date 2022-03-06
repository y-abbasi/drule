namespace DRule;

public class Person
{
    
    public string FirstName { get; set; }
    public int CurrentUserId() => 200;
    public int UserId { get; set; }
    public string LastName { get; set; }
    public List<Address> Addresses { get; set; }
    public DateTime BirthDate { get; set; }
    public int Grade { get; set; }
}