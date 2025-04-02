using System.IO;
using UnityEngine.UI;
using UnityEngine;
using UniRx;
using System;

namespace UNIHper
{
    public static class BuiltInTypeExtension
    {
        public static Vector2 ToVector2(this Vector3 InVector3)
        {
            return new Vector2(InVector3.x, InVector3.y);
        }

        public static Vector3 ToVector3(this Vector2 InVector2)
        {
            return new Vector3(InVector2.x, InVector2.y, 0);
        }

        public static Vector3 ToVector3(this Vector2 InVector2, float InZ)
        {
            return new Vector3(InVector2.x, InVector2.y, InZ);
        }

        public static DateTime GetClosestValidDate(this DateTime date)
        {
            int year = date.Year;
            int month = date.Month;
            int day = date.Day;

            // 确保年份在合法范围
            year = Math.Max(1, Math.Min(9999, year));

            // 确保月份在合法范围
            month = Math.Max(1, Math.Min(12, month));

            // 获取该年月的最后一天是哪一天
            int lastDayOfMonth = DateTime.DaysInMonth(year, month);

            // 确保日在当月的合法范围内
            day = Math.Max(1, Math.Min(lastDayOfMonth, day));

            // 返回调整后的合法日期
            return new DateTime(year, month, day);
        }

        // 设置年份
        public static DateTime SetYear(this DateTime date, int year)
        {
            return new DateTime(year, date.Month, date.Day);
        }

        // 设置月份，自动调整日
        public static DateTime SetMonth(this DateTime date, int month)
        {
            int lastDayInNewMonth = DateTime.DaysInMonth(date.Year, month);
            int newDay = Math.Min(date.Day, lastDayInNewMonth);
            return new DateTime(date.Year, month, newDay);
        }

        // 设置日
        public static DateTime SetDay(this DateTime date, int day)
        {
            int lastDayInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            int newDay = Math.Min(day, lastDayInMonth);
            return new DateTime(date.Year, date.Month, newDay);
        }
    }

    public static class ExtUtils
    {
        public static bool RandomBool()
        {
            return UnityEngine.Random.Range(0, 2) == 0;
        }
    }
}
