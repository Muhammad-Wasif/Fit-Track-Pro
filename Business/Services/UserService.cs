using FitTrack.Database;
using FitTrack.Models;

namespace FitTrack.Services;

// ================================================================
//  STREAK SERVICE
// ================================================================
public class StreakService
{
    private readonly StreakRepository _repo = new();

    public LoginStreak GetOrCreate(int personId)
    {
        return _repo.GetByPersonId(personId)
               ?? new LoginStreak { PersonId = personId, CurrentStreak = 0, LongestStreak = 0 };
    }
}

// ================================================================
//  UPDATED USER SERVICE
//  Adds: trainer lock, remove-trainee-from-trainer
// ================================================================
public class UserService
{
    private readonly PersonRepository _personRepo = new();
    private readonly GoalRepository   _goalRepo   = new();

    public Person? GetById(int personId) => _personRepo.GetById(personId);

    public List<Person> GetAllTrainees() => _personRepo.GetAllByRole("Trainee");
    public List<Person> GetAllTrainers() => _personRepo.GetAllByRole("Trainer");
    public List<Person> GetAll()         => _personRepo.GetAll();

    public List<Person> GetTraineesByTrainer(int trainerId)
        => _personRepo.GetTraineesByTrainer(trainerId);

    public (bool success, string message) UpdateProfile(
        int personId, string fullName, string email,
        string gender, int age, double heightCm, double weightKg,
        double? bodyFatPct, int? goalId)
    {
        Person? person = _personRepo.GetById(personId);
        if (person == null) return (false, "User not found.");

        if (goalId.HasValue && !_goalRepo.Exists(goalId.Value))
            return (false, "Selected goal does not exist.");

        person.FullName   = fullName.Trim();
        person.Email      = email.Trim().ToLower();
        person.Gender     = gender;
        person.Age        = age;
        person.HeightCm   = heightCm;
        person.WeightKg   = weightKg;
        person.BodyFatPct = bodyFatPct;
        person.GoalId     = goalId;

        _personRepo.Update(person);
        return (true, "Profile updated.");
    }

    /// <summary>
    /// Trainee selects a trainer. Once set, the assignment is locked
    /// (trainee cannot change trainer themselves — spec §9).
    /// </summary>
    public (bool success, string message) AssignTrainer(int traineeId, int trainerId)
    {
        Person? trainee = _personRepo.GetById(traineeId);
        if (trainee == null) return (false, "Trainee not found.");

        if (trainee.TrainerLocked)
            return (false, "Trainer is already assigned and locked. Contact your trainer to change.");

        Person? trainer = _personRepo.GetById(trainerId);
        if (trainer == null || trainer.Role != "Trainer")
            return (false, "Trainer not found.");

        _personRepo.UpdateTrainer(traineeId, trainerId);
        _personRepo.LockTrainer(traineeId);        // Lock so trainee can't change
        return (true, "Trainer assigned successfully.");
    }

    /// <summary>
    /// Trainer removes a trainee. Resets trainer assignment to NULL
    /// and unlocks so trainee can pick another trainer (spec §9).
    /// </summary>
    public (bool success, string message) RemoveTraineeFromTrainer(int traineeId)
    {
        Person? trainee = _personRepo.GetById(traineeId);
        if (trainee == null) return (false, "Trainee not found.");

        _personRepo.RemoveTrainer(traineeId);
        return (true, "Trainee removed. They may now select a new trainer.");
    }

    public (bool success, string message) SetRole(int personId, string role)
    {
        string[] allowed = { "Trainee", "Trainer", "Admin" };
        if (!allowed.Contains(role)) return (false, "Invalid role.");

        Person? person = _personRepo.GetById(personId);
        if (person == null) return (false, "User not found.");

        _personRepo.UpdateRole(personId, role);
        return (true, $"Role updated to {role}.");
    }

    public (bool success, string message) DeleteUser(int personId)
    {
        Person? person = _personRepo.GetById(personId);
        if (person == null) return (false, "User not found.");

        _personRepo.Delete(personId);
        return (true, "User deleted.");
    }
}
