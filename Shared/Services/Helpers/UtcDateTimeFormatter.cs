using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Services.Helpers;

/// <summary>
/// A static helper class for formatting DateTime values to UTC with millisecond precision.
/// </summary>
/// <remarks>
/// For consistency across the Hazard application, ALL its DateTime values should be normalized using <see cref="Normalize(DateTime)"/>."/>
///</remarks>
public static class UtcDateTimeFormatter
{
    /// <summary>
    /// Converts a date time to UTC and truncates to seconds.
    /// </summary>
    /// <param name="original">The date time to normalize.</param>
    /// <returns>A date time in Universal Time, without ticks beyond seconds.</returns>
    public static DateTime Normalize(DateTime original)
    {
        var utc = original.Kind == DateTimeKind.Utc ? original : original.ToUniversalTime();

        return TruncateToSeconds(utc);
    }

    private static DateTime TruncateToSeconds(DateTime original)
    {
        return new DateTime(original.Ticks - (original.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }
}
