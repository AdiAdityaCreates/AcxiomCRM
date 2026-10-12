# Database setup
Run these commands from the AcxiomCRM project folder after installing .NET 8 SDK:

```bash
dotnet restore
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

If `dotnet-ef` is already installed, skip the install command. For SQL Server Express or another server, edit `appsettings.json` and use a connection string appropriate to your environment. Do not commit real passwords or secrets.
