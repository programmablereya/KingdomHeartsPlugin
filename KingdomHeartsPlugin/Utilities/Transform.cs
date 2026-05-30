using System.Numerics;

namespace KingdomHeartsPlugin.Utilities
{
    /// <summary>
    /// Transformation matrix helper functions.
    /// </summary>
    public static class Transform
    {
        /// <summary>
        /// Creates a transformation matrix that scales, then applies an offset.
        /// </summary>
        /// <param name="scale">The factor by which to scale the mesh.</param>
        /// <param name="offset">The offset by which to translate the mesh.</param>
        /// <returns>The transformation matrix.</returns>
        public static Matrix3x2 ScaleAndOffset(float scale, Vector2 offset)
        {
            return Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(offset);
        }
    }   
}