dotnet tool install --global dotnet-ef --version 3.1.3
dotnet tool update --global dotnet-ef --version 3.1.3
dotnet build
dotnet ef --startup-project ../OranAuth.WebApp/ database update
pause