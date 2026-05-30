using System;
using System.Collections.Immutable;
using KingdomHeartsPlugin.Enums;

namespace KingdomHeartsPlugin.Utilities
{
    /// <summary>
    /// String formatting utility methods.
    /// </summary>
    public static class StringFormatting
    {
        /// <summary>
        /// Suffixes indicating the magnitude of a quantity in powers of 1000:
        ///     none, thousands (kilo), millions, billions, trillions.
        /// </summary>
        private static readonly ImmutableArray<string> MagnitudeSuffixes = ["", "K", "M", "B", "T"];
        
        /// <summary>
        /// Formats <see cref="val"/> using the given abbreviation or formatting <see cref="style" />.
        /// </summary>
        /// <param name="val">The integer to format.</param>
        /// <param name="style">The formatting style to use, if any.</param>
        /// <param name="formatter">A format provider to use to format the number.</param>
        /// <returns>The formatted string.</returns>
        public static string FormatIntegerAbbreviated(
            long val, NumberFormatStyle style, IFormatProvider? formatter = null)
        {
            var groups = 
                (int) Math.Clamp(Math.Floor(Math.Log10(Math.Abs(val)) / 3.0f), 0, MagnitudeSuffixes.Length - 1);
            var groupFormat = new string(',', groups);
            var suffix = MagnitudeSuffixes[groups];
            if (groups == 0)
            {
                style = NumberFormatStyle.NoFormatting;
            }

            return val.ToString(style switch
            {
                NumberFormatStyle.ThousandsSeparator => "#,##0",
                NumberFormatStyle.SmallNumber => $"0{groupFormat}{suffix}",
                NumberFormatStyle.SmallNumberOneDecimalPrecision => $"0{groupFormat}.0{suffix}",
                NumberFormatStyle.SmallNumberTwoDecimalPrecision => $"0{groupFormat}.00{suffix}",
                _ => "0",
            }, formatter);
        }
    }
}
