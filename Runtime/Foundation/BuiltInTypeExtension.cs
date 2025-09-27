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

        public static Vector4 ToVector4(this Vector2 InVector2)
        {
            return new Vector4(InVector2.x, InVector2.y, 0, 0);
        }

        public static Vector3 Reverse(this Vector3 InVector3)
        {
            return new Vector3(InVector3.z, InVector3.y, InVector3.x);
        }

        public static Vector4 Reverse(this Vector4 InVector4)
        {
            return new Vector4(InVector4.w, InVector4.z, InVector4.y, InVector4.x);
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

    public static class RectTransformExtensions
    {
        /// <summary>
        /// 使RectTransform的左边缘到指定原点的连线的垂直方向，保持RectTransform的旋转。
        /// 适用于UI元素，绕Z轴旋转调整。
        /// </summary>
        /// <param name="rectTransform">目标RectTransform</param>
        /// <param name="originWorldPos">参考原点的世界坐标</param>
        public static void AlignLeftEdgePerpendicularToOrigin(this RectTransform rectTransform, Vector3 originWorldPos)
        {
            if (rectTransform == null)
                return;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            // corners顺序：左下（0）、左上（1）、右上（2）、右下（3）
            Vector3 leftTop = corners[2];
            Vector3 leftBottom = corners[3];

            Vector3 leftEdgeCenter = (leftTop + leftBottom) * 0.5f;

            Vector3 dir = leftEdgeCenter - originWorldPos;
            if (dir.sqrMagnitude < 0.0001f)
                return;
            dir.Normalize();

            // 计算垂直方向（假设Canvas为XY平面，Z轴为正前方向）
            Vector3 perpendicularDir = Vector3.Cross(dir, Vector3.forward);

            float angle = Mathf.Atan2(perpendicularDir.y, perpendicularDir.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
