using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using KingdomHeartsPlugin.Enums;
using KingdomHeartsPlugin.UIElements.Experience;
using KingdomHeartsPlugin.UIElements.LimitBreak;
using KingdomHeartsPlugin.UIElements.ParameterResource;
using KingdomHeartsPlugin.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace KingdomHeartsPlugin.UIElements.HealthBar
{
    public class HealthFrame : IDisposable
    {
        #region Sub-displays
        private LimitGauge? _limitGauge;
        private ResourceBar? _resourceBar;
        private ClassBar? _expBar;
        #endregion
        
        #region Temp Health Values
        private uint LastHp { get; set; }
        private uint HpBeforeDamaged { get; set; }
        private uint HpBeforeRestored { get; set; }
        private float HpTemp { get; set; }
        
        private float LastShieldHp { get; set; }
        private float ShieldBeforeLost { get; set; }
        private float ShieldBeforeApplied { get; set; }
        private float ShieldTemp { get; set;  }
        #endregion

        #region Alpha Channels
        public float LostShieldAlpha { get; private set; }
        public float DamagedHealthAlpha { get; private set; }
        public float LowHealthAlpha { get; private set; }
        private int LowHealthAlphaDirection { get; set; }
        #endregion

        #region Timers
        private float HealthRestoreTime { get; set; }
        private float ShieldApplyTime { get; set; }
        private readonly Stopwatch _sinceLastLog;
        private const uint LogDebounceTimeMs = 10000;
        #endregion

        #region Positioning
        private float _verticalAnimationTicks;
        private float HealthY { get; set; }
        private float HealthVerticalSpeed { get; set; }
        #endregion

        #region Constants - Sizes and Colors
        private static readonly Vector2 RingCenterOffset = new(128.0f);

        private const float RingBackgroundRadius = 80f;
        private static readonly Vector3 RingBackgroundColor = new(0.25f, 0.25f, 0.25f);
        private const float RingBackgroundAlpha = 0.68f;
        private static readonly uint RingBackgroundColorHealthy =
            ImGui.GetColorU32(new Vector4(RingBackgroundColor, RingBackgroundAlpha));
        private static readonly Vector3 RingBackgroundDanger = new(1.0f, 0.0f, 0.0f);
        
        private const float RingTrackRadius = 106f;
        private const float RingTrackThickness = 5.0f;
        private static readonly uint RingTrackColor = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));

        private const float HpOuterRadius = 125f;
        private const float HpInnerRadius = 88f;
        // 180 degrees - starting from the left, facing up
        private const double HpStartAngle = Math.PI;
        // -90 degrees - ending south, facing west
        private const double HpEndAngle = -Math.PI / 2;

        private const float ShieldThickness = 11.0f;
        
        private static readonly Vector3 HpBackgroundColor = new(0.07843f, 0.07843f, 0.0745f);
        private static readonly Vector3 HpBackgroundDamaged = new(1.0f, 0.0f, 0.0f);
        private const float HpBackgroundAlpha = 1.0f;
        
        private static readonly Vector3 HpDamageColor = new (1.0f, 0.0f, 0.0f);
        
        private static readonly uint HpRecoveryColorInner = ImGui.GetColorU32(new Vector4(0.0f, 0.73f, 1.0f, 1.0f));
        private static readonly uint HpRecoveryColorOuter = ImGui.GetColorU32(new Vector4(0.0f, 0.51f, 1.0f, 1.0f));
        
        private static readonly uint HpColorInner =
            ImGui.GetColorU32(new Vector4(167f / 255f, 255f / 255f, 1f / 255f, 255f / 255f));
        private static readonly uint HpColorOuter =
            ImGui.GetColorU32(new Vector4(67f / 255f, 184f / 255f, 1f / 255f, 255f / 255f));
        
        private const float ShieldInnerAlpha = 0.85f;
        
        private static readonly uint ShieldApplyColorInner =
            ImGui.GetColorU32(new Vector4(0.89f, 0.86f, 0.60f, ShieldInnerAlpha));
        private static readonly uint ShieldApplyColorOuter =
            ImGui.GetColorU32(new Vector4(0.89f, 0.82f, 0.59f, 1.0f));
        
        private static readonly Vector3 ShieldLostColorInner = new(0.34f, 0.33f, 0.24f);
        private static readonly Vector3 ShieldLostColorOuter = new(0.33f, 0.30f, 0.23f);
        
        private static readonly uint ShieldColorInner =
            ImGui.GetColorU32(new Vector4(0.98f, 0.88f, 0.0f, ShieldInnerAlpha));
        private static readonly uint ShieldColorOuter =
            ImGui.GetColorU32(new Vector4(0.96f, 0.72f, 0.0f, 1.0f));
        
        private static readonly uint HpOutlineColor = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 1f));
        private const float HpOutlineThickness = 5.0f;
        #endregion
        

        public HealthFrame()
        {
            HealthY = 0;
            _verticalAnimationTicks = 0;
            HealthVerticalSpeed = 0f;
            LowHealthAlpha = 0;
            LowHealthAlphaDirection = 0;
            
            _limitGauge = new LimitGauge();
            _resourceBar = new ResourceBar();
            _expBar = new ClassBar();
            _sinceLastLog = new Stopwatch();
        }

        public unsafe void Draw()
        {
            var player = KingdomHeartsPlugin.Ot.LocalPlayer;
            var parameterWidget = (AtkUnitBase*) KingdomHeartsPlugin.Gui.GetAddonByName("_ParameterWidget", 1).Address;

            if (parameterWidget != null)
            {
                // Do not do or draw anything if the parameter widget is not visible
                if (!parameterWidget->IsVisible)
                {
                    return;
                }
            }

            // Do not do or draw anything if player is null or game ui is hidden
            if (player is null || KingdomHeartsPlugin.Gui.GameUiHidden)
            {
                return;
            }

            var drawList = ImGui.GetWindowDrawList();

            if (ImGui.GetDrawListSharedData().IsNull) return;

            ImGui.Dummy(new Vector2(220, 256));

            if (KingdomHeartsPlugin.Ui.Configuration.HpBarEnabled)
            {
                var approximateShieldHp = Math.Min(player.MaxHp, player.ShieldPercentage * player.MaxHp / 100f);
                UpdateHealth(player, approximateShieldHp);
                DrawHealth(drawList, player.CurrentHp, approximateShieldHp, player.MaxHp);
            }

            if (KingdomHeartsPlugin.Ui.Configuration.ResourceBarEnabled) _resourceBar?.Draw(player);
            if (KingdomHeartsPlugin.Ui.Configuration.LimitBarEnabled) _limitGauge?.Draw();
            _expBar?.Draw(player, HealthY * KingdomHeartsPlugin.Ui.Configuration.HpDamageWobbleIntensity / 100f);

            if (KingdomHeartsPlugin.Ui.Configuration.ShowHpVal && KingdomHeartsPlugin.Ui.Configuration.HpBarEnabled)
            {
                // Draw HP Value
                var basePosition = ImGui.GetItemRectMin() + new Vector2(KingdomHeartsPlugin.Ui.Configuration.HpValueTextPositionX, KingdomHeartsPlugin.Ui.Configuration.HpValueTextPositionY) * KingdomHeartsPlugin.Ui.Configuration.Scale;
                
                ImGuiAdditions.TextShadowedDrawList(drawList,
                    KingdomHeartsPlugin.Ui.Configuration.HpValueTextSize,
                    $"{StringFormatting.FormatDigits(player.CurrentHp, (NumberFormatStyle)KingdomHeartsPlugin.Ui.Configuration.HpValueTextStyle)}",
                    basePosition,
                    new Vector4(255 / 255f, 255 / 255f, 255 / 255f, 1f),
                    new Vector4(0 / 255f, 0 / 255f, 0 / 255f, 0.25f), 3, (TextAlignment)KingdomHeartsPlugin.Ui.Configuration.HpValueTextAlignment);
            }
        }

        private bool CanLogNow()
        {
            if (_sinceLastLog is { IsRunning: true, ElapsedMilliseconds: < LogDebounceTimeMs }) return false;
            
            _sinceLastLog.Restart();
            return true;
        }
        
        private void UpdateHealth(IPlayerCharacter player, float approximateShieldHp)
        {
            if (LastHp > player.CurrentHp && LastHp <= player.MaxHp)
                DamagedHealth(LastHp);
            if (LastShieldHp > approximateShieldHp)
                LostShield(LastShieldHp);
            if (LastHp < player.CurrentHp)
                RestoredHealth(LastHp);
            if (LastShieldHp < approximateShieldHp)
                AppliedShield(LastShieldHp);

            UpdateLowHealth(player.CurrentHp, player.MaxHp);
            
            UpdateDamagedHealth();
            UpdateLostShield();

            UpdateRestoredHealth(player.CurrentHp);
            UpdateAppliedShield(approximateShieldHp);

            if (HpBeforeDamaged > player.MaxHp)
                HpBeforeDamaged = player.MaxHp;

            LastHp = player.CurrentHp;
            LastShieldHp = approximateShieldHp;
        }

        private void DamagedHealth(uint health)
        {
            DamagedHealthAlpha = 1f;
            HealthY = 0;
            HealthVerticalSpeed = -3;
            HpBeforeDamaged = health;
        }
        
        private void LostShield(float shield)
        {
            LostShieldAlpha = 1f;
            ShieldBeforeLost = shield;
        }

        private void RestoredHealth(uint health)
        {
            if (HealthRestoreTime <= 0)
            {
                HpTemp = health;
                HpBeforeRestored = health;
            }

            HealthRestoreTime = 1f;
        }
        
        private void AppliedShield(float shield)
        {
            if (ShieldApplyTime <= 0)
            {
                ShieldTemp = shield;
                ShieldBeforeApplied = shield;
            }

            ShieldApplyTime = 0.1f;
        }

        private void UpdateRestoredHealth(uint currentHp)
        {
            if (HealthRestoreTime > 0)
            {
                HealthRestoreTime -= 1 * KingdomHeartsPlugin.UiSpeed;
            }
            else if (HpTemp < currentHp)
            {
                HpTemp += (currentHp - HpBeforeRestored) * KingdomHeartsPlugin.UiSpeed;
                if (HpBeforeRestored > currentHp)
                    HpBeforeRestored = currentHp;
            }

            if (HpTemp > currentHp)
                HpTemp = currentHp;
        }
        
        private void UpdateAppliedShield(float currentShield)
        {
            if (ShieldApplyTime > 0)
            {
                ShieldApplyTime -= 1 * KingdomHeartsPlugin.UiSpeed;
            }
            else if (ShieldTemp < currentShield)
            {
                ShieldTemp += (currentShield - ShieldBeforeApplied) * KingdomHeartsPlugin.UiSpeed * 5;
                if (ShieldBeforeApplied > currentShield)
                    ShieldBeforeApplied = currentShield;
            }

            if (ShieldTemp > currentShield)
                ShieldTemp = currentShield;
        }

        private void UpdateLostShield()
        {
            switch (LostShieldAlpha)
            {
                case > 0.97f:
                    LostShieldAlpha -= 0.45f * KingdomHeartsPlugin.UiSpeed;
                    break;
                case > 0.6f:
                    LostShieldAlpha -= 4.0f * KingdomHeartsPlugin.UiSpeed;
                    break;
                case > 0.59f:
                    LostShieldAlpha -= 0.025f * KingdomHeartsPlugin.UiSpeed;
                    break;
                case > 0.0f:
                    LostShieldAlpha -= 5f * KingdomHeartsPlugin.UiSpeed;
                    break;
            }
        }
        
        private void UpdateDamagedHealth()
        {
            switch (DamagedHealthAlpha)
            {
                case > 0.97f:
                    DamagedHealthAlpha -= 0.09f * KingdomHeartsPlugin.UiSpeed;
                    break;
                case > 0.6f:
                    DamagedHealthAlpha -= 0.8f * KingdomHeartsPlugin.UiSpeed;
                    break;
                case > 0.59f:
                    DamagedHealthAlpha -= 0.005f * KingdomHeartsPlugin.UiSpeed;
                    break;
                case > 0.0f:
                    DamagedHealthAlpha -= 1f * KingdomHeartsPlugin.UiSpeed;
                    break;
            }

            // Vertical wobble
            _verticalAnimationTicks += 240 * KingdomHeartsPlugin.UiSpeed;

            while (_verticalAnimationTicks > 1)
            {
                float intensity = KingdomHeartsPlugin.Ui.Configuration.HpDamageWobbleIntensity / 100f;
                _verticalAnimationTicks--;
                HealthY += HealthVerticalSpeed;

                if (HealthY > 3)
                {
                    HealthVerticalSpeed -= 0.2f;
                }

                else if (HealthY < -3)
                {
                    HealthVerticalSpeed += 0.2f;
                }
                else if (HealthY is > -3 and < 3 && HealthVerticalSpeed is > -0.33f and < 0.33f)
                {
                    HealthVerticalSpeed = 0;
                    HealthY = 0;
                }
                else if (HealthVerticalSpeed != 0)
                {
                    HealthVerticalSpeed *= 0.94f;
                }
            }
        }

        private void UpdateLowHealth(uint health, uint maxHealth)
        {
            if ((health > maxHealth * (KingdomHeartsPlugin.Ui.Configuration.LowHpPercent / 100f) || health <= 0) && LowHealthAlpha <= 0) return;

            if (LowHealthAlphaDirection == 0)
            {
                LowHealthAlpha += 1.6f * KingdomHeartsPlugin.UiSpeed;

                if (LowHealthAlpha >= .4)
                    LowHealthAlphaDirection = 1;
            }
            else
            {
                LowHealthAlpha -= 1.6f * KingdomHeartsPlugin.UiSpeed;

                if (LowHealthAlpha <= 0)
                    LowHealthAlphaDirection = 0;
            }
        }
        
        private void DrawHealth(ImDrawListPtr drawList, uint hp, float shieldHpApproximate, uint maxHp)
        {
            int fullRingSizeHp, minimumSizeHp, maximumSizeHp;
            float hpPerPixel;
            if (KingdomHeartsPlugin.IsInPvp)
            {
                fullRingSizeHp = KingdomHeartsPlugin.Ui.Configuration.PvpHpForFullRing;
                minimumSizeHp = KingdomHeartsPlugin.Ui.Configuration.PvpMinimumHpForLength;
                maximumSizeHp = KingdomHeartsPlugin.Ui.Configuration.PvpMaximumHpForMaximumLength;
                hpPerPixel = KingdomHeartsPlugin.Ui.Configuration.PvpHpPerPixelLongBar;
            }
            else
            {
                fullRingSizeHp = KingdomHeartsPlugin.Ui.Configuration.HpForFullRing;
                minimumSizeHp = KingdomHeartsPlugin.Ui.Configuration.MinimumHpForLength;
                maximumSizeHp = KingdomHeartsPlugin.Ui.Configuration.MaximumHpForMaximumLength;
                hpPerPixel = KingdomHeartsPlugin.Ui.Configuration.HpPerPixelLongBar;
            }

            var maxBarLength = Math.Max(0f, maximumSizeHp - fullRingSizeHp) / hpPerPixel;
            
            var scale = KingdomHeartsPlugin.Ui.Configuration.Scale;
            var drawPosition = ImGui.GetItemRectMin() + new Vector2(
                0, HealthY * KingdomHeartsPlugin.Ui.Configuration.HpDamageWobbleIntensity / 100f * scale);

            DrawRingBackgroundAndTrack(drawList, drawPosition);
            var baseRing = RingGauge.EmptyRingGauge;
            var ringResolution = 0;
            var ringPosition = drawPosition + RingCenterOffset;
            try
            {
                ringResolution =
                    drawList._CalcCircleAutoSegmentCount((HpOuterRadius + HpOutlineThickness) * scale);
                baseRing = RingGauge.Construct(
                    ringResolution,
                    HpOuterRadius,
                    HpInnerRadius,
                    HpStartAngle,
                    HpEndAngle,
                    maxBarLength
                );
            }
            catch (Exception ex)
            {
                if (CanLogNow())
                {
                    KingdomHeartsPlugin.Pl.Error(ex,
                        "While calculating baseRing (ringResolution {ringResolution}, scale {scale})",
                        ringResolution, scale);
                }
            }

            var maxRing = RingGauge.EmptyRingGauge;
            try
            {
                var maxhpFraction = MaxHpFraction(
                    maxHp,
                    maximumSizeHp,
                    minimumSizeHp,
                    fullRingSizeHp,
                    baseRing.SectorFraction,
                    baseRing.BarFraction);
                maxRing = baseRing.GetSubset(maxhpFraction);
            }
            catch (Exception ex)
            {
                if (CanLogNow())
                {
                    KingdomHeartsPlugin.Pl.Error(ex,
                        "While slicing maxRing (maxHp {maxHp}, maximumSizeHp {maximumSizeHp}," +
                        "minimumSizeHp {minimumSizeHp}, fullRingSizeHp {fullRingSizeHp}," +
                        "sectorFraction {sectorFraction}, barFraction {barFraction})",
                        maxHp, maximumSizeHp, minimumSizeHp, fullRingSizeHp, baseRing.SectorFraction,
                        baseRing.BarFraction);
                }
            }
            
            try
            {
                var hpBackgroundColor =
                    ColorAddons.Interpolate(HpBackgroundColor, HpBackgroundDamaged, LowHealthAlpha);
                maxRing.FillFlat(drawList, ringPosition, ImGui.GetColorU32(new Vector4(hpBackgroundColor, HpBackgroundAlpha)), scale);
            }
            catch (Exception ex)
            {
                if (CanLogNow())
                {
                    KingdomHeartsPlugin.Pl.Error(ex, "While filling maxRing");
                }
            }
            
            var hpFraction = (double forHp) => forHp / maxHp;

            if (DamagedHealthAlpha > 0 && HpBeforeDamaged > hp)
            {
                try
                {
                    var damageRing = maxRing.GetSubset(hpFraction(hp), hpFraction(HpBeforeDamaged));
                    damageRing.FillFlat(drawList, ringPosition, ImGui.GetColorU32(new Vector4(HpDamageColor, DamagedHealthAlpha)), scale);
                }
                catch (Exception ex)
                {
                    if (CanLogNow())
                    {
                        KingdomHeartsPlugin.Pl.Error(ex,
                            "While rendering damageRing (hp {hp}, maxHp {maxHp}, HpBeforeDamaged {HpBeforeDamaged})",
                            hp, maxHp, HpBeforeDamaged);
                    }
                }
            }

            var effectiveCurrentHp = (float) hp;
            if (KingdomHeartsPlugin.Ui.Configuration.ShowHpRecovery && HpTemp < hp)
            {
                try
                {
                    effectiveCurrentHp = HpTemp;
                    var recoveryRing = maxRing.GetSubset(hpFraction(effectiveCurrentHp), hpFraction(hp));
                    recoveryRing.FillInnerToOuter(drawList, ringPosition, HpRecoveryColorInner, HpRecoveryColorOuter, scale);
                }
                catch (Exception ex)
                {
                    if (CanLogNow())
                    {
                        KingdomHeartsPlugin.Pl.Error(ex,
                            "While rendering recoveryRing (effectiveCurrentHp {effectiveCurrentHp}, hp {hp}, maxHp {maxHp})",
                            effectiveCurrentHp, hp, maxHp);
                    }
                }
            }

            try
            {
                var currentRing = maxRing.GetSubset(hpFraction(effectiveCurrentHp));
                currentRing.FillInnerToOuter(drawList, ringPosition, HpColorInner, HpColorOuter, scale);
            }
            catch (Exception ex)
            {
                if (CanLogNow())
                {
                    KingdomHeartsPlugin.Pl.Error(ex,
                        "While rendering currentRing (effectiveCurrentHp {effectiveCurrentHp}, maxHp {maxHp})",
                        effectiveCurrentHp, maxHp);
                }
            }
            
            var hasLostShield = LostShieldAlpha > 0 && ShieldBeforeLost > shieldHpApproximate;
            var hasAppliedShield =
                KingdomHeartsPlugin.Ui.Configuration.ShowHpRecovery && ShieldTemp < shieldHpApproximate;
            var effectiveCurrentShield = hasAppliedShield ? ShieldTemp : shieldHpApproximate;
            var hasShield = hasLostShield || hasAppliedShield || effectiveCurrentShield > 0;
            var maxShieldRing = hasShield
                ? maxRing.AdjustThickness(ShieldThickness - (HpOuterRadius - HpInnerRadius)) 
                : RingGauge.EmptyRingGauge;
            
            if (hasLostShield)
            {
                try
                {
                    var lostShieldRing =
                        maxShieldRing.GetSubset(hpFraction(shieldHpApproximate), hpFraction(ShieldBeforeLost));
                    lostShieldRing.FillInnerToOuter(drawList,
                        ringPosition,
                        ImGui.GetColorU32(new Vector4(ShieldLostColorInner, LostShieldAlpha * ShieldInnerAlpha)),
                        ImGui.GetColorU32(new Vector4(ShieldLostColorOuter, LostShieldAlpha)), scale);
                }
                catch (Exception ex)
                {
                    if (CanLogNow())
                    {
                        KingdomHeartsPlugin.Pl.Error(ex,
                            "While rendering lostShieldRing (shieldHpApproximate {shieldHpApproximate}, maxHp {maxHp}, ShieldBeforeLost {ShieldBeforeLost})",
                            shieldHpApproximate, maxHp, ShieldBeforeLost);
                    }
                }
            }
            
            if (hasAppliedShield)
            {
                try
                {
                    var recoveryShieldRing = maxShieldRing.GetSubset(hpFraction(effectiveCurrentShield), hpFraction(shieldHpApproximate));
                    recoveryShieldRing.FillInnerToOuter(drawList, ringPosition, ShieldApplyColorInner, ShieldApplyColorOuter, scale);
                }
                catch (Exception ex)
                {
                    if (CanLogNow())
                    {
                        KingdomHeartsPlugin.Pl.Error(ex,
                            "While rendering recoveryShieldRing (effectiveCurrentShield {effectiveCurrentShield}, hp {shieldHpApproximate}, maxHp {maxHp})",
                            effectiveCurrentShield, shieldHpApproximate, maxHp);
                    }
                }
            }
            
            try
            {
                var currentShieldRing = maxShieldRing.GetSubset(hpFraction(effectiveCurrentShield));
                currentShieldRing.FillInnerToOuter(drawList, ringPosition, ShieldColorInner, ShieldColorOuter, scale);
            }
            catch (Exception ex)
            {
                if (CanLogNow())
                {
                    KingdomHeartsPlugin.Pl.Error(ex,
                        "While rendering currentShieldRing (effectiveCurrentShield {effectiveCurrentShield}, maxHp {maxHp})",
                        effectiveCurrentShield, maxHp);
                }
            }

            try
            {
                maxRing.Stroke(drawList, ringPosition, HpOutlineColor, ImDrawFlags.None, HpOutlineThickness * scale, scale);
            }
            catch (Exception ex)
            {
                if (CanLogNow())
                {
                    KingdomHeartsPlugin.Pl.Error(ex, "While stroking maxRing");
                }
            }
        }

        private static double MaxHpFraction(uint maxHp, int maximumSizeHp, int minimumSizeHp, int fullRingSizeHp,
            double sectorFraction, double barFraction)
        {
            // Use Min and Max instead of Clamp because we can't trust that the user didn't put them backwards.
            // The resulting gauge will be smaller than the minimum, but that's the user's fault.
            // We can always go smaller - can't always go bigger.
            var effectiveMaxHp = Math.Min(maximumSizeHp, Math.Max(minimumSizeHp, maxHp));
            if (effectiveMaxHp <= fullRingSizeHp)
            {
                return sectorFraction * effectiveMaxHp / fullRingSizeHp;
            }
            else
            {
                return sectorFraction 
                                + barFraction
                                * (effectiveMaxHp - fullRingSizeHp)
                                / (maximumSizeHp - fullRingSizeHp);
            }
        }

        private void DrawRingBackgroundAndTrack(ImDrawListPtr drawList, Vector2 position)
        {
            var scale = KingdomHeartsPlugin.Ui.Configuration.Scale;
            var center = position + RingCenterOffset * scale;
            
            var ringBackgroundSize = new Vector2(RingBackgroundRadius * scale * 2);
            drawList.PushClipRect(center - ringBackgroundSize / 2, center + ringBackgroundSize / 2);
            drawList.AddCircleFilled(center, RingBackgroundRadius, 
                LowHealthAlpha > 0
                    ? ImGui.GetColorU32(new Vector4(
                        ColorAddons.Interpolate(RingBackgroundColor, RingBackgroundDanger, LowHealthAlpha),
                        RingBackgroundAlpha))
                    : RingBackgroundColorHealthy);
            drawList.PopClipRect();
            
            var ringTrackSize = new Vector2(RingTrackRadius * 2 + RingTrackThickness);
            drawList.PushClipRect(center - ringTrackSize / 2, center + ringTrackSize / 2);
            drawList.AddCircle(center, RingTrackRadius, RingTrackColor, RingTrackThickness);
            drawList.PopClipRect();
        }

        public void Dispose()
        {
            _limitGauge?.Dispose();
            _resourceBar?.Dispose();
            _expBar?.Dispose();

            _limitGauge = null;
            _resourceBar = null;
            _expBar = null;
        }
    }
}
