using System.Numerics;
using TUnit.Assertions.Exceptions;

namespace KingdomHeartsPlugin.Test.Testing.Vector2Assertions.SelfTest;

public class Vector2EqualsAssertionTest
{
    [Test]
    [MethodDataSource(nameof(ExactEqualsSuccessCases))]
    public async Task ExactEqualsSuccess(Vector2 expected, Vector2 actual)
    {
        await Assert.That(TestDelegate).ThrowsNothing();
        return;
        
        async Task TestDelegate() => await Assert.That(actual).IsVectorEqualTo(expected);
    }
    
    [Test]
    [MethodDataSource(nameof(ExactEqualsSuccessCases))]
    public async Task ToleranceZeroEqualsSuccess(Vector2 expected, Vector2 actual)
    {
        await Assert.That(TestDelegate).ThrowsNothing();
        return;
        
        async Task TestDelegate() => await Assert.That(actual).IsVectorEqualTo(expected).Within(0.0f);
    }

    [Test]
    [MethodDataSource(nameof(ExactEqualsSuccessCases))]
    public async Task RelativeToleranceZeroEqualsSuccess(Vector2 expected, Vector2 actual)
    {
        await Assert.That(TestDelegate).ThrowsNothing();
        return;
        
        async Task TestDelegate() => await Assert.That(actual).IsVectorEqualTo(expected).WithinRelativeTolerance(0.0d);
    }
    
    public static IEnumerable<(Vector2 expected, Vector2 actual)> ExactEqualsSuccessCases =>
    [
        (Vector2.Zero, Vector2.Zero),
        (Vector2.NegativeZero, Vector2.NegativeZero),
        (Vector2.Zero, Vector2.NegativeZero),
        (Vector2.NegativeZero, Vector2.Zero),
        
        (Vector2.NaN, Vector2.NaN),
        (Vector2.AllBitsSet, Vector2.AllBitsSet),
        (Vector2.NaN, Vector2.AllBitsSet),
        (Vector2.AllBitsSet, Vector2.NaN),
        
        (Vector2.NegativeInfinity, Vector2.NegativeInfinity),
        (Vector2.PositiveInfinity, Vector2.PositiveInfinity),
        
        (Vector2.E, Vector2.E),
        (Vector2.Pi, Vector2.Pi),
        (Vector2.Tau, Vector2.Tau),
        (Vector2.Epsilon, Vector2.Epsilon),
        
        (Vector2.UnitX, Vector2.UnitX),
        (Vector2.UnitY, Vector2.UnitY),
        (Vector2.One, Vector2.One),
    ];
    
    [Test]
    [MethodDataSource(nameof(ExactEqualsFailureCases))]
    public async Task ExactEqualsFailure(Vector2 expected, Vector2 actual, string message)
    {
        await Assert.That(TestDelegate).Throws<AssertionException>().WithMessageContaining(message);
        return;
        
        async Task TestDelegate() => await Assert.That(actual).IsVectorEqualTo(expected);
    }
    
    public static IEnumerable<(Vector2 expected, Vector2 actual, string message)> ExactEqualsFailureCases =>
    [
        (Vector2.One, Vector2.Zero, "Expected to be <1, 1>\nbut found <0, 0>"),
        (Vector2.Zero, Vector2.One, "Expected to be <0, 0>\nbut found <1, 1>"),
        
        (Vector2.Zero, Vector2.Epsilon, "Expected to be <0, 0>\nbut found <1E-45, 1E-45>"),
        (Vector2.Epsilon, Vector2.Zero, "Expected to be <1E-45, 1E-45>\nbut found <0, 0>"),
        
        (Vector2.NegativeInfinity, Vector2.PositiveInfinity, "Expected to be <-∞, -∞>\nbut found <∞, ∞>"),
        (Vector2.PositiveInfinity, Vector2.NegativeInfinity, "Expected to be <∞, ∞>\nbut found <-∞, -∞>"),
        
        (Vector2.UnitX, Vector2.UnitY, "Expected to be <1, 0>\nbut found <0, 1>"),
        (Vector2.UnitY, Vector2.UnitX, "Expected to be <0, 1>\nbut found <1, 0>"),
    ];
}