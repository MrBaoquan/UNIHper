using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UNIHper.GhostComponent.Util
{
    public static class GameObjectExtensions
    {
        public static bool Requires(Type obj, Type requirement)
        {
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

        // csharpier-ignore
        public static string GetFullPath(this Transform transform, string separator = "/", Transform stopParent = null)
        {
            string path = transform.name;
            while (transform.parent != null && transform.parent != stopParent)
            {
                transform = transform.parent;
                path = transform.name + separator + path;
            }
            return path;
        }
    }
}
