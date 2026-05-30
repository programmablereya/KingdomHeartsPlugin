using Dalamud.Bindings.ImGui;

namespace KingdomHeartsPlugin.Test.Testing;

/// <summary>
/// Initializes ImGui to the level necessary for use of ImGui functions, which are used by the plugin.
/// </summary>
public static class ImGuiInitializer
{
    /// <summary>
    /// Initializes ImGui before each testing session.
    /// </summary>
    [Before(TestSession)]
    public static void InitializeImGui()
    {
        ImGui.CreateContext();
    }

    /// <summary>
    /// Deinitializes ImGui at the conclusion of each testing session.
    /// </summary>
    [After(TestSession)]
    public static void CleanupImGui()
    {
        ImGui.DestroyContext();
    }
}