using KingdomHeartsPlugin.Utilities;
using TUnit.Assertions.Exceptions;

namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification.SelfTest;

public class SafetyCheckFailureCodeExtensionTests
{
    [Test]
    [ReleaseBehaviorTest]
    public async Task FailsSafetyCheckWithCode_FailsWhenNotInDebug()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to throw SafetyCheckException with code \"test\"" +
            "\nbut could never throw SafetyCheckException because safety checks are disabled in DEBUG");
        return;

        void InnerDelegate() => throw new SafetyCheckException("test", "safety check failed");
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).FailsSafetyCheckWithCode("test");
    }
    
    [Test]
    [ReleaseBehaviorTest]
    public async Task PassesSafetyChecks_FailsWhenNotInDebug()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to pass all safety checks" +
            "\nbut did not even run safety checks because safety checks are disabled in DEBUG");
        return;

        void InnerDelegate()
        {
        }
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).PassesSafetyChecks();
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task FailsSafetyCheckWithCode_FailsTestWhenNothingThrown()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to throw SafetyCheckException with code \"test\"" +
            "\nbut no exception was thrown");
        return;

        void InnerDelegate()
        {
        }
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).FailsSafetyCheckWithCode("test");
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task PassesSafetyChecks_PassesTestWhenNothingThrown()
    {
        await Assert.That(TestDelegate).ThrowsNothing();
        return;

        void InnerDelegate()
        {
        }
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).PassesSafetyChecks();
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task FailsSafetyCheckWithCode_FailsTestWhenNonSafetyCheckThrown()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to throw SafetyCheckException with code \"test\"" +
            "\nbut threw System.Exception");
        return;
        
        void InnerDelegate() => throw new Exception();
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).FailsSafetyCheckWithCode("test");   
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task PassesSafetyChecks_FailsTestWhenNonSafetyCheckThrown()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to pass all safety checks" +
            "\nbut threw System.Exception");
        return;
        
        void InnerDelegate() => throw new Exception();
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).PassesSafetyChecks();   
    }

    [Test]
    [SafetyCheckTest]
    public async Task FailsSafetyCheckWithCode_FailsTestWhenSafetyCheckWithDifferentCodeThrown()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to throw SafetyCheckException with code \"test\"" +
            "\nbut threw SafetyCheckException with code \"fail\"");
        return;
        
        void InnerDelegate() => throw new SafetyCheckException("fail", "safety check failed");
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).FailsSafetyCheckWithCode("test");   
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task PassesSafetyChecks_FailsTestWhenSafetyCheckThrown()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to pass all safety checks" +
            $"\nbut threw SafetyCheckException with code \"fail\"");
        return;
        
        void InnerDelegate() => throw new SafetyCheckException("fail", "safety check failed");
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).PassesSafetyChecks();   
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task FailsSafetyCheckWithCode_FailsTestWhenSafetyCheckWithPrefixThrown()
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().And.HasMessageContaining(
            "Expected to throw SafetyCheckException with code \"test\"" +
            "\nbut threw SafetyCheckException with code \"testable\"");
        return;
        
        void InnerDelegate() => throw new SafetyCheckException("testable", "testable safety check failed");
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).FailsSafetyCheckWithCode("test");   
    }
    
    [Test]
    [SafetyCheckTest]
    public async Task FailsSafetyCheckWithCode_PassesTestWhenSafetyCheckWithSameCodeThrown()
    {
        await Assert.That(TestDelegate).ThrowsNothing();
        return;
        
        void InnerDelegate() => throw new SafetyCheckException("test", "safety check for testing");
        async Task TestDelegate() =>
            await Assert.That(InnerDelegate).FailsSafetyCheckWithCode("test");   
    }
}