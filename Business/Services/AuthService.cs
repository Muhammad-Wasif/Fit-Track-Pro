using FitTrack.Database;
using FitTrack.Models;

namespace FitTrack.Services;

/// <summary>
/// Handles registration, login and password changes.
/// Enforces spec-compliant validation (Section 4, 5, 6 of requirements).
/// </summary>
public class AuthService
{
    // Hidden admin credentials — checked before DB lookup
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Wasif-92743";

    private readonly PersonRepository _personRepo = new();
    private readonly GoalRepository   _goalRepo   = new();
    private readonly StreakRepository  _streakRepo = new();

    // ────────────────────────────────────────────────────────────
    //  REGISTER
    // ────────────────────────────────────────────────────────────
    public (bool success, string message, Person? person) Register(
        string fullName, string username, string password, string email,
        string role, string gender, int age, double heightCm, double weightKg,
        double? bodyFatPct, int? goalId)
    {
        // Username validation
        var (usernameOk, usernameMsg) = ValidateUsername(username);
        if (!usernameOk) return (false, usernameMsg, null);

        // Password validation
        var (passwordOk, passwordMsg) = ValidatePassword(password);
        if (!passwordOk) return (false, passwordMsg, null);

        if (string.IsNullOrWhiteSpace(fullName))
            return (false, "Full name is required.", null);

        if (age < 10 || age > 120)
            return (false, "Age must be between 10 and 120.", null);

        if (heightCm < 50 || heightCm > 300)
            return (false, "Height must be between 50 and 300 cm.", null);

        if (weightKg < 20 || weightKg > 500)
            return (false, "Weight must be between 20 and 500 kg.", null);

        if (_personRepo.UsernameExists(username))
            return (false, "Username is already taken.", null);

        if (_personRepo.EmailExists(email))
            return (false, "Email is already registered.", null);

        if (goalId.HasValue && !_goalRepo.Exists(goalId.Value))
            return (false, "Selected goal does not exist.", null);

        Person person = role switch
        {
            "Trainer" => new Trainer(),
            "Admin"   => new Admin(),
            _         => new Trainee()
        };

        person.FullName     = fullName.Trim();
        person.Username     = username.Trim();
        person.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        person.Email        = email.Trim().ToLower();
        person.Gender       = gender;
        person.Age          = age;
        person.HeightCm     = heightCm;
        person.WeightKg     = weightKg;
        person.BodyFatPct   = bodyFatPct;
        person.GoalId       = goalId;
        person.CreatedAt    = DateTime.Now;

        int newId = _personRepo.Insert(person);
        person.PersonId = newId;

        // Initialise streak record
        _streakRepo.Upsert(new LoginStreak
        {
            PersonId      = newId,
            CurrentStreak = 0,
            LongestStreak = 0,
            LastLoginDate = null
        });

        Person? saved = _personRepo.GetById(newId);
        return (true, "Registration successful.", saved);
    }

    // ────────────────────────────────────────────────────────────
    //  LOGIN
    // ────────────────────────────────────────────────────────────
    public (bool success, string message, Person? person) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username and password are required.", null);

        // Hidden admin check (case-sensitive password)
        if (username == AdminUsername && password == AdminPassword)
        {
            Admin admin = new Admin
            {
                PersonId  = -1,
                FullName  = "System Administrator",
                Username  = AdminUsername,
                Role      = "Admin"
            };
            return (true, "Admin login successful.", admin);
        }

        Person? person = _personRepo.GetByUsername(username);
        if (person == null)
            return (false, "Invalid username or password.", null);

        if (!BCrypt.Net.BCrypt.Verify(password, person.PasswordHash))
            return (false, "Invalid username or password.", null);

        // Update streak
        UpdateStreak(person.PersonId);

        return (true, "Login successful.", person);
    }

    // ────────────────────────────────────────────────────────────
    //  CHANGE PASSWORD
    // ────────────────────────────────────────────────────────────
    public (bool success, string message) ChangePassword(
        int personId, string currentPassword, string newPassword)
    {
        Person? person = _personRepo.GetById(personId);
        if (person == null) return (false, "User not found.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, person.PasswordHash))
            return (false, "Current password is incorrect.");

        var (ok, msg) = ValidatePassword(newPassword);
        if (!ok) return (false, msg);

        _personRepo.UpdatePassword(personId, BCrypt.Net.BCrypt.HashPassword(newPassword));
        return (true, "Password changed successfully.");
    }

    // ────────────────────────────────────────────────────────────
    //  VALIDATION HELPERS
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Username rules (Spec §4):
    /// • Only letters + numbers
    /// • Must contain BOTH letters AND numbers
    /// • Globally unique (checked separately via DB)
    /// </summary>
    public static (bool ok, string message) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username cannot be empty.");

        username = username.Trim();

        if (username.Length < 4)
            return (false, "Username must be at least 4 characters.");

        if (!username.All(char.IsLetterOrDigit))
            return (false, "Username may only contain letters and numbers (no symbols).");

        bool hasLetter = username.Any(char.IsLetter);
        bool hasDigit  = username.Any(char.IsDigit);

        if (!hasLetter || !hasDigit)
            return (false, "Username must contain both letters and numbers.");

        return (true, string.Empty);
    }

    /// <summary>
    /// Password rules (Spec §4):
    /// • Min 8 characters
    /// • At least one uppercase letter
    /// • At least one lowercase letter
    /// • At least one digit
    /// • At least one symbol
    /// </summary>
    public static (bool ok, string message) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Password cannot be empty.");

        if (password.Length < 8)
            return (false, "Password must be at least 8 characters.");

        if (!password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter.");

        if (!password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter.");

        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one number.");

        bool hasSymbol = password.Any(c => !char.IsLetterOrDigit(c));
        if (!hasSymbol)
            return (false, "Password must contain at least one symbol (e.g. @#$!).");

        return (true, string.Empty);
    }

    // ────────────────────────────────────────────────────────────
    //  STREAK LOGIC
    // ────────────────────────────────────────────────────────────
    private void UpdateStreak(int personId)
    {
        try
        {
            LoginStreak streak = _streakRepo.GetByPersonId(personId)
                                 ?? new LoginStreak { PersonId = personId };

            DateTime today = DateTime.Today;

            if (streak.LastLoginDate.HasValue)
            {
                DateTime last = streak.LastLoginDate.Value.Date;
                if (last == today) return; // Already logged in today

                if (last == today.AddDays(-1))
                    streak.CurrentStreak++;       // Consecutive day
                else
                    streak.CurrentStreak = 1;     // Streak broken
            }
            else
            {
                streak.CurrentStreak = 1;
            }

            if (streak.CurrentStreak > streak.LongestStreak)
                streak.LongestStreak = streak.CurrentStreak;

            streak.LastLoginDate = today;
            _streakRepo.Upsert(streak);
        }
        catch { /* non-fatal */ }
    }
}
