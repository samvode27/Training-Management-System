public class MyService
{
    public Guid Id { get; } = Guid.NewGuid();
}

builder.Services.AddTransient<MyService>();

var s1 = serviceProvider.GetService<MyService>();
var s2 = serviceProvider.GetService<MyService>();

Console.WriteLine(s1.Id);
Console.WriteLine(s2.Id);