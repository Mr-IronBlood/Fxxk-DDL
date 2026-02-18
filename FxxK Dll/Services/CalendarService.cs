using FxxkDDL.Core.Interfaces;
using FxxkDDL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace FxxkDDL.Services
{
    public class CalendarService : ICalendarService
    {
        /// <summary>
        /// 获取某个月份的所有DDL事件
        /// </summary>
        public List<CalendarEvent> GetEventsForMonth(int year, int month)
        {
            var events = new List<CalendarEvent>();
            // 每次都创建新的 TaskService 以获取最新数据
            var taskService = new TaskService();
            var tasks = taskService.GetPendingTasks();

            foreach (var task in tasks)
            {
                if (task.Deadline.HasValue &&
                    task.Deadline.Value.Year == year &&
                    task.Deadline.Value.Month == month)
                {
                    events.Add(CreateEventFromTask(task));
                }
            }

            return events.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
        }

        /// <summary>
        /// 获取某周的所有DDL事件
        /// </summary>
        public List<CalendarEvent> GetEventsForWeek(DateTime startDate, int days = 7)
        {
            var events = new List<CalendarEvent>();
            var endDate = startDate.AddDays(days - 1);
            var taskService = new TaskService();
            var tasks = taskService.GetPendingTasks();

            foreach (var task in tasks)
            {
                if (task.Deadline.HasValue &&
                    task.Deadline.Value.Date >= startDate.Date &&
                    task.Deadline.Value.Date <= endDate.Date)
                {
                    events.Add(CreateEventFromTask(task));
                }
            }

            return events.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
        }

        /// <summary>
        /// 获取今天的DDL事件
        /// </summary>
        public List<CalendarEvent> GetEventsForToday()
        {
            var today = DateTime.Today;
            var events = new List<CalendarEvent>();
            var taskService = new TaskService();
            var tasks = taskService.GetPendingTasks();

            foreach (var task in tasks)
            {
                if (task.Deadline.HasValue && task.Deadline.Value.Date == today)
                {
                    events.Add(CreateEventFromTask(task));
                }
            }

            return events.OrderBy(e => e.StartTime).ToList();
        }

        /// <summary>
        /// 获取从今天开始的未来DDL事件（用于周视图）
        /// </summary>
        public List<CalendarEvent> GetUpcomingEvents(int maxDays = 14)
        {
            var today = DateTime.Today;
            var endDate = today.AddDays(maxDays);
            var events = new List<CalendarEvent>();
            var taskService = new TaskService();
            var tasks = taskService.GetPendingTasks();

            foreach (var task in tasks)
            {
                if (task.Deadline.HasValue &&
                    task.Deadline.Value.Date >= today &&
                    task.Deadline.Value.Date <= endDate)
                {
                    events.Add(CreateEventFromTask(task));
                }
            }

            return events.OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
        }

        /// <summary>
        /// 根据任务创建日历事件
        /// </summary>
        private CalendarEvent CreateEventFromTask(DDLTask task)
        {
            return new CalendarEvent
            {
                Task = task,
                Date = task.Deadline?.Date ?? DateTime.Today,
                StartTime = task.Deadline ?? DateTime.Today,
                EndTime = (task.Deadline ?? DateTime.Today).AddHours(1),
                EventColor = task.GetDisplayColor(),
                IsCompleted = task.IsCompleted,
                IsPinned = false
            };
        }


    }
}
