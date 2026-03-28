# WorkoutLog

## API

### HTTPS development certificate (PowerShell)

Run these commands on each development machine:

```powershell
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\workoutlog.pfx" -p "WorkoutLogDev!2026"
dotnet dev-certs https --trust
```
