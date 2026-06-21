// dependency best practice
// Instead of creating objects manually, .NET gives them to you.

public interface IEmailService
{
    void Send(string message);
}

public class EmailService : IEmailService
{
    public void Send(string message)
    {
        Console.WriteLine("Email sent: " + message);
    }
}

public class UserService
{
    private readonly IEmailService _email;

    public UserService(IEmailService email)
    {
        _email = email;
    }

    public void Register()
    {
        _email.Send("Welcome!");
    }
}