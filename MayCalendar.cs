using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Globalization;

public class MayCalendarForm : Form
{
    private Button buttonPrevMonth;
private Button buttonNextMonth;
private Button buttonPrevYear;
private Button buttonNextYear;
private CheckBox checkBoxHolidays;
private Button buttonAddEvent;
private Button buttonDeleteEvent;
private Button buttonBgColor;
private Button buttonBgImage;
private Label labelResult;
private Label labelCurrentMonth;
private Panel panelCalendar;
private DateTime currentDate = DateTime.Now;
private DateTime? selectedDate;
private Color baseDayBackColor = Color.White;
    
    // Система событий
    private Dictionary<DateTime, List<EventInfo>> userEvents = new Dictionary<DateTime, List<EventInfo>>();
    private const string EventsFileName = "calendar_events.json";

    // Класс для хранения информации о событии
    public class EventInfo
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public RecurrenceType Recurrence { get; set; }
        public DateTime? EndDate { get; set; } // Дата окончания повторения (опционально)
    }

    // Типы повторения событий
    public enum RecurrenceType
    {
        None,       // Без повторения
        Daily,      // Ежедневно
        Weekly,     // Еженедельно
        Monthly,    // Ежемесячно
        Yearly      // Ежегодно
    }

    // Российские праздники
    private Dictionary<string, string> russianHolidays = new Dictionary<string, string>
    {
        {"01.01", "Новый год"},
        {"02.01", "Новогодние каникулы"},
        {"03.01", "Новогодние каникулы"},
        {"04.01", "Новогодние каникулы"},
        {"05.01", "Новогодние каникулы"},
        {"06.01", "Новогодние каникулы"},
        {"07.01", "Рождество Христово"},
        {"08.01", "Новогодние каникулы"},
        {"23.02", "День защитника Отечества"},
        {"08.03", "Международный женский день"},
        {"01.05", "Праздник Весны и Труда"},
        {"09.05", "День Победы"},
        {"12.06", "День России"},
        {"04.11", "День народного единства"}
    };

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MayCalendarForm());
    }

    public MayCalendarForm()
    {
        // Нормализуем текущую дату на первый день месяца, чтобы избежать смещения на конец месяца
        currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
        InitializeComponent();
        LoadEvents();
        SetupCalendar();
    }

    private void InitializeComponent()
    {
        this.Text = "Календарь Мая";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Панель для навигации
        Panel navigationPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.LightGray
        };

        // Кнопка "Предыдущий месяц"
        buttonPrevMonth = new Button
        {
            Text = "<",
            Size = new Size(40, 30),
            Location = new Point(10, 10),
            Font = new Font("Arial", 12, FontStyle.Bold)
        };
        buttonPrevMonth.Click += ButtonPrevMonth_Click;

        // Кнопка "Следующий месяц"
        buttonNextMonth = new Button
        {
            Text = ">",
            Size = new Size(40, 30),
            Location = new Point(60, 10),
            Font = new Font("Arial", 12, FontStyle.Bold)
        };
        buttonNextMonth.Click += ButtonNextMonth_Click;

        // Метка с текущим месяцем и годом
        labelCurrentMonth = new Label
        {
            Text = GetMonthYearText(currentDate),
            Size = new Size(200, 30),
            Location = new Point(120, 15),
            Font = new Font("Arial", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Кнопки навигации по годам
        buttonPrevYear = new Button
        {
            Text = "<<",
            Size = new Size(50, 30),
            Location = new Point(340, 10),
            Font = new Font("Arial", 10, FontStyle.Bold)
        };
        buttonPrevYear.Click += ButtonPrevYear_Click;

        buttonNextYear = new Button
        {
            Text = ">>",
            Size = new Size(50, 30),
            Location = new Point(400, 10),
            Font = new Font("Arial", 10, FontStyle.Bold)
        };
        buttonNextYear.Click += ButtonNextYear_Click;

        navigationPanel.Controls.Add(buttonPrevMonth);
        navigationPanel.Controls.Add(buttonNextMonth);
        navigationPanel.Controls.Add(labelCurrentMonth);
        navigationPanel.Controls.Add(buttonPrevYear);
        navigationPanel.Controls.Add(buttonNextYear);

        // Подсказки для навигационных кнопок
        var navToolTip = new ToolTip();
        navToolTip.SetToolTip(buttonPrevMonth, "Предыдущий месяц");
        navToolTip.SetToolTip(buttonNextMonth, "Следующий месяц");
        navToolTip.SetToolTip(buttonPrevYear, "Предыдущий год");
        navToolTip.SetToolTip(buttonNextYear, "Следующий год");

        // Панель для отображения дней месяца
        panelCalendar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            AutoScroll = true
        };

        // Чекбокс для российских праздников
        checkBoxHolidays = new CheckBox
        {
            Text = "Показывать российские праздники",
            Location = new Point(460, 15),
            Size = new Size(200, 20),
            Checked = true
        };
        checkBoxHolidays.CheckedChanged += CheckBoxHolidays_CheckedChanged;
        navigationPanel.Controls.Add(checkBoxHolidays);

        // Кнопка добавления события
        buttonAddEvent = new Button
        {
            Text = "Добавить событие",
            Size = new Size(120, 30),
            Location = new Point(10, 10),
            BackColor = Color.LightBlue
        };
        buttonAddEvent.Click += ButtonAddEvent_Click;

        // Кнопка удаления события (только вручную добавленные)
        buttonDeleteEvent = new Button
        {
            Text = "Удалить событие",
            Size = new Size(120, 30),
            Location = new Point(140, 10), // справа от кнопки добавления
            BackColor = Color.LightCoral
        };
        buttonDeleteEvent.Click += ButtonDeleteEvent_Click;

        // Кнопка выбора цвета фона календаря
        buttonBgColor = new Button
        {
            Text = "Фон (цвет)",
            Size = new Size(110, 30),
            Location = new Point(270, 10),
            BackColor = Color.Beige
        };
        buttonBgColor.Click += ButtonBgColor_Click;

        // Кнопка выбора фонового изображения
        buttonBgImage = new Button
        {
            Text = "Фон (картинка)",
            Size = new Size(130, 30),
            Location = new Point(390, 10),
            BackColor = Color.Beige
        };
        buttonBgImage.Click += ButtonBgImage_Click;

        // Панель для кнопки и результата
        Panel bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 150,
            BackColor = Color.LightGray
        };

        // Метка для отображения результата
        labelResult = new Label
        {
            Location = new Point(10, 50),
            Size = new Size(560, 90),
            Font = new Font("Arial", 10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.TopLeft
        };

        bottomPanel.Controls.Add(buttonAddEvent);
        bottomPanel.Controls.Add(buttonDeleteEvent);
        bottomPanel.Controls.Add(buttonBgColor);
        bottomPanel.Controls.Add(buttonBgImage);
        bottomPanel.Controls.Add(labelResult);

        this.Controls.Add(panelCalendar);
        this.Controls.Add(navigationPanel);
        this.Controls.Add(bottomPanel);

        // Инициализируем календарь
        CreateCalendarGrid();
    }

    private void CreateCalendarGrid()
    {
        panelCalendar.Controls.Clear();
        
        DateTime firstDay = new DateTime(currentDate.Year, currentDate.Month, 1);
        int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
        
        // Заголовки дней недели
        string[] dayNames = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
        for (int i = 0; i < 7; i++)
        {
            Label dayHeader = new Label
            {
                Text = dayNames[i],
                Size = new Size(80, 30),
                Location = new Point(i * 80 + 10, 10),
                Font = new Font("Arial", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelCalendar.Controls.Add(dayHeader);
        }
        
        // Определяем день недели первого дня месяца (0 = воскресенье, 1 = понедельник, ...)
        int firstDayOfWeek = (int)firstDay.DayOfWeek;
        if (firstDayOfWeek == 0) firstDayOfWeek = 7; // Воскресенье = 7
        firstDayOfWeek--; // Приводим к 0-6 где 0 = понедельник
        
        // Создаем кнопки для дней
        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime currentDayDate = new DateTime(currentDate.Year, currentDate.Month, day);
            
            Button dayButton = new Button
            {
                Text = day.ToString(),
                Size = new Size(78, 50),
                Location = new Point((firstDayOfWeek + day - 1) % 7 * 80 + 10, 
                                   ((firstDayOfWeek + day - 1) / 7 + 1) * 55 + 10),
                Font = new Font("Arial", 10),
                Tag = currentDayDate
            };
            
            // Устанавливаем цвет в зависимости от типа дня
            SetDayButtonColor(dayButton, currentDayDate);
            
            dayButton.Click += DayButton_Click;
            panelCalendar.Controls.Add(dayButton);
        }
    }
    
    private void SetDayButtonColor(Button button, DateTime date)
    {
        // Проверяем российские праздники (зеленый цвет)
        if (checkBoxHolidays.Checked && IsRussianHoliday(date))
        {
            button.BackColor = Color.Green;
            button.ForeColor = Color.White;
        }
        // Проверяем выходные дни - суббота и воскресенье (красный цвет)
        else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
        {
            button.BackColor = Color.Red;
            button.ForeColor = Color.White;
        }
        else
        {
            // Используем настраиваемый базовый цвет вместо фиксированного белого
            button.BackColor = baseDayBackColor;
            button.ForeColor = Color.Black;
        }
        
        // Выделяем сегодняшний день
        if (date.Date == DateTime.Today)
        {
            button.Font = new Font(button.Font, FontStyle.Bold);
        }
        
        // Подсветка дней с событиями (приоритет над остальными)
        if (GetEventsForDate(date).Any())
        {
            button.BackColor = Color.MediumPurple;
            button.ForeColor = Color.White;
        }
    }
     private void DayButton_Click(object sender, EventArgs e)
     {
         Button clickedButton = sender as Button;
         DateTime clickedDate = (DateTime)clickedButton.Tag;
         // Сохраняем выбранную дату для последущего добавления события
         selectedDate = clickedDate;
         
         string result = $"Выбрана дата: {clickedDate:dd.MM.yyyy} ({GetDayName(clickedDate.DayOfWeek)})\n";
         
         // Отображаем, является ли выбранная дата праздником
         if (checkBoxHolidays.Checked && IsRussianHoliday(clickedDate))
         {
             result += $"Российский праздник: {GetHolidayName(clickedDate)}\n";
         }
         
         // Отмечаем выходные дни
         if (clickedDate.DayOfWeek == DayOfWeek.Saturday || clickedDate.DayOfWeek == DayOfWeek.Sunday)
         {
             result += "Выходной день\n";
         }
         
         // Показываем события на эту дату
         var eventsOnDate = GetEventsForDate(clickedDate);
         if (eventsOnDate.Any())
         {
             result += "\nСобытия на эту дату:\n";
             foreach (var evt in eventsOnDate)
             {
                 string recurrenceText = evt.Recurrence != RecurrenceType.None ? $" ({GetRecurrenceText(evt.Recurrence)})" : "";
                 result += $"• {evt.Title}{recurrenceText}\n";
             }
         }
         else
         {
             result += "\nСобытий на эту дату нет.";
         }
         
         labelResult.Text = result;
     }
      
      private void ButtonAddEvent_Click(object sender, EventArgs e)
      {
          // Используем выбранную дату, если она есть; иначе — сегодня, если он в текущем месяце; иначе — 1-е число текущего месяца
          DateTime dateToUse = selectedDate ??
              ((DateTime.Today.Year == currentDate.Year && DateTime.Today.Month == currentDate.Month)
                  ? DateTime.Today
                  : new DateTime(currentDate.Year, currentDate.Month, 1));
          
          AddEvent(dateToUse);
      }

      private void ButtonDeleteEvent_Click(object sender, EventArgs e)
      {
          // Тот же процесс: клик по дате, затем кнопка удаления; если дата не выбрана — используем ту же дефолтную логику
          DateTime dateToUse = selectedDate ??
              ((DateTime.Today.Year == currentDate.Year && DateTime.Today.Month == currentDate.Month)
                  ? DateTime.Today
                  : new DateTime(currentDate.Year, currentDate.Month, 1));

          DeleteEvent(dateToUse);
      }

       private void ButtonBgColor_Click(object sender, EventArgs e)
       {
           using (var colorDialog = new ColorDialog())
           {
               colorDialog.FullOpen = true;
               colorDialog.Color = panelCalendar.BackColor;
               if (colorDialog.ShowDialog() == DialogResult.OK)
               {
                   // Сбрасываем фон-изображение и задаём цвет
                   if (panelCalendar.BackgroundImage != null)
                   {
                       var old = panelCalendar.BackgroundImage;
                       panelCalendar.BackgroundImage = null;
                       old.Dispose();
                   }
                   panelCalendar.BackColor = colorDialog.Color;
                   // Обновляем базовый цвет дней и пересоздаём сетку, чтобы цвет применился к ячейкам
                   baseDayBackColor = colorDialog.Color;
                   CreateCalendarGrid();
                   
                   // Форсируем перерисовку панели, чтобы изменение стало видимым сразу
                   panelCalendar.Invalidate();
                   panelCalendar.Refresh();

                   labelResult.Text = $"Цвет фона календаря установлен: #{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}";
                   labelResult.ForeColor = Color.Blue;
               }
           }
       }

       private void ButtonBgImage_Click(object sender, EventArgs e)
       {
           using (var ofd = new OpenFileDialog())
           {
               ofd.Title = "Выберите изображение для фона";
               ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*";
               ofd.Multiselect = false;
               if (ofd.ShowDialog() == DialogResult.OK)
               {
                   try
                   {
                       // Загружаем изображение без блокировки исходного файла
                       using (var fs = File.OpenRead(ofd.FileName))
                       using (var img = Image.FromStream(fs))
                       {
                           if (panelCalendar.BackgroundImage != null)
                           {
                               var old = panelCalendar.BackgroundImage;
                               panelCalendar.BackgroundImage = null;
                               old.Dispose();
                           }
                           panelCalendar.BackgroundImage = new Bitmap(img);
                           panelCalendar.BackgroundImageLayout = ImageLayout.Stretch;
                       }
                       // Форсируем перерисовку панели, чтобы изображение применилось немедленно
                       panelCalendar.Invalidate();
                       panelCalendar.Refresh();

                       labelResult.Text = $"Фоновое изображение установлено: {Path.GetFileName(ofd.FileName)}";
                       labelResult.ForeColor = Color.Blue;
                   }
                   catch (Exception ex)
                   {
                       MessageBox.Show($"Не удалось установить фоновое изображение: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                   }
               }
           }
       }
    
    private string GetMonthYearText(DateTime date)
    {
        string[] monthNames = {
            "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
            "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
        };
        return $"{monthNames[date.Month - 1]} {date.Year}";
    }
    
    private void ButtonPrevMonth_Click(object sender, EventArgs e)
    {
        currentDate = currentDate.AddMonths(-1);
        currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
        selectedDate = null;
        labelCurrentMonth.Text = GetMonthYearText(currentDate);
        CreateCalendarGrid();
    }
    
    private void ButtonNextMonth_Click(object sender, EventArgs e)
    {
        currentDate = currentDate.AddMonths(1);
        currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
        selectedDate = null;
        labelCurrentMonth.Text = GetMonthYearText(currentDate);
        CreateCalendarGrid();
    }

    private void SetupCalendar()
    {
        // Обновляем отображение текущего месяца
        labelCurrentMonth.Text = GetMonthYearText(currentDate);
        
        // Создаем календарную сетку
        CreateCalendarGrid();
    }

    private void HighlightSpecialDates()
{
    // Для кастомной сетки достаточно её перерисовать: праздники/выходные/события учитываются в SetDayButtonColor
    CreateCalendarGrid();
}

    private List<DateTime> GetAllEventDates(int year)
    {
        var eventDates = new List<DateTime>();
        
        foreach (var kvp in userEvents)
        {
            foreach (var eventInfo in kvp.Value)
            {
                var eventDate = eventInfo.Date;
                
                switch (eventInfo.Recurrence)
                {
                    case RecurrenceType.None:
                        if (eventDate.Year == year)
                            eventDates.Add(eventDate);
                        break;
                        
                    case RecurrenceType.Daily:
                        var startDate = new DateTime(year, 1, 1);
                        var endDate = new DateTime(year, 12, 31);
                        if (eventInfo.EndDate.HasValue && eventInfo.EndDate.Value < endDate)
                            endDate = eventInfo.EndDate.Value;
                            
                        for (var date = eventDate; date <= endDate && date.Year <= year; date = date.AddDays(1))
                        {
                            if (date.Year == year)
                                eventDates.Add(date);
                        }
                        break;
                        
                    case RecurrenceType.Weekly:
                        for (var date = eventDate; date.Year <= year; date = date.AddDays(7))
                        {
                            if (eventInfo.EndDate.HasValue && date > eventInfo.EndDate.Value)
                                break;
                            if (date.Year == year)
                                eventDates.Add(date);
                        }
                        break;
                        
                    case RecurrenceType.Monthly:
                        for (var date = eventDate; date.Year <= year; date = date.AddMonths(1))
                        {
                            if (eventInfo.EndDate.HasValue && date > eventInfo.EndDate.Value)
                                break;
                            if (date.Year == year)
                                eventDates.Add(date);
                        }
                        break;
                        
                    case RecurrenceType.Yearly:
                        var yearlyDate = new DateTime(year, eventDate.Month, eventDate.Day);
                        if (eventInfo.EndDate.HasValue && yearlyDate > eventInfo.EndDate.Value)
                            break;
                        eventDates.Add(yearlyDate);
                        break;
                }
            }
        }
        
        return eventDates;
    }

    private bool IsWeekend(DateTime date)
    {
        DayOfWeek dayOfWeek = date.DayOfWeek;
        return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
    }

    private bool IsRussianHoliday(DateTime date)
    {
        string key = date.ToString("dd.MM");
        return russianHolidays.ContainsKey(key);
    }

    private string GetHolidayName(DateTime date)
    {
        string key = date.ToString("dd.MM");
        return russianHolidays.ContainsKey(key) ? russianHolidays[key] : null;
    }

    // Навигация по годам

    private void ButtonPrevYear_Click(object sender, EventArgs e)
    {
        // Переключение года для кастомного календаря
        currentDate = currentDate.AddYears(-1);
        currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
        selectedDate = null;
        labelCurrentMonth.Text = GetMonthYearText(currentDate);
        CreateCalendarGrid();
    }

    private void ButtonNextYear_Click(object sender, EventArgs e)
    {
        // Переключение года для кастомного календаря
        currentDate = currentDate.AddYears(1);
        currentDate = new DateTime(currentDate.Year, currentDate.Month, 1);
        selectedDate = null;
        labelCurrentMonth.Text = GetMonthYearText(currentDate);
        CreateCalendarGrid();
    }


    private void CheckBoxHolidays_CheckedChanged(object sender, EventArgs e)
    {
        // Переключение праздников влияет на цвета кнопок — перерисовываем сетку
        CreateCalendarGrid();
    }

    // Система событий

    private void AddEvent(DateTime date)
    {
        using (var form = new Form())
        {
            form.Text = "Новое событие";
            form.Size = new Size(450, 220);
            form.StartPosition = FormStartPosition.CenterParent;
            
            var labelTitle = new Label();
            labelTitle.Text = $"Добавить событие на {date:dd.MM.yyyy}:";
            labelTitle.Location = new Point(10, 10);
            labelTitle.Size = new Size(400, 20);
            
            var textBoxTitle = new TextBox();
            textBoxTitle.Location = new Point(10, 35);
            textBoxTitle.Size = new Size(400, 20);
            
            var labelRecurrence = new Label();
            labelRecurrence.Text = "Повторение:";
            labelRecurrence.Location = new Point(10, 70);
            labelRecurrence.Size = new Size(100, 20);
            
            var comboRecurrence = new ComboBox();
            comboRecurrence.Location = new Point(120, 68);
            comboRecurrence.Size = new Size(150, 25);
            comboRecurrence.DropDownStyle = ComboBoxStyle.DropDownList;
            comboRecurrence.Items.AddRange(new string[] { 
                "Без повторения", 
                "Ежедневно", 
                "Еженедельно", 
                "Ежемесячно", 
                "Ежегодно" 
            });
            comboRecurrence.SelectedIndex = 0;
            
            var labelEndDate = new Label();
            labelEndDate.Text = "До даты (опционально):";
            labelEndDate.Location = new Point(10, 105);
            labelEndDate.Size = new Size(150, 20);
            
            var datePickerEnd = new DateTimePicker();
            datePickerEnd.Location = new Point(170, 103);
            datePickerEnd.Size = new Size(150, 25);
            datePickerEnd.Value = date.AddYears(1);
            datePickerEnd.Enabled = false;
            
            var checkBoxEndDate = new CheckBox();
            checkBoxEndDate.Text = "Ограничить";
            checkBoxEndDate.Location = new Point(330, 105);
            checkBoxEndDate.Size = new Size(100, 20);
            checkBoxEndDate.CheckedChanged += (s, e) => datePickerEnd.Enabled = checkBoxEndDate.Checked;
            
            var buttonOk = new Button();
            buttonOk.Text = "OK";
            buttonOk.Location = new Point(250, 150);
            buttonOk.Size = new Size(75, 25);
            buttonOk.DialogResult = DialogResult.OK;
            
            var buttonCancel = new Button();
            buttonCancel.Text = "Отмена";
            buttonCancel.Location = new Point(335, 150);
            buttonCancel.Size = new Size(75, 25);
            buttonCancel.DialogResult = DialogResult.Cancel;
            
            form.Controls.Add(labelTitle);
            form.Controls.Add(textBoxTitle);
            form.Controls.Add(labelRecurrence);
            form.Controls.Add(comboRecurrence);
            form.Controls.Add(labelEndDate);
            form.Controls.Add(datePickerEnd);
            form.Controls.Add(checkBoxEndDate);
            form.Controls.Add(buttonOk);
            form.Controls.Add(buttonCancel);
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;
            
            if (form.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBoxTitle.Text))
            {
                var eventInfo = new EventInfo
                {
                    Title = textBoxTitle.Text,
                    Date = date.Date,
                    Recurrence = (RecurrenceType)comboRecurrence.SelectedIndex,
                    EndDate = checkBoxEndDate.Checked ? datePickerEnd.Value.Date : (DateTime?)null
                };
                
                if (!userEvents.ContainsKey(date.Date))
                {
                    userEvents[date.Date] = new List<EventInfo>();
                }
                userEvents[date.Date].Add(eventInfo);
                SaveEvents();
                HighlightSpecialDates();
                CreateCalendarGrid();
                
                string recurrenceText = comboRecurrence.SelectedIndex > 0 ? $" ({comboRecurrence.Text})" : "";
                labelResult.Text = $"Событие добавлено на {date:dd.MM.yyyy}: {textBoxTitle.Text}{recurrenceText}";
                labelResult.ForeColor = Color.Blue;
            }
        }
    }

    private void DeleteEvent(DateTime date)
    {
        // Удаляем только события, добавленные вручную (RecurrenceType.None) на указанную дату
        var key = date.Date;
        if (!userEvents.ContainsKey(key))
        {
            labelResult.Text = $"На {date:dd.MM.yyyy} нет пользовательских событий для удаления.";
            labelResult.ForeColor = Color.Blue;
            return;
        }

        var manualEvents = userEvents[key].Where(e => e.Recurrence == RecurrenceType.None).ToList();
        if (manualEvents.Count == 0)
        {
            labelResult.Text = $"На {date:dd.MM.yyyy} нет пользовательских событий для удаления.";
            labelResult.ForeColor = Color.Blue;
            return;
        }

        if (manualEvents.Count == 1)
        {
            // Удаляем единственное событие без выбора
            var toRemove = manualEvents[0];
            userEvents[key].Remove(toRemove);
            if (userEvents[key].Count == 0) userEvents.Remove(key);
            SaveEvents();
            HighlightSpecialDates();
            CreateCalendarGrid();
            labelResult.Text = $"Удалено событие на {date:dd.MM.yyyy}: {toRemove.Title}";
            labelResult.ForeColor = Color.Blue;
            return;
        }

        // Если несколько — дать выбрать в списке
        using (var form = new Form())
        {
            form.Text = $"Удаление событий на {date:dd.MM.yyyy}";
            form.Size = new Size(420, 260);
            form.StartPosition = FormStartPosition.CenterParent;

            var label = new Label();
            label.Text = "Выберите событие для удаления:";
            label.Location = new Point(10, 10);
            label.Size = new Size(380, 20);

            var listBox = new ListBox();
            listBox.Location = new Point(10, 35);
            listBox.Size = new Size(380, 150);
            foreach (var evt in manualEvents)
            {
                listBox.Items.Add(evt.Title);
            }

            var buttonOk = new Button();
            buttonOk.Text = "Удалить";
            buttonOk.Location = new Point(235, 195);
            buttonOk.Size = new Size(75, 25);
            buttonOk.DialogResult = DialogResult.OK;

            var buttonCancel = new Button();
            buttonCancel.Text = "Отмена";
            buttonCancel.Location = new Point(315, 195);
            buttonCancel.Size = new Size(75, 25);
            buttonCancel.DialogResult = DialogResult.Cancel;

            form.Controls.Add(label);
            form.Controls.Add(listBox);
            form.Controls.Add(buttonOk);
            form.Controls.Add(buttonCancel);
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            if (form.ShowDialog() == DialogResult.OK && listBox.SelectedIndex >= 0)
            {
                var toRemove = manualEvents[listBox.SelectedIndex];
                userEvents[key].Remove(toRemove);
                if (userEvents[key].Count == 0) userEvents.Remove(key);
                SaveEvents();
                HighlightSpecialDates();
                CreateCalendarGrid();
                labelResult.Text = $"Удалено событие на {date:dd.MM.yyyy}: {toRemove.Title}";
                labelResult.ForeColor = Color.Blue;
            }
        }
    }
    
    private void LoadEvents()
    {
        try
        {
            if (File.Exists(EventsFileName))
            {
                string json = File.ReadAllText(EventsFileName);
                var eventDict = JsonSerializer.Deserialize<Dictionary<string, List<EventInfo>>>(json);
                
                userEvents.Clear();
                foreach (var kvp in eventDict)
                {
                    if (DateTime.TryParse(kvp.Key, out DateTime date))
                    {
                        userEvents[date.Date] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки событий: {ex.Message}");
        }
    }

    private void SaveEvents()
    {
        try
        {
            var eventDict = new Dictionary<string, List<EventInfo>>();
            foreach (var kvp in userEvents)
            {
                eventDict[kvp.Key.ToString("yyyy-MM-dd")] = kvp.Value;
            }
            
            string json = JsonSerializer.Serialize(eventDict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(EventsFileName, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения событий: {ex.Message}");
        }
    }


    private List<EventInfo> GetEventsForDate(DateTime date)
    {
        var eventsForDate = new List<EventInfo>();
        
        foreach (var kvp in userEvents)
        {
            foreach (var eventInfo in kvp.Value)
            {
                if (IsEventOnDate(eventInfo, date))
                {
                    eventsForDate.Add(eventInfo);
                }
            }
        }
        
        return eventsForDate;
    }

    private bool IsEventOnDate(EventInfo eventInfo, DateTime date)
    {
        var eventDate = eventInfo.Date.Date;
        var checkDate = date.Date;
        
        // Проверяем, не истекло ли событие
        if (eventInfo.EndDate.HasValue && checkDate > eventInfo.EndDate.Value.Date)
            return false;
            
        switch (eventInfo.Recurrence)
        {
            case RecurrenceType.None:
                return eventDate == checkDate;
                
            case RecurrenceType.Daily:
                return checkDate >= eventDate;
                
            case RecurrenceType.Weekly:
                var daysDiff = (checkDate - eventDate).Days;
                return daysDiff >= 0 && daysDiff % 7 == 0;
                
            case RecurrenceType.Monthly:
                return checkDate >= eventDate && 
                       checkDate.Day == eventDate.Day;
                       
            case RecurrenceType.Yearly:
                return checkDate.Month == eventDate.Month && 
                       checkDate.Day == eventDate.Day &&
                       checkDate >= eventDate;
                       
            default:
                return false;
        }
    }

    private string GetRecurrenceText(RecurrenceType recurrence)
    {
        return recurrence switch
        {
            RecurrenceType.Daily => "ежедневно",
            RecurrenceType.Weekly => "еженедельно", 
            RecurrenceType.Monthly => "ежемесячно",
            RecurrenceType.Yearly => "ежегодно",
            _ => ""
        };
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
}
