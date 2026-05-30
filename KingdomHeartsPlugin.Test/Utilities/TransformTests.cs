using System.Numerics;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.Test.Utilities;

public class TransformTests
{
    [Test]
    [MethodDataSource(nameof(ScaleAndOffsetTests))]
    public async Task ScaleAndOffset(string name, float scale, Vector2 offset, Matrix3x2 expected)
    {
        await Assert.That(Transform.ScaleAndOffset(scale, offset)).IsEqualTo(expected);
    }

    public static IEnumerable<(string name, float scale, Vector2 offset, Matrix3x2 expected)> ScaleAndOffsetTests =>
        [
            new("identity", 1.0f, Vector2.Zero, Matrix3x2.Identity),
            new("scale only", 2.0f, Vector2.Zero, Matrix3x2.CreateScale(2.0f)),
            new("translate only", 1.0f, Vector2.One, Matrix3x2.CreateTranslation(Vector2.One)),
            new("scale and translate", 2.0f, Vector2.One, new Matrix3x2(2.0f, 0.0f, 0.0f, 2.0f, 1.0f, 1.0f)),
        ];
}