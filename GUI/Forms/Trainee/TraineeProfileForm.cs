using FitTrack.GUI.Controls;
using FitTrack.Models;
using FitTrack.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FitTrack.GUI.Forms.Trainee;

public class TraineeProfileForm : AppShell
{
    // ── Services ─────────────────────────────────────────────────
    private readonly UserService _userSvc = new();
    private readonly GoalService _goalSvc = new();

    // ── Content controls ─────────────────────────────────────────
    private Label       lblHeading   = null!;
    private MetricCard  cardBMI      = null!;
    private MetricCard  cardStatus   = null!;
    private MetricCard  cardBMR      = null!;
    private MetricCard  cardTDEE     = null!;
    private MetricCard  cardTarget   = null!;
    private Label       _lblBMI      = null!;
    private Label       _lblBMR      = null!;
    private Label       _lblTDEE     = null!;
    private RoundedPanel panelEdit   = null!;
    private StyledTextBox _tbFN      = null!;
    private StyledTextBox _tbEmail   = null!;
    private StyledTextBox _tbAge     = null!;
    private ComboBox     _cbGender   = null!;
    private StyledTextBox _tbHeight  = null!;
    private StyledTextBox _tbWeight  = null!;
    private StyledTextBox _tbBF      = null!;
    private ComboBox     _cbGoal     = null!;
    private Label        _lblMsg     = null!;
    private GoldButton   btnSave     = null!;
    private RoundedPanel panelTrainer= null!;
    private bool         _hasTrainer;

    // ── Designer support ─────────────────────────────────────────
    public TraineeProfileForm() : this(new Models.Trainee { FullName = "Trainee Designer", Role = "Trainee" }) { }

    public TraineeProfileForm(Person user) : base(user, "My Profile")
    {
        _hasTrainer = user.TrainerId.HasValue;

        // ── Nav ──
        var btnDash = AddNavButton("🏠", "Dashboard", "dashboard");
        var btnProf = AddNavButton("👤", "Profile",   "profile");
        var btnWork = AddNavButton("💪", "Workout",   "workout");
        var btnNut  = AddNavButton("🥗", "Nutrition", "nutrition");
        var btnProg = AddNavButton("📈", "Progress",  "progress");
        var btnSets = AddNavButton("⚙️", "Settings",  "settings");

        btnDash.Click += (_, _) => NavigateToForm<TraineeDashboardForm>();
        btnProf.Click += (_, _) => NavigateToForm<TraineeProfileForm>();
        btnWork.Click += (_, _) => NavigateToForm<TraineeWorkoutForm>();
        btnNut.Click  += (_, _) => NavigateToForm<TraineeNutritionForm>();
        btnProg.Click += (_, _) => NavigateToForm<TraineeProgressForm>();
        btnSets.Click += (_, _) => NavigateToForm<TraineeSettingsForm>();

        SetActiveNav("profile");
        BuildContent();
        LoadGoals();
        PopulateFields();
        BuildTrainerSection();

        Resize += (_, _) => UITheme.ResponsiveReflow(_contentArea);
    }

