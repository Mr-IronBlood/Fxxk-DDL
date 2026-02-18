using FxxkDDL.Core.Common;
using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace FxxkDDL.Core.ViewModels
{
    /// <summary>
    /// 日历页面ViewModel
    /// </summary>
    public class CalendarViewModel : ViewModelBase
    {
        private readonly ICalendarService _calendarService;
        private DateTime _currentDate;
        private DateTime _weekStartDate;
        private int _weekDays;
        private bool _isMonthView;
        private List<DayCellData> _monthDays;
        private List<WeekDayData> _weekDaysData;

        /// <summary>
        /// 月视图日期单元格数据
        /// </summary>
        public class DayCellData
        {
            public int DayNumber { get; set; }
            public DateTime Date { get; set; }
            public bool IsCurrentMonth { get; set; }
            public bool IsToday { get; set; }
            public int EventCount { get; set; }
            public bool HasEvents => EventCount > 0;
            public List<DotData> Dots { get; set; } = new List<DotData>();
            public Brush TextColor => IsCurrentMonth ?
                (IsToday ? Brushes.White : Brushes.Black) :
                Brushes.Gray;
        }

        /// <summary>
        /// 点数据（用于月视图事件标记）
        /// </summary>
        public class DotData
        {
            public Brush Color { get; set; }
            public string Importance { get; set; }
        }

        /// <summary>
        /// 周视图日期列数据
        /// </summary>
        public class WeekDayData
        {
            public DateTime Date { get; set; }
            public string DayOfWeek { get; set; }
            public string DateString { get; set; }
            public bool IsToday { get; set; }
            public string IsTodayText => IsToday ? "今天" : "";
            public Brush DateBackground { get; set; }
            public Brush DateColor { get; set; }
            public Brush DayOfWeekColor { get; set; }
            public List<WeekTaskData> Events { get; set; } = new List<WeekTaskData>();
        }

        /// <summary>
        /// 周视图任务数据
        /// </summary>
        public class WeekTaskData
        {
            public string EventId { get; set; }
            public string TaskDescription { get; set; }
            public DateTime Deadline { get; set; }
            public string TimeString { get; set; }
            public string Importance { get; set; }
            public Brush BackgroundColor { get; set; }
            public Brush ImportanceColor { get; set; }
            public CalendarEvent OriginalEvent { get; set; }

            // 卡片样式属性
            public string CardBackgroundColor { get; set; }
            public string CardBorderBrush { get; set; }
            public double CardShadowDepth { get; set; }
            public Brush TaskDescriptionColor { get; set; }
            public Brush TimeStringColor { get; set; }
        }

        /// <summary>
        /// 当前日期
        /// </summary>
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (SetProperty(ref _currentDate, value))
                {
                    if (IsMonthView)
                    {
                        LoadMonthView();
                    }
                    else
                    {
                        LoadWeekView();
                    }
                }
            }
        }

        /// <summary>
        /// 周开始日期
        /// </summary>
        public DateTime WeekStartDate
        {
            get => _weekStartDate;
            set
            {
                if (SetProperty(ref _weekStartDate, value))
                {
                    LoadWeekView();
                }
            }
        }

        /// <summary>
        /// 周视图天数
        /// </summary>
        public int WeekDays
        {
            get => _weekDays;
            set
            {
                if (SetProperty(ref _weekDays, value))
                {
                    LoadWeekView();
                }
            }
        }

        /// <summary>
        /// 是否为月视图
        /// </summary>
        public bool IsMonthView
        {
            get => _isMonthView;
            set
            {
                if (SetProperty(ref _isMonthView, value))
                {
                    if (value)
                    {
                        LoadMonthView();
                    }
                    else
                    {
                        LoadWeekView();
                    }
                }
            }
        }

        /// <summary>
        /// 月视图天数数据
        /// </summary>
        public List<DayCellData> MonthDays
        {
            get => _monthDays;
            private set => SetProperty(ref _monthDays, value);
        }

        /// <summary>
        /// 周视图天数数据
        /// </summary>
        public List<WeekDayData> WeekDaysData
        {
            get => _weekDaysData;
            private set => SetProperty(ref _weekDaysData, value);
        }

        /// <summary>
        /// 月视图命令
        /// </summary>
        public ICommand SwitchToMonthViewCommand { get; }

        /// <summary>
        /// 周视图命令
        /// </summary>
        public ICommand SwitchToWeekViewCommand { get; }

        /// <summary>
        /// 上一月/周命令
        /// </summary>
        public ICommand PreviousCommand { get; }

        /// <summary>
        /// 今天命令
        /// </summary>
        public ICommand TodayCommand { get; }

        /// <summary>
        /// 下一月/周命令
        /// </summary>
        public ICommand NextCommand { get; }

        /// <summary>
        /// 7天视图命令
        /// </summary>
        public ICommand SevenDaysCommand { get; }

        /// <summary>
        /// 14天视图命令
        /// </summary>
        public ICommand FourteenDaysCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public CalendarViewModel()
        {
            _calendarService = ServiceLocator.GetService<ICalendarService>();

            // 初始化属性
            _currentDate = DateTime.Today;
            _weekStartDate = DateTime.Today;
            _weekDays = 7;
            _isMonthView = true;

            // 初始化命令
            SwitchToMonthViewCommand = new RelayCommand(() => IsMonthView = true);
            SwitchToWeekViewCommand = new RelayCommand(() => IsMonthView = false);
            PreviousCommand = new RelayCommand(GoPrevious);
            TodayCommand = new RelayCommand(GoToday);
            NextCommand = new RelayCommand(GoNext);
            SevenDaysCommand = new RelayCommand(() => WeekDays = 7);
            FourteenDaysCommand = new RelayCommand(() => WeekDays = 14);

            // 初始加载
            LoadMonthView();
        }

        /// <summary>
        /// 加载月视图
        /// </summary>
        private void LoadMonthView()
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    var year = CurrentDate.Year;
                    var month = CurrentDate.Month;

                    // 获取当前月份的事件
                    var events = _calendarService.GetEventsForMonth(year, month);

                    // 生成月视图数据
                    var monthDays = new List<DayCellData>();

                    // 计算当月第一天和最后一天
                    var firstDayOfMonth = new DateTime(year, month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                    // 计算第一天是星期几（0=周日，1=周一，...）
                    int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                    // 转换为周一为第一天（0=周一，6=周日）
                    firstDayOfWeek = firstDayOfWeek == 0 ? 6 : firstDayOfWeek - 1;

                    // 添加上个月的天数
                    var previousMonth = firstDayOfMonth.AddMonths(-1);
                    var daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);

                    for (int i = 0; i < firstDayOfWeek; i++)
                    {
                        var day = daysInPreviousMonth - firstDayOfWeek + i + 1;
                        var date = new DateTime(previousMonth.Year, previousMonth.Month, day);
                        monthDays.Add(CreateDayCellData(date, false));
                    }

                    // 添加当月的天数
                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        var date = new DateTime(year, month, day);
                        monthDays.Add(CreateDayCellData(date, true));
                    }

                    // 添加下个月的天数（补全6行7列=42天）
                    int totalCells = 42;
                    int remainingCells = totalCells - monthDays.Count;
                    var nextMonth = firstDayOfMonth.AddMonths(1);

                    for (int i = 0; i < remainingCells; i++)
                    {
                        var date = new DateTime(nextMonth.Year, nextMonth.Month, i + 1);
                        monthDays.Add(CreateDayCellData(date, false));
                    }

                    MonthDays = monthDays;
                }
                catch (Exception ex)
                {
                    SetError($"加载月视图失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 加载周视图
        /// </summary>
        private void LoadWeekView()
        {
            ExecuteWithBusyAsync(async () =>
            {
                try
                {
                    var events = _calendarService.GetEventsForWeek(WeekStartDate, WeekDays);
                    var weekDaysData = new List<WeekDayData>();

                    for (int i = 0; i < WeekDays; i++)
                    {
                        var date = WeekStartDate.AddDays(i);
                        var isToday = date.Date == DateTime.Today;

                        var dayData = new WeekDayData
                        {
                            Date = date,
                            DayOfWeek = GetChineseDayOfWeek(date.DayOfWeek),
                            DateString = date.ToString("MM-dd"),
                            IsToday = isToday,
                            DateBackground = isToday ? Brushes.LightBlue : Brushes.Transparent,
                            DateColor = isToday ? Brushes.White : Brushes.Black,
                            DayOfWeekColor = isToday ? Brushes.White : Brushes.Gray
                        };

                        // 获取当天的任务
                        var dayEvents = events.Where(e => e.Date.Date == date.Date).ToList();
                        foreach (var ev in dayEvents)
                        {
                            // 根据重要性获取颜色
                            var importance = ev.Task?.Importance ?? "中";
                            var importanceColor = ev.GetColorByImportance();
                            var importanceColorBrush = new SolidColorBrush(importanceColor);
                            var cardBackgroundColor = importance switch
                            {
                                "高" => "#E74C3C",     // 红色背景
                                "中" => "#F1C40F",    // 黄色背景
                                "低" => "#2ECC71",    // 绿色背景
                                _ => "#ECF0F1"      // 灰色背景
                            };

                            var cardBorderBrush = "#D4D4D4"; // 浅灰边框
                            var cardShadowDepth = importance switch
                            {
                                "高" => 8.0,
                                "中" => 5.0,
                                "低" => 3.0,
                                _ => 2.0
                            };

                            var taskDescriptionColor = Brushes.White;
                            var timeStringColor = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)); // 90% alpha

                            dayData.Events.Add(new WeekTaskData
                            {
                                EventId = ev.Id,
                                TaskDescription = ev.Task?.Description ?? "未知任务",
                                Deadline = ev.StartTime,
                                TimeString = ev.StartTime.ToString("HH:mm"),
                                Importance = importance,
                                CardBackgroundColor = cardBackgroundColor,
                                CardBorderBrush = cardBorderBrush,
                                CardShadowDepth = cardShadowDepth,
                                TaskDescriptionColor = taskDescriptionColor,
                                TimeStringColor = timeStringColor,
                                BackgroundColor = importanceColorBrush,
                                ImportanceColor = Brushes.White,
                                OriginalEvent = ev
                            });
                        }

                        weekDaysData.Add(dayData);
                    }

                    WeekDaysData = weekDaysData;
                }
                catch (Exception ex)
                {
                    SetError($"加载周视图失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 创建日期单元格数据
        /// </summary>
        private DayCellData CreateDayCellData(DateTime date, bool isCurrentMonth)
        {
            var isToday = date.Date == DateTime.Today;

            // 获取当天的任务
            var events = _calendarService.GetEventsForMonth(date.Year, date.Month)
                .Where(e => e.Date.Date == date.Date)
                .ToList();

            var dots = events.Select(e => new DotData
            {
                Color = new SolidColorBrush(e.GetColorByImportance()),
                Importance = e.Task?.Importance ?? "中"
            }).ToList();

            return new DayCellData
            {
                DayNumber = date.Day,
                Date = date,
                IsCurrentMonth = isCurrentMonth,
                IsToday = isToday,
                EventCount = events.Count,
                Dots = dots
            };
        }

        /// <summary>
        /// 获取中文星期几
        /// </summary>
        private string GetChineseDayOfWeek(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "周一",
                DayOfWeek.Tuesday => "周二",
                DayOfWeek.Wednesday => "周三",
                DayOfWeek.Thursday => "周四",
                DayOfWeek.Friday => "周五",
                DayOfWeek.Saturday => "周六",
                DayOfWeek.Sunday => "周日",
                _ => "未知"
            };
        }

        /// <summary>
        /// 前往上一个月/周
        /// </summary>
        private void GoPrevious()
        {
            if (IsMonthView)
            {
                CurrentDate = CurrentDate.AddMonths(-1);
            }
            else
            {
                WeekStartDate = WeekStartDate.AddDays(-WeekDays);
            }
        }

        /// <summary>
        /// 前往今天
        /// </summary>
        private void GoToday()
        {
            if (IsMonthView)
            {
                CurrentDate = DateTime.Today;
            }
            else
            {
                WeekStartDate = DateTime.Today;
            }
        }

        /// <summary>
        /// 前往下一个月/周
        /// </summary>
        private void GoNext()
        {
            if (IsMonthView)
            {
                CurrentDate = CurrentDate.AddMonths(1);
            }
            else
            {
                WeekStartDate = WeekStartDate.AddDays(WeekDays);
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public void Refresh()
        {
            if (IsMonthView)
            {
                LoadMonthView();
            }
            else
            {
                LoadWeekView();
            }
        }
    }
}