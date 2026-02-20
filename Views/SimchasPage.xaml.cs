using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Jewochron.Services;
using Jewochron.Models;
using System.Collections.ObjectModel;

namespace Jewochron.Views
{
    public sealed partial class SimchasPage : Page
    {
        private SimchaService? simchaService;
        private HebrewCalendarService? hebrewCalendarService;
        private int selectedHebrewDay;
        private int selectedHebrewMonth;
        private int selectedHebrewYear;
        private string selectedHebrewDateString = "";
        
        public ObservableCollection<SimchaDisplayItem> Simchas { get; set; } = new();

        public SimchasPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            hebrewCalendarService = new HebrewCalendarService();
            
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(appDataPath, "Jewochron", "simchas.db");
            simchaService = new SimchaService(dbPath, hebrewCalendarService);

            InitializeDatePickers();
            await LoadSimchas();
        }

        private void InitializeDatePickers()
        {
            if (hebrewCalendarService == null) return;

            for (int i = 1; i <= 30; i++)
            {
                DayComboBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }

            var months = new[] 
            { 
                (1, "Tishrei"), (2, "Cheshvan"), (3, "Kislev"), (4, "Tevet"),
                (5, "Shevat"), (6, "Adar"), (7, "Nisan"), (8, "Iyar"),
                (9, "Sivan"), (10, "Tammuz"), (11, "Av"), (12, "Elul")
            };
            foreach (var (num, name) in months)
            {
                MonthComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = num });
            }

            var currentYear = DateTime.Today.Year;
            var hebrewCalendar = new System.Globalization.HebrewCalendar();
            var currentHebrewYear = hebrewCalendar.GetYear(DateTime.Today);

            for (int i = currentHebrewYear - 50; i <= currentHebrewYear + 20; i++)
            {
                YearComboBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }

            var today = DateTime.Today;
            var hebrewDate = hebrewCalendarService.ConvertToHebrewDate(today);
            
            if (hebrewDate.HasValue)
            {
                DayComboBox.SelectedValue = hebrewDate.Value.day;
                MonthComboBox.SelectedValue = hebrewDate.Value.month;
                YearComboBox.SelectedValue = hebrewDate.Value.year;
                UpdateDateDisplay();
            }
        }

        private void UpdateDateDisplay()
        {
            if (DayComboBox.SelectedItem is ComboBoxItem dayItem &&
                MonthComboBox.SelectedItem is ComboBoxItem monthItem &&
                YearComboBox.SelectedItem is ComboBoxItem yearItem && 
                hebrewCalendarService != null)
            {
                selectedHebrewDay = (int)dayItem.Tag;
                selectedHebrewMonth = (int)monthItem.Tag;
                selectedHebrewYear = (int)yearItem.Tag;

                var formatted = hebrewCalendarService.FormatHebrewDate(selectedHebrewDay, selectedHebrewMonth, selectedHebrewYear);
                selectedHebrewDateString = formatted.hebrew;
                SelectedDateDisplay.Text = $"{formatted.english}\n{formatted.hebrew}";
            }
        }

        private async Task LoadSimchas()
        {
            if (simchaService == null || hebrewCalendarService == null) return;

            var simchas = await simchaService.GetAllSimchasAsync();
            Simchas.Clear();

            foreach (var simcha in simchas)
            {
                var nextOccurrence = simcha.GetNextOccurrence(hebrewCalendarService);
                var displayItem = new SimchaDisplayItem
                {
                    Id = simcha.Id,
                    Name = simcha.Name,
                    Type = simcha.Type,
                    TypeEmoji = GetTypeEmoji(simcha.Type),
                    HebrewDate = simcha.HebrewDate,
                    NextOccurrence = nextOccurrence?.ToString("dddd, MMMM d, yyyy") ?? "Unable to calculate",
                    Notes = simcha.Notes,
                    NotesVisibility = string.IsNullOrWhiteSpace(simcha.Notes) ? Visibility.Collapsed : Visibility.Visible
                };
                Simchas.Add(displayItem);
            }
        }

        private string GetTypeEmoji(string type)
        {
            return type switch
            {
                "HebrewBirthday" => "🎂",
                "BarMitzvah" => "📜",
                "BatMitzvah" => "📜",
                "Wedding" => "💒",
                "Engagement" => "💍",
                "BritMilah" => "👶",
                "Pidyon" => "🕊️",
                "UpSherin" => "✂️",
                "Anniversary" => "💝",
                _ => "🎉"
            };
        }

        private void DayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDateDisplay();
        }

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDateDisplay();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDateDisplay();
        }

        private async void AddSimcha_Click(object sender, RoutedEventArgs e)
        {
            if (simchaService == null || hebrewCalendarService == null) return;

            if (string.IsNullOrWhiteSpace(txtName.Text) || cmbType.SelectedItem is not ComboBoxItem typeItem ||
                selectedHebrewDay == 0 || selectedHebrewMonth == 0 || selectedHebrewYear == 0)
            {
                return;
            }

            var englishDate = hebrewCalendarService?.ConvertToEnglishDate(selectedHebrewDay, selectedHebrewMonth, selectedHebrewYear);

            var simcha = new Simcha
            {
                Name = txtName.Text.Trim(),
                Type = typeItem.Tag?.ToString() ?? "",
                HebrewDate = selectedHebrewDateString,
                HebrewDay = selectedHebrewDay,
                HebrewMonth = selectedHebrewMonth,
                HebrewYear = selectedHebrewYear,
                EnglishDate = englishDate,
                IsRecurring = chkRecurring.IsChecked ?? true,
                Notes = txtNotes.Text.Trim()
            };

            var success = await simchaService.AddSimchaAsync(simcha);
            if (success)
            {
                txtName.Text = "";
                cmbType.SelectedIndex = -1;
                txtNotes.Text = "";
                chkRecurring.IsChecked = true;
                
                await LoadSimchas();
            }
        }

        private async void DeleteSimcha_Click(object sender, RoutedEventArgs e)
        {
            if (simchaService == null) return;

            if (sender is Button button && button.Tag is int id)
            {
                var success = await simchaService.DeleteSimchaAsync(id);
                if (success)
                {
                    await LoadSimchas();
                }
            }
        }

        private void SimchasList_ItemClick(object sender, ItemClickEventArgs e)
        {
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }

    public class SimchaDisplayItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string TypeEmoji { get; set; } = "";
        public string HebrewDate { get; set; } = "";
        public string NextOccurrence { get; set; } = "";
        public string Notes { get; set; } = "";
        public Visibility NotesVisibility { get; set; } = Visibility.Collapsed;
    }
}
