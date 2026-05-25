System.Console.WriteLine("//////// This variable may be null ////////");

string? region = null;

Console.WriteLine(region?.ToUpper());

System.Console.WriteLine("//////// Null Coalescing(If null, use fallback) ////////");

string display = region ?? "Unassigned";

Console.WriteLine(display);

System.Console.WriteLine("//////// Null Coalescing Assignment(Assign only if currently null.) ////////");

region ??= "Addis Ababa";

Console.WriteLine(region);