using System.Windows;
using WpfTest.Models;

namespace WpfTest;

//Ввід ПІБ, вибір тесту і запуск проходження
public partial class BeginWindow : Window
{
    public BeginWindow()
    {
        InitializeComponent();

        using var db = new MyDataContext();
        TestsListBox.ItemsSource = db
            .Tests
            .OrderBy(t => t.Title)
            .Select(x=> new TestItemModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                QuestionsCount = x.Questions.Count,
                TimeLimitMinutes = x.TimeLimitMinutes
            })
            .ToList();
        if (TestsListBox.Items.Count > 0)
        {
            TestsListBox.SelectedIndex = 0;
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var lastName = txtLastName.Text.Trim();
        var firstName = txtFirstName.Text.Trim();
        var middleName = txtMiddleName.Text.Trim();

        if (lastName.Length == 0 || firstName.Length == 0)
        {
            MessageBox.Show("Введіть прізвище та ім'я.", "Увага",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TestsListBox.SelectedItem is not TestItemModel selected)
        {
            MessageBox.Show("Оберіть тест зі списку.", "Увага",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (selected.QuestionsCount == 0)
        {
            MessageBox.Show("У вибраному тесті немає питань.", "Увага",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var testWindow = new TestWindow(
            selected.Id,
            lastName,
            firstName,
            middleName.Length == 0 ? null : middleName);

        testWindow.Show();
        Close();
    }
}
