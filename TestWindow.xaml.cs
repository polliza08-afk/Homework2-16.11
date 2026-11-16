using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfTest.Entities;
using WpfTest.Services;

namespace WpfTest;

/// <summary>
/// Вікно проходження тесту: питання по черзі, підсумок зберігається в БД.
/// </summary>
public partial class TestWindow : Window
{
    private readonly int _testId;
    private readonly string _testTitle;
    private readonly string _lastName;
    private readonly string _firstName;
    private readonly string? _middleName;
    private readonly string _participantName;
    private readonly List<QuestionEntity> _questions;
    private readonly Dictionary<int, List<int>> _selections = new();
    private readonly DateTime _startedAt = DateTime.UtcNow;
    private readonly DateTime? _deadline;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _index;
    private bool _finished;

    public TestWindow(int testId, string lastName, string firstName, string? middleName)
    {
        InitializeComponent();

        using var db = new MyDataContext();
        var test = new TestService(db).GetTestWithQuestions(testId)
            ?? throw new InvalidOperationException("Тест не знайдено.");
        if (test.Questions.Count == 0)
            throw new InvalidOperationException("У тесті немає питань.");

        _testId = testId;
        _testTitle = test.Title;
        _lastName = lastName;
        _firstName = firstName;
        _middleName = middleName;
        _participantName = string.Join(" ",
            new[] { lastName, firstName, middleName }
                .Where(s => !string.IsNullOrEmpty(s)));
        _questions = test.Questions.OrderBy(q => q.Id).ToList();
        Title = $"{_testTitle} — {_participantName}";

        if (test.TimeLimitMinutes is int minutes)
        {
            _deadline = _startedAt.AddMinutes(minutes);
            _timer.Tick += Timer_Tick;
            UpdateTimerText();
            _timer.Start();
        }

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        var question = _questions[_index];

        ProgressBar.Maximum = _questions.Count;
        ProgressBar.Value = _index;
        ProgressText.Text = $"Питання {_index + 1} з {_questions.Count}";
        QuestionText.Text = question.Text;

        AnswersPanel.Children.Clear();
        // Один правильний варіант -> RadioButton, кілька -> CheckBox
        bool singleChoice = question.Answers.Count(a => a.IsCorrect) == 1;
        foreach (var answer in question.Answers.OrderBy(a => a.Id))
        {
            ToggleButton option = singleChoice
                ? new RadioButton()
                : new CheckBox();
            option.Content = answer.Text;
            option.Tag = answer;
            option.Margin = new Thickness(0, 4, 0, 4);
            AnswersPanel.Children.Add(option);
        }

        NextButton.Content = _index == _questions.Count - 1 ? "Завершити" : "Далі";
    }

    /// <summary>Запам'ятовує вибрані варіанти поточного питання.</summary>
    private void CaptureSelection()
    {
        if (_index >= _questions.Count) return;

        _selections[_questions[_index].Id] = AnswersPanel.Children.OfType<ToggleButton>()
            .Where(c => c.IsChecked == true)
            .Select(c => ((AnswerEntity)c.Tag).Id)
            .ToList();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureSelection();

        _index++;
        if (_index < _questions.Count)
        {
            ShowQuestion();
            return;
        }

        await FinishAsync(timedOut: false);
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateTimerText();
        if (DateTime.UtcNow < _deadline) return;

        CaptureSelection();
        await FinishAsync(timedOut: true);
    }

    private void UpdateTimerText()
    {
        var left = _deadline!.Value - DateTime.UtcNow;
        if (left < TimeSpan.Zero) left = TimeSpan.Zero;
        TimerText.Text = $"Залишилось часу: {(int)left.TotalMinutes:00}:{left.Seconds:00}";
    }

    private async Task FinishAsync(bool timedOut)
    {
        if (_finished) return;
        _finished = true;
        _timer.Stop();
        NextButton.IsEnabled = false;

        ProgressBar.Value = _questions.Count;
        ProgressText.Text = "Тест завершено";
        QuestionText.Text = string.Empty;
        AnswersPanel.Children.Clear();

        TestResultEntity result;
        try
        {
            using var db = new MyDataContext();
            result = await new TestResultService(db).SaveResultAsync(
                _testId, _lastName, _firstName, _middleName, _startedAt, _questions, _selections);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося зберегти результат: {ex.Message}", "Помилка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        MessageBox.Show(
            (timedOut ? "Час вичерпано.\n" : string.Empty) +
            $"Тест: {_testTitle}\n" +
            $"Учасник: {_participantName}\n" +
            $"Правильних відповідей: {result.CorrectAnswersCount}\n" +
            $"Неправильних відповідей: {result.WrongAnswersCount}\n" +
            $"Всього питань: {_questions.Count}\n\n" +
            "Результат збережено.",
            "Результат");

        new BeginWindow().Show();
        Close();
    }
}
