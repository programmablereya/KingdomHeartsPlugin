using System;

namespace KingdomHeartsPlugin.Utilities;

/// <summary>
/// An exception thrown when a safety check fails. It should never be thrown by production code;
/// use <see cref="System.Diagnostics.ConditionalAttribute">Conditional</see> on void CheckXxx methods that throw it,
/// e.g.:
/// <code>[Conditional("DEBUG")]
/// private void CheckSafety() {}</code>
/// </summary>
public class SafetyCheckException : Exception
{
    /// <summary>
    /// The test-readable code identifying this safety check.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Constructor for safety checks not associated with an exception. 
    /// </summary>
    /// <param name="code">The test-readable code identifying this safety check.</param>
    public SafetyCheckException(string code) : base(code)
    {
        Code = code;
    }
    
    /// <summary>
    /// Constructor for safety checks not associated with an exception. 
    /// </summary>
    /// <param name="code">The test-readable code identifying this safety check.</param>
    /// <param name="message">The human-readable message to put in the exception message.</param>
    public SafetyCheckException(string code, string message) : base($"{code}: {message}")
    {
        Code = code;
    }

    /// <summary>
    /// Constructor for safety checks associated with an exception.
    /// </summary>
    /// <param name="code">The test-readable code identifying this safety check.</param>
    /// <param name="innerException">The exception which caused the safety check to fail.</param>
    public SafetyCheckException(string code, Exception innerException) :
        base(code, innerException)
    {
        Code = code;
    }
    
    /// <summary>
    /// Constructor for safety checks associated with an exception.
    /// </summary>
    /// <param name="code">The test-readable code identifying this safety check.</param>
    /// <param name="message">The human-readable message to put in the exception message.</param>
    /// <param name="innerException">The exception which caused the safety check to fail.</param>
    public SafetyCheckException(string code, string message, Exception innerException) :
        base($"{code}: {message}", innerException)
    {
        Code = code;
    }
}