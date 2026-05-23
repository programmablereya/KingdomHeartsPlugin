using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace KingdomHeartsPlugin.Utilities
{
    class ColorAddons
    {
        public static Vector3 Interpolate(Vector3 source, Vector3 target, float percent)
        {
            float r = source.X + (target.X - source.X) * percent;
            float g = source.Y + (target.Y - source.Y) * percent;
            float b = source.Z + (target.Z - source.Z) * percent;

            return new Vector3(r, g, b);
        }

        public static Func<double, uint> Interpolator(uint start, uint end)
        {
            if (start == end)
            {
                return _ => start;
            }
            var startVector = ImGui.ColorConvertU32ToFloat4(start);
            var endVector = ImGui.ColorConvertU32ToFloat4(end);
            return fraction => ImGui.ColorConvertFloat4ToU32(
                startVector * (1f - (float)fraction) + endVector * (float)fraction);
        }
    }
}
