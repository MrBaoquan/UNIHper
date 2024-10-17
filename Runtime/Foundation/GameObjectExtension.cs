using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UNIHper
{
    public static class GameObjectExtensions
    {
        public static bool Requires(Type obj, Type requirement)
        {
            //also check for m_Type1 and m_Type2 if required
            return Attribute.IsDefined(obj, typeof(RequireComponent))
                && Attribute
                    .GetCustomAttributes(obj, typeof(RequireComponent))
                    .OfType<RequireComponent>()
                    .Any(
                        rc =>
                            new List<Type> { rc.m_Type0, rc.m_Type1, rc.m_Type2 }
                                .Where(rc => rc != null)
                                .Any(rc => rc.IsAssignableFrom(requirement))
                    );
        }

        public static bool CanDestroy(this GameObject go, Type t)
        {
            return !go.GetComponents<Component>().Any(c => Requires(c.GetType(), t));
        }

        public static string GetFullPath(this Transform transform, string separator = "/")
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + separator + path;
            }
            return path;
        }

        public static Rect GetWorldRect(this RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return new Rect(corners[0], corners[2] - corners[0]);
        }
    }
}
