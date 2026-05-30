using System.Numerics;
using TUnit.Assertions.Core;

namespace KingdomHeartsPlugin.Test.Testing.Vector2Assertions;

/// <summary>
/// Extension methods for <see cref="Vector2" /> assertions. 
/// </summary>
public static class Vector2Extension
{
    extension(IAssertionSource<Vector2> assertionSource)
    {
        public Vector2EqualsAssertion IsVectorEqualTo(Vector2 expected)
        {
            return new Vector2EqualsAssertion(assertionSource.Context, expected);
        }
    }

}