using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.Test.Utilities;

public class SafetyCheckExceptionTests
{
    [Test]
    [MethodDataSource(nameof(MessageTests))]
    public async Task Message(string name, SafetyCheckException exception, string expectedMessage)
    {
        await Assert.That(exception).HasMessageEqualTo(expectedMessage);
    }

    public static IEnumerable<(string name, SafetyCheckException exception, string expectedMessage)> MessageTests =>
    [
        new("code only", new SafetyCheckException("a code"), "a code"),
        new("code and message",
            new SafetyCheckException("another code", "some message"), "another code: some message"),
        new("code and exception",
            new SafetyCheckException("a third code", new Exception("inner message")), "a third code"),
        new("code and message and exception",
            new SafetyCheckException("a final code", "the message to go with", new Exception("inner message")),
            "a final code: the message to go with")
    ];
    
    [Test]
    [MethodDataSource(nameof(CodeTests))]
    public async Task Code(string name, SafetyCheckException exception, string expectedCode)
    {
        await Assert.That(exception.Code).IsEqualTo(expectedCode);
    }
    
    public static IEnumerable<(string name, SafetyCheckException exception, string expectedCode)> CodeTests =>
    [
        new("code only", new SafetyCheckException("a code"), "a code"),
        new("code and message",
            new SafetyCheckException("another code", "some message"), "another code"),
        new("code and exception",
            new SafetyCheckException("a third code", new Exception("inner message")), "a third code"),
        new("code and message and exception",
            new SafetyCheckException("a final code", "the message to go with", new Exception("inner message")),
            "a final code")
    ];
    
    [Test]
    [MethodDataSource(nameof(InnerExceptionTests))]
    public async Task InnerException(string name, SafetyCheckException exception, string expectedMessage)
    {
        await Assert.That(exception).HasInnerException();
        await Assert.That(exception.InnerException).HasMessageEqualTo(expectedMessage);
    }
    
    public static IEnumerable<(string name, SafetyCheckException exception, string expectedMessage)>
        InnerExceptionTests =>
    [
        new("code and exception",
            new SafetyCheckException("a third code", new Exception("an inner message")), "an inner message"),
        new("code and message and exception",
            new SafetyCheckException("a final code", "the message to go with",
                new Exception("another inner message")),
            "another inner message")
    ];

    [Test]
    [MethodDataSource(nameof(NoInnerExceptionTests))]
    public async Task NoInnerException(string name, SafetyCheckException exception)
    {
        await Assert.That(exception).HasNoInnerException();
    }
    
    public static IEnumerable<(string name, SafetyCheckException exception)> NoInnerExceptionTests =>
    [
        new("code only", new SafetyCheckException("a code")),
        new("code and message",
            new SafetyCheckException("another code", "some message")),
    ];
}