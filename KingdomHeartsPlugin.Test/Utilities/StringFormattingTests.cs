using System.Collections.Immutable;
using System.Globalization;
using KingdomHeartsPlugin.Enums;
using KingdomHeartsPlugin.Utilities;

namespace KingdomHeartsPlugin.Test.Utilities;

public class StringFormattingTests
{
    [Test]
    [MethodDataSource(nameof(SmallNumberValuesTests))]
    public async Task SmallNumberValues(long value, string expected)
    {
        using (Assert.Multiple())
        {
            foreach (var style in Enum.GetValues<NumberFormatStyle>()) {
                await Assert.That(
                    StringFormatting.FormatIntegerAbbreviated(value, style, CultureInfo.InvariantCulture))
                    .IsEqualTo(expected);
            }
        }
    }

    public static IEnumerable<(long value, string expected)> SmallNumberValuesTests =>
    [
        new(0, "0"),
        new(1, "1"),
        new(-1, "-1"),
        new(999, "999"),
        new(-999, "-999"),
    ];
    
    [Test]
    [MethodDataSource(nameof(LargeNumberTests))]
    public async Task LargeNumberValues(long value, ImmutableArray<string> expected)
    {
        var styles = Enum.GetValues<NumberFormatStyle>();
        await Assert.That(expected.Length).IsEqualTo(styles.Length);
        using (Assert.Multiple())
        {
            for (var index = 0; index < expected.Length; index++)
            {
                await Assert.That(
                    StringFormatting.FormatIntegerAbbreviated(value, styles[index],
                        CultureInfo.InvariantCulture)).IsEqualTo(expected[index]);                
            }
        }
    }
    
    public static IEnumerable<(long value, ImmutableArray<string> expected)> LargeNumberTests =>
    [
        new(1000, ["1000", "1,000", "1K", "1.0K", "1.00K"]),
        new(-1000, ["-1000", "-1,000", "-1K", "-1.0K", "-1.00K"]),
        new(1001, ["1001", "1,001", "1K", "1.0K", "1.00K"]),
        new(1010, ["1010", "1,010", "1K", "1.0K", "1.01K"]),
        new(1100, ["1100", "1,100", "1K", "1.1K", "1.10K"]),
        new(1005, ["1005", "1,005", "1K", "1.0K", "1.01K"]),
        new(-1005, ["-1005", "-1,005", "-1K", "-1.0K", "-1.01K"]),
        new(1050, ["1050", "1,050", "1K", "1.1K", "1.05K"]),
        new(1500, ["1500", "1,500", "2K", "1.5K", "1.50K"]),
        new(999999, ["999999", "999,999", "1000K", "1000.0K", "1000.00K"]),
        new(1000000, ["1000000", "1,000,000", "1M", "1.0M", "1.00M"]),
        new(999999999, ["999999999", "999,999,999", "1000M", "1000.0M", "1000.00M"]),
        new(1000000000, ["1000000000", "1,000,000,000", "1B", "1.0B", "1.00B"]),
        new(999999999999, ["999999999999", "999,999,999,999", "1000B", "1000.0B", "1000.00B"]),
        new(1000000000000, ["1000000000000", "1,000,000,000,000", "1T", "1.0T", "1.00T"]),
        new(999999999999999,
            ["999999999999999", "999,999,999,999,999", "1000T", "1000.0T", "1000.00T"]),
        new(999999999999999999,
            ["999999999999999999", "999,999,999,999,999,999", "1000000T", "1000000.0T", "1000000.00T"]),
    ];
}