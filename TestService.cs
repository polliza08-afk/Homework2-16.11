using Microsoft.EntityFrameworkCore;
using WpfTest.Entities;

namespace WpfTest.Services;

/// <summary>
/// Читання тестів з БД для вікон. Приймає контекст ззовні.
/// </summary>
public class TestService
{
    private readonly MyDataContext _db;

    public TestService(MyDataContext db)
    {
        _db = db;
    }

    //Усі тести, відсортовані за назвою
    public List<TestEntity> GetTests()
    {
        return _db.Tests
            .OrderBy(t => t.Title)
            .ToList();
    }

    //Тест з питаннями та відповідями або null, якщо не знайдено
    public TestEntity? GetTestWithQuestions(int testId)
    {
        return _db.Tests
            .Include(t => t.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefault(t => t.Id == testId);
    }
}
