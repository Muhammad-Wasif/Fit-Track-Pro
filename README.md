# FIT-TRACK PRO — OOP Edition

C# .NET 8 | Plain ADO.NET | SQL Server Express | OOP (Abstraction, Inheritance, Polymorphism, Encapsulation, Association)

## Setup (Do Once)

1. Open `FitTrack/Database/schema.sql` in SSMS and run it — creates the database and all tables.
2. Open `FitTrack/Program.cs` and update the connection string if your SQL Server instance name is different.
3. Run the app — seed data loads automatically on first launch.

## Run

```
cd FitTrack
dotnet run
```

## OOP Concepts Demonstrated

| Concept | Where |
|---|---|
| Abstraction | `Person` abstract class with `GetRoleLabel()` abstract method |
| Inheritance | `Trainee`, `Trainer`, `Admin` all inherit from `Person` |
| Polymorphism | `GetRoleLabel()` and `ToString()` overridden in each subclass |
| Encapsulation | Private fields via properties in all Model classes |
| Association | `WorkoutPlan` has a list of `WorkoutPlanExercise`; `WorkoutSession` has a list of `SessionLog` |

## Database — 3NF Normalization

- No repeating groups (1NF)
- No partial dependencies (2NF)
- No transitive dependencies (3NF)
- All foreign keys explicit with proper cascade rules
