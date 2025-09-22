using System;
using System.Drawing;
using System.Windows.Forms;

public class MayCalendarForm : Form
{
    private MonthCalendar monthCalendar1;
    private TextBox textBoxDay;
    private TextBox textBoxStartDay;
    private Button buttonCheck;
    private Button buttonSetStartDay;
    private Label labelDay;
    private Label labelStartDay;
    private Label labelResult;
    private int currentYear = DateTime.Now.Year;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MayCalendarForm());
    }

    public MayCalendarForm()
    {
        InitializeComponents();
        SetupCalendar();
    }

    private void InitializeComponents()
    {
        this.Text = "Календарь мая";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Поле ввода дня месяца
        labelDay = new Label();
        labelDay.Text = "Введите день мая (1-31):";
        labelDay.Location = new Point(20, 20);
        labelDay.Size = new Size(150, 20);

        textBoxDay = new TextBox();
        textBoxDay.Location = new Point(180, 18);
        textBoxDay.Size = new Size(60, 20);

        buttonCheck = new Button();
        buttonCheck.Text = "Проверить";
        buttonCheck.Location = new Point(250, 16);
        buttonCheck.Size = new Size(80, 25);
        buttonCheck.Click += ButtonCheck_Click;

        // Поле ввода начального дня недели
        labelStartDay = new Label();
        labelStartDay.Text = "День недели 1 мая (1-7):";
        labelStartDay.Location = new Point(20, 60);
        labelStartDay.Size = new Size(150, 20);

        textBoxStartDay = new TextBox();
        textBoxStartDay.Location = new Point(180, 58);
        textBoxStartDay.Size = new Size(60, 20);

        buttonSetStartDay = new Button();
        buttonSetStartDay.Text = "Установить";
        buttonSetStartDay.Location = new Point(250, 56);
        buttonSetStartDay.Size = new Size(80, 25);
        buttonSetStartDay.Click += ButtonSetStartDay_Click;

        // Результат проверки
        labelResult = new Label();
        labelResult.Location = new Point(20, 100);
        labelResult.Size = new Size(400, 40);
        labelResult.Font = new Font("Arial", 10, FontStyle.Bold);

        // Календарь
        monthCalendar1 = new MonthCalendar();
        monthCalendar1.Location = new Point(20, 150);
        monthCalendar1.MaxSelectionCount = 1;
        monthCalendar1.DateSelected += MonthCalendar1_DateSelected;

        // Добавляем элементы на форму
        this.Controls.Add(labelDay);
        this.Controls.Add(textBoxDay);
        this.Controls.Add(buttonCheck);
        this.Controls.Add(labelStartDay);
        this.Controls.Add(textBoxStartDay);
        this.Controls.Add(buttonSetStartDay);
        this.Controls.Add(labelResult);
        this.Controls.Add(monthCalendar1);
    }

    private void SetupCalendar()
    {
        // Устанавливаем календарь на май текущего года
        monthCalendar1.SetDate(new DateTime(currentYear, 5, 1));
        monthCalendar1.MinDate = new DateTime(currentYear, 5, 1);
        monthCalendar1.MaxDate = new DateTime(currentYear, 5, 31);
        
        // Выделяем выходные дни
        HighlightHolidays();
    }

    private void HighlightHolidays()
    {
        // Выходные дни: 1-5 мая и 8-10 мая
        DateTime[] holidays = new DateTime[]
        {
            new DateTime(currentYear, 5, 1),
            new DateTime(currentYear, 5, 2),
            new DateTime(currentYear, 5, 3),
            new DateTime(currentYear, 5, 4),
            new DateTime(currentYear, 5, 5),
            new DateTime(currentYear, 5, 8),
            new DateTime(currentYear, 5, 9),
            new DateTime(currentYear, 5, 10)
        };

        monthCalendar1.BoldedDates = holidays;
        monthCalendar1.UpdateBoldedDates();
    }

    private bool IsHoliday(int day)
    {
        // Проверяем специальные выходные дни
        if ((day >= 1 && day <= 5) || (day >= 8 && day <= 10))
            return true;

        // Проверяем субботы и воскресенья
        DateTime date = new DateTime(currentYear, 5, day);
        DayOfWeek dayOfWeek = date.DayOfWeek;
        
        return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
    }

    private void ButtonCheck_Click(object sender, EventArgs e)
    {
        if (int.TryParse(textBoxDay.Text, out int day))
        {
            if (day >= 1 && day <= 31)
            {
                bool isHoliday = IsHoliday(day);
                labelResult.Text = $"День {day} мая: {(isHoliday ? "ВЫХОДНОЙ" : "РАБОЧИЙ")}";
                labelResult.ForeColor = isHoliday ? Color.Red : Color.Green;
            }
            else
            {
                labelResult.Text = "Введите день от 1 до 31";
                labelResult.ForeColor = Color.Red;
            }
        }
        else
        {
            labelResult.Text = "Введите корректное число";
            labelResult.ForeColor = Color.Red;
        }
    }

    private void ButtonSetStartDay_Click(object sender, EventArgs e)
    {
        if (int.TryParse(textBoxStartDay.Text, out int startDay))
        {
            if (startDay >= 1 && startDay <= 7)
            {
                // Преобразуем номер дня недели (1-7) в Day
                Day firstDay = (Day)startDay;
                monthCalendar1.FirstDayOfWeek = firstDay;

                labelResult.Text = $"Месяц начинается с {GetDayName((DayOfWeek)(startDay - 1))}";
                labelResult.ForeColor = Color.Blue;
            }
            else
            {
                labelResult.Text = "Введите день недели от 1 до 7";
                labelResult.ForeColor = Color.Red;
            }
        }
        else
        {
            labelResult.Text = "Введите корректный номер дня недели";
            labelResult.ForeColor = Color.Red;
        }
    }

    private string GetDayName(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Sunday: return "воскресенья";
            case DayOfWeek.Monday: return "понедельника";
            case DayOfWeek.Tuesday: return "вторника";
            case DayOfWeek.Wednesday: return "среды";
            case DayOfWeek.Thursday: return "четверга";
            case DayOfWeek.Friday: return "пятницы";
            case DayOfWeek.Saturday: return "субботы";
            default: return "неизвестного дня";
        }
    }

    private void MonthCalendar1_DateSelected(object sender, DateRangeEventArgs e)
    {
        DateTime selectedDate = e.Start;
        if (selectedDate.Month == 5)
        {
            int day = selectedDate.Day;
            bool isHoliday = IsHoliday(day);
            labelResult.Text = $"Выбран день {day} мая: {(isHoliday ? "ВЫХОДНОЙ" : "РАБОЧИЙ")}";
            labelResult.ForeColor = isHoliday ? Color.Red : Color.Green;
        }
    }
}