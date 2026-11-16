using WpfTest.Entities;

namespace WpfTest.Services;

/// <summary>
/// Підрахунок результату проходження тесту та його збереження в БД.
/// </summary>
public class TestResultService
{
    private readonly MyDataContext _db;

    public TestResultService(MyDataContext db)
    {
        _db = db;
    }

    ///Відповідь правильна, якщо вибрано всі правильні варіанти і жодного зайвого
    public static bool IsQuestionCorrect(QuestionEntity question, IReadOnlyCollection<int> selectedAnswerIds)
    {
        var correctIds = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).ToHashSet();
        return selectedAnswerIds.Count > 0 && correctIds.SetEquals(selectedAnswerIds);
    }

    /// <summary>
    /// Рахує результат і зберігає його разом із вибраними відповідями.
    /// Питання без відповіді рахуються як неправильні.
    /// </summary>
    public async Task<TestResultEntity> SaveResultAsync(
        int testId,
        string lastName,
        string firstName,
        string? middleName,
        DateTime startedAtUtc,
        IReadOnlyList<QuestionEntity> questions,
        IReadOnlyDictionary<int, List<int>> selections)
    {
        var result = new TestResultEntity
        {
            TestId = testId,
            LastName = lastName,
            FirstName = firstName,
            MiddleName = middleName,
            StartedAt = startedAtUtc,
            FinisheddAt = DateTime.UtcNow
        };

        foreach (var question in questions)
        {
            var selectedIds = selections.TryGetValue(question.Id, out var ids) ? ids : new List<int>();

            if (IsQuestionCorrect(question, selectedIds)) result.CorrectAnswersCount++;
            else result.WrongAnswersCount++;

            foreach (var answer in question.Answers.Where(a => selectedIds.Contains(a.Id)))
            {
                result.Answers.Add(new TestResultAnswerEntity
                {
                    SelectedAnswerId = answer.Id,
                    IsCorrect = answer.IsCorrect
                });
            }
        }

        _db.TestResults.Add(result);
        await _db.SaveChangesAsync();
        return result;
    }
}
