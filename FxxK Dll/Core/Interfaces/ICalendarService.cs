using FxxkDDL.Models;
using System;
using System.Collections.Generic;

namespace FxxkDDL.Core.Interfaces
{
    /// <summary>
    /// 日历服务接口
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// 获取某个月份的所有DDL事件
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="month">月份</param>
        /// <returns>按日期和时间排序的事件列表</returns>
        List<CalendarEvent> GetEventsForMonth(int year, int month);

        /// <summary>
        /// 获取某周的所有DDL事件
        /// </summary>
        /// <param name="startDate">周开始日期</param>
        /// <param name="days">天数（默认7天）</param>
        /// <returns>按日期和时间排序的事件列表</returns>
        List<CalendarEvent> GetEventsForWeek(DateTime startDate, int days = 7);

        /// <summary>
        /// 获取今天的DDL事件
        /// </summary>
        /// <returns>按时间排序的事件列表</returns>
        List<CalendarEvent> GetEventsForToday();

        /// <summary>
        /// 获取从今天开始的未来DDL事件（用于周视图）
        /// </summary>
        /// <param name="maxDays">最大天数（默认14天）</param>
        /// <returns>按日期和时间排序的事件列表</returns>
        List<CalendarEvent> GetUpcomingEvents(int maxDays = 14);
    }
}