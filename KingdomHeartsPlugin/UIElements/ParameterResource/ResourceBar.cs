using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Enums;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.UIElements.ParameterResource
{
    public class ResourceBar
    {
        private enum Resource
        {
            Mp,
            Cp,
            Gp
        }

        public void Update(IPlayerCharacter player)
        {
            var minLength = 1;
            var maxLength = 1;
            var lengthRate = 1f;
            var lowPercent = 0f;

            if (player.MaxMp > 0)
            {
                ResourceValue = player.CurrentMp;
                ResourceMax = player.MaxMp;
                ResourceType = Resource.Mp;

                minLength = KingdomHeartsPlugin.Ui.Configuration.MinimumMpLength;
                maxLength = KingdomHeartsPlugin.Ui.Configuration.MaximumMpLength;
                lengthRate = KingdomHeartsPlugin.Ui.Configuration.MpPerPixelLength;
                lowPercent = KingdomHeartsPlugin.Ui.Configuration.LowMpPercent;
            }
            else if (player.MaxCp > 0)
            {
                ResourceValue = player.CurrentCp;
                ResourceMax = player.MaxCp;
                ResourceType = Resource.Cp;

                minLength = KingdomHeartsPlugin.Ui.Configuration.MinimumCpLength;
                maxLength = KingdomHeartsPlugin.Ui.Configuration.MaximumCpLength;
                lengthRate = KingdomHeartsPlugin.Ui.Configuration.CpPerPixelLength;
                lowPercent = KingdomHeartsPlugin.Ui.Configuration.LowCpPercent;
            }
            else if (player.MaxGp > 0)
            {
                ResourceValue = player.CurrentGp;
                ResourceMax = player.MaxGp;
                ResourceType = Resource.Gp;

                minLength = KingdomHeartsPlugin.Ui.Configuration.MinimumGpLength;
                maxLength = KingdomHeartsPlugin.Ui.Configuration.MaximumGpLength;
                lengthRate = KingdomHeartsPlugin.Ui.Configuration.GpPerPixelLength;
                lowPercent = KingdomHeartsPlugin.Ui.Configuration.LowGpPercent;
            }

            MaxResourceLength = (ResourceMax < minLength ? minLength : ResourceMax > maxLength ? maxLength : ResourceMax) / lengthRate;
            ResourceLow = ResourceValue <= ResourceMax * lowPercent / 100.0f;
            IsConscious = !player.IsDead;
        }

        public void Draw(IPlayerCharacter player)
        {
            Update(player);
            var drawList = ImGui.GetWindowDrawList();
            var basePosition = new Vector2(KingdomHeartsPlugin.Ui.Configuration.ResourceBarPositionX, KingdomHeartsPlugin.Ui.Configuration.ResourceBarPositionY);
            var textPosition = new Vector2(KingdomHeartsPlugin.Ui.Configuration.ResourceTextPositionX, KingdomHeartsPlugin.Ui.Configuration.ResourceTextPositionY) * KingdomHeartsPlugin.Ui.Configuration.Scale;
            var scale = KingdomHeartsPlugin.Ui.Configuration.Scale;
            var origin = ImGui.GetItemRectMin() + basePosition * scale;

            var foregroundUpperColor = NormalForegroundUpperColor;
            var foregroundLowerColor = NormalForegroundLowerColor;
            var backgroundColor = NormalBackgroundColor;
            var textColor = NormalTitleColor;
            var textShadowColor = NormalTitleShadowColor;
            
            if (ResourceLow)
            {
                FlashCycleTime = (FlashCycleTime + KingdomHeartsPlugin.UiSpeed) % FlashCycleDurationSec;
                if (FlashCycleTime >= FlashCycleDurationSec * FlashDutyCycle || !IsConscious)
                {
                    // we are currently Off
                    foregroundUpperColor = FlashOffForegroundUpperColor;
                    foregroundLowerColor = FlashOffForegroundLowerColor;
                    backgroundColor = FlashOffBackgroundColor;
                    textColor = FlashOffTitleColor;
                    textShadowColor = FlashOffTitleShadowColor;
                }
                else
                {
                    // we are currently On
                    foregroundUpperColor = FlashOnForegroundUpperColor;
                    foregroundLowerColor = FlashOnForegroundLowerColor;
                    backgroundColor = FlashOnBackgroundColor;
                    textColor = FlashOnTitleColor;
                    textShadowColor = FlashOnTitleShadowColor;
                }
            }
            else
            {
                FlashCycleTime = 0.0f;
            }

            // Frame
            var barOuterStart = origin + new Vector2(0.65f - MaxResourceLength - 6.0f, 0.0f) * scale;
            var barOuterEnd = origin + new Vector2(ResourceTitleWidth + 12.0f, 32.0f) * scale;
            drawList.PushClipRect(barOuterStart, barOuterEnd);
            drawList.AddRectFilled(
                barOuterStart,
                barOuterEnd,
                FrameColor,
                5.0f * scale,
                ImDrawFlags.RoundCornersAll);
            drawList.PopClipRect();
            
            // Text
            DrawResourceTitle(drawList, origin + new Vector2(3.0f, 4.0f) * scale, scale, textColor, textShadowColor);
            
            // BG
            var barInnerStart = origin + new Vector2(0.33f - MaxResourceLength, 5.0f) * scale;
            var barInnerEnd = origin + new Vector2(0.33f, 5.0f + 22.0f) * scale;
            drawList.PushClipRect(barInnerStart, barInnerEnd);
            drawList.AddRectFilled(barInnerStart, barInnerEnd, backgroundColor);
            drawList.PopClipRect();

            // FG
            if (ResourceValue > 0)
            {
                var barValueStart = barInnerStart +
                                    new Vector2((barInnerEnd.X - barInnerStart.X) * (ResourceMax - ResourceValue) / ResourceMax, 0.0f) * scale;
                drawList.PushClipRect(barValueStart, barInnerEnd);
                drawList.AddRectFilledMultiColor(
                    barValueStart,
                    barInnerEnd,
                    foregroundUpperColor,
                    foregroundUpperColor,
                    foregroundLowerColor,
                    foregroundLowerColor);
                drawList.PopClipRect();
            }
            
            if (KingdomHeartsPlugin.Ui.Configuration.ShowResourceVal)
                ImGuiAdditions.TextShadowedDrawList(drawList,
                    KingdomHeartsPlugin.Ui.Configuration.ResourceTextSize,
                    $"{StringFormatting.FormatDigits(
                        KingdomHeartsPlugin.Ui.Configuration.TruncateMp && ResourceType == Resource.Mp
                            ? ResourceValue / MpTruncateMultiplier
                            : ResourceValue,
                        KingdomHeartsPlugin.Ui.Configuration.ResourceTextStyle)}",
                    origin + textPosition,
                   ValueColor, ValueShadowColor, ValueShadowWidth,
                    (TextAlignment)KingdomHeartsPlugin.Ui.Configuration.ResourceTextAlignment);
        }
        
        public FlatMesh ResourceCharacterMesh => ResourceType switch {
            Resource.Mp => StylizedText.ResourceM,
            Resource.Cp => StylizedText.ResourceC,
            Resource.Gp => StylizedText.ResourceG,
            _ => StylizedText.ResourceM
        };

        public float ResourceTitleWidth => ResourceCharacterMesh.Size.X + StylizedText.ResourceCharacterSpacing +
                                           StylizedText.ResourceP.Size.X;
        
        public void DrawResourceTitle(ImDrawListPtr drawList, Vector2 origin, float scale, uint color, uint shadowColor)
        {
            var shadowOffset = new Vector2(2.0f) * scale;

            var typeCharacterPosition = origin + shadowOffset * scale;
            var pPosition = typeCharacterPosition
                            + new Vector2((ResourceCharacterMesh.Size.X + StylizedText.ResourceCharacterSpacing) * scale, 0.0f);
            
            ResourceCharacterMesh.Fill(drawList, typeCharacterPosition + shadowOffset, scale, shadowColor);
            StylizedText.ResourceP.Fill(drawList, pPosition + shadowOffset, scale, shadowColor);
            ResourceCharacterMesh.Fill(drawList, typeCharacterPosition, scale, color);
            StylizedText.ResourceP.Fill(drawList, pPosition, scale, color);
        }

        
        public void Dispose()
        {
        }
        
        #region Colors
        private static readonly uint FrameColor = ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        private static readonly Vector4 ValueColor = new Vector4(255 / 255f, 255 / 255f, 255 / 255f, 1f);
        private static readonly Vector4 ValueShadowColor = new Vector4(0 / 255f, 0 / 255f, 0 / 255f, 0.25f);
        private const byte ValueShadowWidth = 3;
        private const uint MpTruncateMultiplier = 100;

        private static readonly uint NormalForegroundUpperColor = ImGui.GetColorU32(new Vector4(0.0f, 0.47f, 0.94f, 1.0f));
        private static readonly uint NormalForegroundLowerColor = ImGui.GetColorU32(new Vector4(0.0f, 0.30f, 0.82f, 1.0f));
        private static readonly uint NormalBackgroundColor = ImGui.GetColorU32(new Vector4(0.02f, 0.04f, 0.33f, 1.0f));
        private static readonly uint NormalTitleColor = ImGui.GetColorU32(new Vector4(0.30f, 0.51f, 0.78f, 1.0f));
        private static readonly uint NormalTitleShadowColor = ImGui.GetColorU32(new Vector4(0.18f, 0.30f, 0.48f, 1.0f));
        
        private static readonly uint FlashOnForegroundUpperColor = ImGui.GetColorU32(new Vector4(0.92f, 0.22f, 0.90f, 1.0f));
        private static readonly uint FlashOnForegroundLowerColor = ImGui.GetColorU32(new Vector4(0.68f, 0.13f, 0.68f, 1.0f));
        private static readonly uint FlashOnBackgroundColor = ImGui.GetColorU32(new Vector4(0.32f, 0.02f, 0.04f, 1.0f));
        private static readonly uint FlashOnTitleColor = ImGui.GetColorU32(new Vector4(0.95f, 0.69f, 0.73f, 1.0f));
        private static readonly uint FlashOnTitleShadowColor = ImGui.GetColorU32(new Vector4(0.38f, 0.18f, 0.20f, 1.0f));
        
        private static readonly uint FlashOffForegroundUpperColor = ImGui.GetColorU32(new Vector4(0.66f, 0.12f, 0.53f, 1.0f));
        private static readonly uint FlashOffForegroundLowerColor = ImGui.GetColorU32(new Vector4(0.36f, 0.11f, 0.32f, 1.0f));
        private static readonly uint FlashOffBackgroundColor = ImGui.GetColorU32(new Vector4(0.35f, 0.01f, 0.08f, 1.0f));
        private static readonly uint FlashOffTitleColor = ImGui.GetColorU32(new Vector4(0.75f, 0.36f, 0.42f, 1.0f));
        private static readonly uint FlashOffTitleShadowColor = ImGui.GetColorU32(new Vector4(0.31f, 0.08f, 0.11f, 1.0f));
        #endregion

        #region Flash Timing
        private bool ResourceLow { get; set; }
        private bool IsConscious { get; set; }
        private float FlashCycleTime { get; set; }
        private const float FlashCycleDurationSec = 1.0f;
        private const float FlashDutyCycle = 0.5f;
        #endregion
        
        private uint ResourceValue { get; set; }
        private Resource ResourceType { get; set; }
        private uint ResourceMax { get; set; }
        private float MaxResourceLength { get; set; }
    }
}
