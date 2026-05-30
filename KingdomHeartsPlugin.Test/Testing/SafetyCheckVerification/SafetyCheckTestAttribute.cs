namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification;

/// <summary>
/// Attribute marking a test as only running in debug mode.
/// </summary>
public class SafetyCheckTestAttribute() : SkipAttribute("This test verifies a safety check and is not supported in Release mode.")
{
    /// <summary>
    /// Marks this test as skipped if we are running in release mode.
    /// </summary>
    /// <param name="context">The context of the test registration.</param>
    /// <returns>True if this test should be skipped because we are in release mode, false if not.</returns>
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
#if DEBUG
        return Task.FromResult(false);
#else
        return Task.FromResult(true);
#endif
    }
}