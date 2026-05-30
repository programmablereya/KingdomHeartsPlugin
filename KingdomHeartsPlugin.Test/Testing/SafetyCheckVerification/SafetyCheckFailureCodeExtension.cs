using KingdomHeartsPlugin.Utilities;
using TUnit.Assertions.Core;

namespace KingdomHeartsPlugin.Test.Testing.SafetyCheckVerification;

/// <summary>
/// Extension methods to enable use of <see cref="SafetyCheckFailureCodeAssertion"/> and
/// <see cref="SafetyCheckPassAssertion"/>.
/// </summary>
public static class SafetyCheckFailureCodeExtension
{
    /// <param name="assertionSource">The assertion source which is expected to fail the given safety check.</param>
    /// <typeparam name="TValue">Type of the delegate's intended return value.</typeparam>
    extension<TValue>(IDelegateAssertionSource<TValue> assertionSource)
    {
        /// <summary>
        /// Verifies that the assertion source fails a safety check with a specific code.
        /// </summary>
        /// <param name="code">The code of the safety check which is expected to fail.</param>
        /// <returns>The awaitable assertion.</returns>
        public SafetyCheckFailureCodeAssertion FailsSafetyCheckWithCode(string code)
        {
            return new SafetyCheckFailureCodeAssertion(assertionSource.Context.MapException<SafetyCheckException>(), code);
        }

        /// <summary>
        /// Verifies that the assertion source passes safety checks - i.e., it doesn't throw.
        /// </summary>
        /// <returns>The awaitable assertion.</returns>
        public SafetyCheckPassAssertion PassesSafetyChecks()
        {
            return new SafetyCheckPassAssertion(assertionSource.Context.MapException<SafetyCheckException>());
        }
    }
}