    // ─────────────────────────────────────────────────────────────
    //  BUILD CONTENT
    // ─────────────────────────────────────────────────────────────
    private void BuildContent()
    {
        lblHeading = new Label
        {
            Text = "My Profile", Location = new Point(0, 0), Size = new Size(500, 36),
            BackColor = Color.Transparent, Font = UITheme.FontHeading(20f), ForeColor = UITheme.TextPrimary
        };

        // Metric cards row — all 220x96, standard size
        int cx = 0, cy = 48;
        cardBMI    = new MetricCard { Location = new Point(cx,       cy), Size = new Size(220, 96), Label = "BMI",    Metric = "0.0",    AccentColor = UITheme.Gold };
        cardStatus = new MetricCard { Location = new Point(cx += 236, cy), Size = new Size(220, 96), Label = "Status", Metric = "Normal", AccentColor = UITheme.Warning };
        cardBMR    = new MetricCard { Location = new Point(cx += 236, cy), Size = new Size(220, 96), Label = "BMR",    Metric = "0 kcal", AccentColor = UITheme.Red };
        cardTDEE   = new MetricCard { Location = new Point(cx += 236, cy), Size = new Size(220, 96), Label = "TDEE",   Metric = "0 kcal", AccentColor = UITheme.TextSecondary };
        cardTarget = new MetricCard { Location = new Point(cx += 236, cy), Size = new Size(220, 96), Label = "Target", Metric = "0 kcal", AccentColor = UITheme.Success };

        _lblBMI  = new Label { Location = new Point(0,   154), Size = new Size(160, 18), BackColor = Color.Transparent, Font = UITheme.FontSmall(8f), ForeColor = UITheme.TextMuted };
        _lblBMR  = new Label { Location = new Point(236, 154), Size = new Size(160, 18), BackColor = Color.Transparent, Font = UITheme.FontSmall(8f), ForeColor = UITheme.TextMuted };
        _lblTDEE = new Label { Location = new Point(472, 154), Size = new Size(160, 18), BackColor = Color.Transparent, Font = UITheme.FontSmall(8f), ForeColor = UITheme.TextMuted };

        // Edit panel — 2-column grid layout
        panelEdit = new RoundedPanel { Location = new Point(0, 182), Size = new Size(760, 400) };

        int ey = UITheme.S16;

        // Row 1: Full Name | Email
        AddFL(panelEdit, "Full Name",   0,   ey);    _tbFN    = TB(panelEdit, 0,   ey + 18, 220);
        AddFL(panelEdit, "Email",       240, ey);    _tbEmail = TB(panelEdit, 240, ey + 18, 220);
        ey += 74;

        // Row 2: Age | Gender
        AddFL(panelEdit, "Age",         0,   ey);    _tbAge   = TB(panelEdit, 0,   ey + 18, 100);
        AddFL(panelEdit, "Gender",      240, ey);
        _cbGender = new ComboBox { Location = new Point(240, ey + 18), Size = new Size(220, UITheme.TextBoxHeight), DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbGender);
        _cbGender.Items.AddRange(new object[] { "Male", "Female", "Other" });
        _cbGender.SelectedIndex = 0;
        panelEdit.Controls.Add(_cbGender);
        ey += 74;

        // Row 3: Height | Weight
        AddFL(panelEdit, "Height (cm)", 0,   ey);    _tbHeight = TB(panelEdit, 0,   ey + 18, 100);
        AddFL(panelEdit, "Weight (kg)", 240, ey);    _tbWeight = TB(panelEdit, 240, ey + 18, 100);
        _tbWeight.Inner.TextChanged += (_, _) => LiveUpdateMetrics();
        ey += 74;

        // Row 4: Body Fat | Goal
        AddFL(panelEdit, "Body Fat %",  0,   ey);    _tbBF    = TB(panelEdit, 0,   ey + 18, 100);
        AddFL(panelEdit, "Goal",        240, ey);
        _cbGoal = new ComboBox { Location = new Point(240, ey + 18), Size = new Size(220, UITheme.TextBoxHeight), DropDownStyle = ComboBoxStyle.DropDownList };
        UITheme.StyleComboBox(_cbGoal);
        panelEdit.Controls.Add(_cbGoal);
        ey += 74;

        _lblMsg = new Label { Location = new Point(0, ey), Size = new Size(700, 22), BackColor = Color.Transparent, Font = UITheme.FontBody(9f) };
        panelEdit.Controls.Add(_lblMsg);
        ey += 30;

        btnSave = new GoldButton { Text = "Save Profile", Location = new Point(0, ey), Size = new Size(200, UITheme.ButtonHeight) };
        btnSave.Click += (_, _) => SaveProfile();
        panelEdit.Controls.Add(btnSave);

        // Trainer section
        panelTrainer = new RoundedPanel { Location = new Point(0, 600), Size = new Size(760, 110) };

        _contentArea.Controls.AddRange(new Control[]
        {
            lblHeading, cardBMI, cardStatus, cardBMR, cardTDEE, cardTarget,
            _lblBMI, _lblBMR, _lblTDEE, panelEdit, panelTrainer
        });
    }

    private StyledTextBox TB(Panel p, int x, int y, int w)
    {
        var tb = new StyledTextBox { Location = new Point(x, y), Size = new Size(w, UITheme.TextBoxHeight) };
        p.Controls.Add(tb);
        return tb;
    }

    private void AddFL(Panel p, string text, int x, int y)
    {
        p.Controls.Add(new Label
        {
            Text = text, Location = new Point(x, y), Size = new Size(220, 16),
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(8.5f), ForeColor = UITheme.TextSecondary
        });
    }

    // ─────────────────────────────────────────────────────────────
    //  LOGIC
    // ─────────────────────────────────────────────────────────────
    private void PopulateFields()
    {
        _tbFN.Text     = CurrentUser.FullName;
        _tbEmail.Text  = CurrentUser.Email;
        _tbAge.Text    = CurrentUser.Age.ToString();
        _tbHeight.Text = CurrentUser.HeightCm.ToString();
        _tbWeight.Text = CurrentUser.WeightKg.ToString();
        _tbBF.Text     = CurrentUser.BodyFatPct?.ToString() ?? "";
        _cbGender.SelectedItem = CurrentUser.Gender;

        var (bmi, cat, bmr, tdee, target) = _goalSvc.GetMetrics(CurrentUser.PersonId);
        cardBMI.Metric    = $"{bmi:F1}";
        cardStatus.Metric = cat;
        cardBMR.Metric    = $"{bmr:F0} kcal";
        cardTDEE.Metric   = $"{tdee:F0} kcal";
        cardTarget.Metric = $"{target:F0} kcal";
    }

