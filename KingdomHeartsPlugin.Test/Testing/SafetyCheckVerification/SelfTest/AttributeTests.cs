namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification.SelfTest;

public class AttributeTests
{
    [Test]
    [ReleaseBehaviorTest]
    // ReSharper disable once AsyncMethodWithoutAwait
    public async Task ReleaseBehaviorTestSkippedInDebug()
    {
        #if DEBUG
        Assert.Fail("This test should never run.");
        #endif
    }

    [Test]
    [SafetyCheckTest]
    // ReSharper disable once AsyncMethodWithoutAwait
    public async Task SafetyCheckTestSkippedInRelease()
    {
        #if !DEBUG
        Assert.Fail("This test should never run.");
        #endif 
    }
}