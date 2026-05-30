using KingdomHeartsPlugin.Utilities;
using TUnit.Assertions.Core;

namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification;

/// <summary>
/// Assertion which verifies that the associated delegate throws a SafetyCheckException with the supplied code.
/// </summary>
public class SafetyCheckFailureCodeAssertion(AssertionContext<SafetyCheckException> context, string code)
    : Assertion<SafetyCheckException>(context)
{
 
    /// <summary>
    /// Checks that the completed assertion source threw the desired exception. Automatically fails in non-DEBUG builds.
    /// </summary>
    /// <param name="metadata">The information about the completed assertion.</param>
    /// <returns>The assertion result, passed or failed.</returns>
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<SafetyCheckException> metadata)
    {
        #if !DEBUG
        return Task.FromResult(AssertionResult.Failed(
            $"could never throw {nameof(SafetyCheckException)} because safety checks are disabled in DEBUG"));
        #else
        var targetException = metadata.Value;
        var evaluationException = metadata.Exception;

        if (evaluationException != null)
        {
            return Task.FromResult(
                AssertionResult.Failed(
                    $"threw {evaluationException}",
                    evaluationException)); 
        }

        if (targetException is { } exception)
        {
            if (exception.Code == code)
            {
                return Task.FromResult(AssertionResult.Passed);
            }

            return Task.FromResult(
                AssertionResult.Failed(
                    $"threw {nameof(SafetyCheckException)} with code \"{exception.Code}\"", exception));
        }
        else
        {
            return Task.FromResult(
                AssertionResult.Failed("no exception was thrown"));
        }
        #endif
    }

    /// <summary>
    /// Gets the human-readable description of this assertion.
    /// </summary>
    /// <returns>The human-readable description of this assertion.</returns>
    protected override string GetExpectation()
    {
        return $"to throw {nameof(SafetyCheckException)} with code \"{code}\"";
    }
}