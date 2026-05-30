using KingdomHeartsPlugin.Utilities;
using TUnit.Assertions.Core;

namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification;

/// <summary>
/// Assertion which verifies that the associated delegate does not throw with the associated code.
/// </summary>
public class SafetyCheckPassAssertion(AssertionContext<SafetyCheckException> context)
    : Assertion<SafetyCheckException>(context)
{
 
    /// <summary>
    /// Checks that the completed assertion source did not throw. Automatically fails in non-DEBUG builds.
    /// </summary>
    /// <param name="metadata">The information about the completed assertion.</param>
    /// <returns>The assertion result, passed or failed.</returns>
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<SafetyCheckException> metadata)
    {
        #if !DEBUG
        return Task.FromResult(AssertionResult.Failed(
            $"did not even run safety checks because safety checks are disabled in DEBUG"));
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

        if (targetException != null)
        {
            return Task.FromResult(
                AssertionResult.Failed(
                    $"threw {nameof(SafetyCheckException)} with code \"{targetException.Code}\"",
                    targetException));
        }
        else
        {
            return Task.FromResult(AssertionResult.Passed);
        }
        #endif
    }

    /// <summary>
    /// Gets the human-readable description of this assertion.
    /// </summary>
    /// <returns>The human-readable description of this assertion.</returns>
    protected override string GetExpectation()
    {
        return "to pass all safety checks";
    }
}