using System.Numerics;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.Test.Utilities;

public class CircleTests
{
    private const float Tolerance = 0.0000001f;
    
    [Test]
    [MethodDataSource(nameof(PointAtAngleTests))]
    public async Task PointAtAngle(string name, Vector2 center, double angle, float radius, Vector2 expected)
    {
        await Assert.That(Circle.PointAtAngle(center, angle, radius))
            .Satisfies(v => (v - expected).Length() < Tolerance);
    }

    public static IEnumerable<(string name, Vector2 center, double angle, float radius, Vector2 expected)>
        PointAtAngleTests =>
    [
        new("0 = 0°", Vector2.Zero, 0, 5.0f, new Vector2(5.0f, 0.0f)),
        new("2π = 360°", new Vector2(3.0f, 1.0f), 2 * Math.PI, 0.5f, new Vector2(3.5f, 1.0f)),
        new("-2π = -360°", new Vector2(5.0f, 0.0f), -2 * Math.PI, 0.0f, new Vector2(5.0f, 0.0f)),
        new("π/2 = 90°", Vector2.Zero, Math.PI / 2, 1.0f, new Vector2(0.0f, 1.0f)),
        new("-3π/2 = -270°", new Vector2(-3.0f, -1.0f), -3 * Math.PI / 2, 1.0f, new Vector2(-3.0f, 0.0f)),
        new("π = 180°", Vector2.Zero, Math.PI, 1.0f, new Vector2(-1.0f, 0.0f)),
        new("-π = -180°", Vector2.Zero, Math.PI, -1.0f, new Vector2(1.0f, 0.0f)),
        new("π/6 = 30°", Vector2.Zero, Math.PI / 6, 1.0f, new Vector2((float)(Math.Sqrt(3) / 2), 0.5f)),
    ];
    
    [Test]
    [MethodDataSource(nameof(ArcLengthTests))]
    public async Task ArcLength(string name, double angle, float radius, float expected)
    {
        await Assert.That(Circle.ArcLength(angle, radius)).IsEqualTo(expected).Within(Tolerance);
    }
    
    public static IEnumerable<(string name, double angle, float radius, float expected)>
        ArcLengthTests =>
    [
        ("simple", 2.0, 5.0f, 10.0f),
    ];
}