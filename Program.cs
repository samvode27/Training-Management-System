// System.Console.WriteLine("//////// This variable may be null ////////");

// string? region = null;

// Console.WriteLine(region?.ToUpper());

// System.Console.WriteLine("//////// Null Coalescing(If null, use fallback) ////////");

// string display = region ?? "Unassigned";

// Console.WriteLine(display);

// System.Console.WriteLine("//////// Null Coalescing Assignment(Assign only if currently null.) ////////");

// region ??= "Addis Ababa";

// Console.WriteLine(region);





System.Console.WriteLine("//////////// double stores values in binary approximation. ////////////");
double grant = 1999.99;
double total = grant * 100000;

Console.WriteLine(total);

System.Console.WriteLine("//////////// Correct: Use decimal ////////////");
decimal money = 1999.99m;
decimal final = money * 100000m;

Console.WriteLine(final);