    private void LoadGoals()
    {
        _cbGoal.Items.Clear();
        _cbGoal.Items.Add("— Select —");
        foreach (var g in _goalSvc.GetAll()) _cbGoal.Items.Add(g);
        if (CurrentUser.GoalId.HasValue)
        {
            foreach (var item in _cbGoal.Items.OfType<Goal>())
            {
                if (item.GoalId == CurrentUser.GoalId) { _cbGoal.SelectedItem = item; break; }
            }
        }
    }

    private void BuildTrainerSection()
    {
        panelTrainer.Controls.Clear();
        var lblTitle = new Label
        {
            Text = "Trainer Assignment", Left = UITheme.S16, Top = 10, Width = 400, Height = 22,
            BackColor = Color.Transparent, Font = UITheme.FontSemiBold(11f), ForeColor = UITheme.TextPrimary
        };
        panelTrainer.Controls.Add(lblTitle);

        if (_hasTrainer)
        {
            string trainerName = (CurrentUser is Models.Trainee t) ? t.TrainerName ?? "Assigned" : "Assigned";
            panelTrainer.Controls.Add(new Label
            {
                Text = $"🔒  Trainer: {trainerName}  (locked — contact trainer to change)",
                Left = UITheme.S16, Top = 40, Width = 700, Height = 22, BackColor = Color.Transparent,
                Font = UITheme.FontBody(10f), ForeColor = UITheme.TextSecondary
            });
        }
        else
        {
            var trainers = new UserService().GetAllTrainers();
            var cbT = new ComboBox { Left = UITheme.S16, Top = 36, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            UITheme.StyleComboBox(cbT);
            cbT.Items.Add("— No Trainer —");
            foreach (var tr in trainers) cbT.Items.Add(tr);
            cbT.DisplayMember = "FullName";
            cbT.SelectedIndex = 0;
            panelTrainer.Controls.Add(cbT);

            var btnAssign   = new GoldButton { Text = "Select Trainer", Left = 290, Top = 36, Width = 150, Height = UITheme.ButtonHeight, Style = GoldButton.ButtonStyle.Ghost };
            var lblAssignMsg= new Label { Left = 454, Top = 44, Width = 280, Height = 22, BackColor = Color.Transparent, Font = UITheme.FontBody(9f) };
            panelTrainer.Controls.Add(lblAssignMsg);
            btnAssign.Click += (_, _) =>
            {
                if (cbT.SelectedItem is not Person trainer) return;
                var (ok, msg) = new UserService().AssignTrainer(CurrentUser.PersonId, trainer.PersonId);
                lblAssignMsg.Text     = ok ? "✓ Trainer assigned!" : msg;
                lblAssignMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
                if (ok) { _hasTrainer = true; btnAssign.Enabled = false; cbT.Enabled = false; }
            };
            panelTrainer.Controls.Add(btnAssign);
        }
    }

    private void LiveUpdateMetrics()
    {
        if (!double.TryParse(_tbWeight.Text, out double wt)) return;
        if (!double.TryParse(_tbHeight.Text, out double ht)) return;
        double h = ht / 100.0;
        double bmi = h > 0 ? Math.Round(wt / (h * h), 1) : 0;
        _lblBMI.Text  = $"BMI preview: {bmi}";
        _lblBMR.Text  = "";
        _lblTDEE.Text = "";
    }

    private void SaveProfile()
    {
        if (!int.TryParse(_tbAge.Text, out int age))         { _lblMsg.Text = "Invalid age.";    _lblMsg.ForeColor = UITheme.Red; return; }
        if (!double.TryParse(_tbHeight.Text, out double ht)) { _lblMsg.Text = "Invalid height."; _lblMsg.ForeColor = UITheme.Red; return; }
        if (!double.TryParse(_tbWeight.Text, out double wt)) { _lblMsg.Text = "Invalid weight."; _lblMsg.ForeColor = UITheme.Red; return; }
        double? bf = double.TryParse(_tbBF.Text, out double bfv) ? bfv : (double?)null;
        int? goalId = _cbGoal.SelectedItem is Goal g ? g.GoalId : (int?)null;

        var (ok, msg) = new UserService().UpdateProfile(CurrentUser.PersonId,
            _tbFN.Text, _tbEmail.Text, _cbGender.SelectedItem?.ToString() ?? "Other", age, ht, wt, bf, goalId);
        _lblMsg.Text     = ok ? "✓ Profile saved." : msg;
        _lblMsg.ForeColor = ok ? UITheme.Success : UITheme.Red;
        if (ok)
        {
            CurrentUser.FullName = _tbFN.Text; CurrentUser.Email = _tbEmail.Text;
            CurrentUser.Age = age; CurrentUser.HeightCm = ht; CurrentUser.WeightKg = wt;
            CurrentUser.BodyFatPct = bf; CurrentUser.GoalId = goalId;
            PopulateFields();
        }
    }
}
