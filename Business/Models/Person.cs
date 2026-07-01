namespace FitTrack.Models;

public abstract class Person
{
    public int      PersonId      { get; set; }
    public string   FullName      { get; set; } = string.Empty;
    public string   Username      { get; set; } = string.Empty;
    public string   PasswordHash  { get; set; } = string.Empty;
    public string   Email         { get; set; } = string.Empty;
    public string   Role          { get; set; } = string.Empty;
    public string   Gender        { get; set; } = string.Empty;
    public int      Age           { get; set; }
    public double   HeightCm      { get; set; }
    public double   WeightKg      { get; set; }
    public double?  BodyFatPct    { get; set; }
    public int?     GoalId        { get; set; }
    public int?     TrainerId     { get; set; }
    public bool     TrainerLocked { get; set; }   // NEW
    public DateTime CreatedAt     { get; set; }

    public abstract string GetRoleLabel();

    public double CalculateBMI()
    {
        double h = HeightCm / 100.0;
        return h > 0 ? Math.Round(WeightKg / (h * h), 2) : 0;
    }

    public string GetBMICategory()
    {
        double bmi = CalculateBMI();
        if (bmi < 18.5) return "Underweight";
        if (bmi < 25.0) return "Normal";
        if (bmi < 30.0) return "Overweight";
        return "Obese";
    }

    /// <summary>Mifflin-St Jeor BMR</summary>
    public double CalculateBMR()
    {
        double bmr = 10 * WeightKg + 6.25 * HeightCm - 5 * Age;
        return Gender.Equals("Male", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(bmr + 5, 0)
            : Math.Round(bmr - 161, 0);
    }

    /// <summary>TDEE = BMR × activity multiplier</summary>
    public double CalculateTDEE(double activityMultiplier = 1.55)
        => Math.Round(CalculateBMR() * activityMultiplier, 0);

    public override string ToString()
        => $"[{PersonId}] {FullName} ({Username}) — {GetRoleLabel()}";
}
