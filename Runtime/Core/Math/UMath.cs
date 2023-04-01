using System;
using UnityEngine;

namespace UNIHper
{
    public static class UMath
    {
        public static bool InRange(double InVal, double InMin, double InMax)
        {
            return InVal >= InMin && InVal <= InMax;
        }

        /// <summary>
        /// 将2D坐标 映射到 目标尺寸下的坐标
        /// </summary>
        /// <param name="Src"></param>
        /// <param name="SrcSize"></param>
        /// <param name="DstSize"></param>
        /// <param name="bRevertY"></param>
        /// <returns></returns>
        public static Vector2 MapCoordinate2D(
            Vector2 Src,
            Vector2 SrcSize,
            Vector2 DstSize,
            bool bRevertY = false
        )
        {
            float _x = ScaleValue(Src.x, SrcSize.x, DstSize.x);
            float _y = ScaleValue(Src.y, SrcSize.y, DstSize.y);
            if (bRevertY)
            {
                _y = DstSize.y - _y;
            }
            _x = Mathf.Clamp(_x, 0, DstSize.x);
            _y = Mathf.Clamp(_y, 0, DstSize.y);
            return new Vector2(_x, _y);
        }

        //
        public static float ScaleValue(float Input, float Basic, float Scale)
        {
            if (Basic == 0)
            {
                return 0;
            }
            return Input * (Scale / Basic);
        }

        public static bool LineSegmentsIntersection2D(
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            Vector2 p4,
            out Vector2 intersection
        )
        {
            intersection = Vector2.zero;

            var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

            if (d == 0.0f)
            {
                return false;
            }

            var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
            {
                return false;
            }

            intersection.x = p1.x + u * (p2.x - p1.x);
            intersection.y = p1.y + u * (p2.y - p1.y);

            return true;
        }

        public static Vector3 Rotate(
            this Vector3 vector,
            Quaternion rotation,
            Vector3 pivot = default(Vector3)
        )
        {
            return rotation * (vector - pivot) + pivot;
        }

        public static Vector3 Rotate(
            this Vector3 vector,
            Vector3 rotation,
            Vector3 pivot = default(Vector3)
        )
        {
            return Rotate(vector, Quaternion.Euler(rotation), pivot);
        }

        public static Vector3 Rotate(
            this Vector3 vector,
            float x,
            float y,
            float z,
            Vector3 pivot = default(Vector3)
        )
        {
            return Rotate(vector, Quaternion.Euler(x, y, z), pivot);
        }

        public static double FMode(double X, double Y)
        {
            return X - (int)(X / Y) * Y;
        }
    }
}
