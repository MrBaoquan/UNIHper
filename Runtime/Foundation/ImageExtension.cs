using UnityEngine;
using UnityEngine.UI;

namespace UNIHper
{
    public static class ImageExtension
    {
        public static Image SetSprite(this Image image, Sprite sprite, bool setNativeSize = true)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                if (setNativeSize)
                    image.SetNativeSize();
            }

            return image;
        }

        public static Image SetSprite(this Image image, string spritePath, bool setNativeSize = true)
        {
            var sprite = Managements.Resource.Get<Sprite>(spritePath);
            return image.SetSprite(sprite, setNativeSize);
        }
    }
}
