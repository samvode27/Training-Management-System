// Step 1: appsettings.json
{
  "Database": {
    "ConnectionString": "Server=.;Database=TrainingDB;"
  }
}

// Step 2: Create Options Class

public class DatabaseOptions
{
    public string ConnectionString { get; set; }
}

// Step 3: Register

builder.Services.Configure<DatabaseOptions>(
    builder.Configuration.GetSection("Database"));

// Step 4: Use
public class DataService
{
    private readonly string _conn;

    public DataService(IOptions<DatabaseOptions> options)
    {
        _conn = options.Value.ConnectionString;
    }
}

