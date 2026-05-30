using Dalamud.Interface.Textures;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace KingdomHeartsPlugin.Utilities
{
    internal static class ImageDrawing
    {
        internal static ISharedImmediateTexture GetSharedTexture(string path)
        {
            return KingdomHeartsPlugin.Tp.GetFromFile(path);
        }
        private static Vector2 ImRotate(Vector2 v, float cosA, float sinA)
        {
            return new Vector2(v.X * cosA - v.Y * sinA, v.X * sinA + v.Y * cosA);
        }
        public static void DrawImageRotated(ImDrawListPtr d, ISharedImmediateTexture texture, Vector2 position, Vector2 size, float angle, uint col = UInt32.MaxValue)
        {
            var basePosition = ImGui.GetItemRectMin();
            var finalPosition = basePosition + position * KingdomHeartsPlugin.Ui.Configuration.Scale;
            var scaledSize = size * KingdomHeartsPlugin.Ui.Configuration.Scale;


            float cosA = (float)Math.Cos(angle);
            float sinA = (float)Math.Sin(angle);
            Vector2[] pos =
            {
                finalPosition + ImRotate(new Vector2(-scaledSize.X * 0.5f, -scaledSize.Y * 0.5f), cosA, sinA),
                finalPosition + ImRotate(new Vector2(+scaledSize.X * 0.5f, -scaledSize.Y * 0.5f), cosA, sinA),
                finalPosition + ImRotate(new Vector2(+scaledSize.X * 0.5f, +scaledSize.Y * 0.5f), cosA, sinA),
                finalPosition + ImRotate(new Vector2(-scaledSize.X * 0.5f, +scaledSize.Y * 0.5f), cosA, sinA)
            };
            Vector2[] uvs = {
                new(0.0f, 0.0f),
                new(1.0f, 0.0f),
                new(1.0f, 1.0f),
                new(0.0f, 1.0f)
            };

            d.PushClipRect(finalPosition - scaledSize * 2, finalPosition + scaledSize * 2);
            d.AddImageQuad(texture.GetWrapOrEmpty().Handle, pos[0], pos[1], pos[2], pos[3], uvs[0], uvs[1], uvs[2], uvs[3], col);
            d.PopClipRect();
        }

        /// <summary>
        /// Places an image quad
        /// </summary>
        /// <param name="d"></param>
        /// <param name="image"></param>
        /// <param name="ULPos">Upper Left Corner</param>
        /// <param name="URPos">Upper Right Corner</param>
        /// <param name="LRPos">Lower Right Corner</param>
        /// <param name="LLPos">Lower Left Corner</param>
        /// <param name="color"></param>
        public static void DrawImageQuad(ImDrawListPtr d, ISharedImmediateTexture image, Vector2 position, Vector2 ULPos, Vector2 URPos, Vector2 LRPos, Vector2 LLPos, uint color = UInt32.MaxValue)
        {
            var basePosition = ImGui.GetItemRectMin();
            var imageSize = new Vector2(image.GetWrapOrEmpty().Width, image.GetWrapOrEmpty().Height) * KingdomHeartsPlugin.Ui.Configuration.Scale;
            var finalPosition = basePosition + position * KingdomHeartsPlugin.Ui.Configuration.Scale; 
            
            Vector2[] uvs = {
                new(0.0f, 0.0f),
                new(1.0f, 0.0f),
                new(1.0f, 1.0f),
                new(0.0f, 1.0f)
            };

            d.PushClipRect(finalPosition - imageSize * 2, finalPosition + imageSize * 2);
            d.AddImageQuad(image.GetWrapOrEmpty().Handle, finalPosition + ULPos * KingdomHeartsPlugin.Ui.Configuration.Scale, finalPosition + new Vector2(imageSize.X, 0) + URPos * KingdomHeartsPlugin.Ui.Configuration.Scale, finalPosition + imageSize + LRPos * KingdomHeartsPlugin.Ui.Configuration.Scale, finalPosition + new Vector2(0, imageSize.Y) + LLPos * KingdomHeartsPlugin.Ui.Configuration.Scale, uvs[0], uvs[1], uvs[2], uvs[3], color);
            d.PopClipRect();
        }

        /// <summary>
        /// Places an image relative to current draw list
        /// </summary>
        /// <param name="d"></param>
        /// <param name="image"></param>
        /// <param name="position">additive position</param>
        /// <param name="color"></param>
        public static void DrawImage(ImDrawListPtr d, ISharedImmediateTexture image, Vector2 position, uint color = UInt32.MaxValue)
        {
            var basePosition = ImGui.GetItemRectMin();
            var imageSize = new Vector2(image.GetWrapOrEmpty().Width, image.GetWrapOrEmpty().Height) * KingdomHeartsPlugin.Ui.Configuration.Scale;
            var finalPosition = basePosition + position * KingdomHeartsPlugin.Ui.Configuration.Scale;

            d.PushClipRect(finalPosition - imageSize * 2, finalPosition + imageSize * 2);
            d.AddImage(image.GetWrapOrEmpty().Handle, finalPosition, finalPosition + imageSize, new Vector2(0,0), new Vector2(1,1), color);
            d.PopClipRect();
        }

        /// <summary>
        /// Places an image relative to current draw list
        /// </summary>
        /// <param name="d"></param>
        /// <param name="image"></param>
        /// <param name="position">additive position, width and height</param>
        /// <param name="color"></param>
        public static void DrawImage(ImDrawListPtr d, ISharedImmediateTexture image, Vector4 position, uint color = UInt32.MaxValue)
        {
            var basePosition = ImGui.GetItemRectMin();
            var imageSize = new Vector2(position.Z, position.W) * KingdomHeartsPlugin.Ui.Configuration.Scale;
            var finalPosition = basePosition + new Vector2(position.X, position.Y) * KingdomHeartsPlugin.Ui.Configuration.Scale;

            d.PushClipRect(finalPosition - imageSize * 2, finalPosition + imageSize * 2);
            d.AddImage(image.GetWrapOrEmpty().Handle, finalPosition, finalPosition + imageSize, new Vector2(0, 0), new Vector2(1, 1), color);
            d.PopClipRect();
        }

        /// <summary>
        /// Places an image relative to current draw list
        /// </summary>
        /// <param name="d"></param>
        /// <param name="image"></param>
        /// <param name="scale">image scale</param>
        /// <param name="position">additive position</param>
        /// <param name="color">image color</param>
        public static void DrawImage(ImDrawListPtr d, ISharedImmediateTexture image, float scale, Vector2 position, uint color = UInt32.MaxValue)
        {
            var basePosition = ImGui.GetItemRectMin();
            var imageSize = new Vector2(image.GetWrapOrEmpty().Width, image.GetWrapOrEmpty().Height) * KingdomHeartsPlugin.Ui.Configuration.Scale * scale;
            var finalPosition = basePosition + position * KingdomHeartsPlugin.Ui.Configuration.Scale;

            d.PushClipRect(finalPosition - imageSize * 2, finalPosition + imageSize * 2);
            d.AddImage(image.GetWrapOrEmpty().Handle, finalPosition, finalPosition + imageSize, new Vector2(0, 0), new Vector2(1, 1), color);
            d.PopClipRect();
        }

        internal static void DrawIcon(ImDrawListPtr d, uint icon, Vector2 size, Vector2 position)
        {
            var tex = GetIconImage(icon);
            if (tex.GetWrapOrEmpty().Handle != IntPtr.Zero)
            {
                var iconSize = new Vector2(tex.GetWrapOrEmpty().Width, tex.GetWrapOrEmpty().Height) * size;
                var imagePosition = position - new Vector2((int)Math.Floor(iconSize.X / 2f), (int)Math.Floor(iconSize.Y / 2f));
                DrawImage(d, tex, new Vector4(imagePosition.X, imagePosition.Y, iconSize.X, iconSize.Y));
            }
        }

        private static ISharedImmediateTexture GetIconImage(uint icon)
        {
            return KingdomHeartsPlugin.Tp.GetFromGameIcon(new GameIconLookup(icon));
        }
    }
}
