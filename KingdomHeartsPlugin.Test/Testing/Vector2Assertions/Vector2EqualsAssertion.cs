using System.Numerics;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace KingdomHeartsPlugin.Test.Testing.Vector2Assertions;

/// <summary>
/// Assertion verifying that two <see cref="Vector2" />s are equal within a given tolerance.
/// </summary>
public class Vector2EqualsAssertion(AssertionContext<Vector2> context, Vector2 expected)
    : ToleranceBasedEqualsAssertion<Vector2, float>(context, expected)
{
    protected override bool HasToleranceValue() => true;

    protected override bool IsWithinTolerance(Vector2 actual, Vector2 expected, float tolerance)
    {
        return AreExactlyEqual(actual, expected) ||
               Vector2.LessThanOrEqualAll(actual - expected, new Vector2(tolerance));
    }

    protected override bool IsWithinRelativeTolerance(Vector2 actual, Vector2 expected, double percentTolerance)
    {
        return AreExactlyEqual(actual, expected) ||
               Vector2.LessThanOrEqualAll(actual - expected, expected * (float)percentTolerance / 100.0f);
    }

    protected override object CalculateDifference(Vector2 actual, Vector2 expected) =>
        Vector2.Abs(actual - expected);

    protected override bool AreExactlyEqual(Vector2 actual, Vector2 expected)
    {
        if (Vector2.IsNaN(actual) == Vector2.Zero && Vector2.IsNaN(expected) == Vector2.Zero)
        {
            return Vector2.EqualsAll(expected, actual);
        }
        
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var xEqual = float.IsNaN(expected.X) ? float.IsNaN(actual.X) : expected.X == actual.X;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        var yEqual = float.IsNaN(expected.Y) ? float.IsNaN(actual.Y) : expected.Y == actual.Y;
        return xEqual && yEqual;
    }
}