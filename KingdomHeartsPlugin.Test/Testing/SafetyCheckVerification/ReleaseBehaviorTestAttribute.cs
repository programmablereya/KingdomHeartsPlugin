namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification;

/// <summary>
/// Attribute marking a test as only running in release mode.
/// </summary>
public class ReleaseBehaviorTestAttribute() : SkipAttribute("This test verifies release behavior and is not supported in Debug mode.")
{
    /// <summary>
    /// Marks this test as skipped if we are running in debug mode.
    /// </summary>
    /// <param name="context">The context of the test registration.</param>
    /// <returns>True if this test should be skipped because we are in debug mode, false if not.</returns>
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
#if DEBUG
        return Task.FromResult(true);
#else
        return Task.FromResult(false);
#endif
    }
}