using System.Text;
using Lextm.SharpSnmpLib;

namespace SnmpWalking.Formatters;

/// <summary>
/// Formats SNMP values for display
/// </summary>
public static class SnmpValueFormatter
{
    /// <summary>
    /// Formats SNMP TimeTicks (hundredths of a second) into a human-readable uptime string
    /// </summary>
    /// <param name="ticks">TimeTicks value</param>
    /// <returns>Formatted string like "5d 3h 24m 15s"</returns>
    public static string FormatUptime(uint ticks)
    {
        var totalSeconds = ticks / 100;
        var days = totalSeconds / 86400;
        var hours = (totalSeconds % 86400) / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        var parts = new List<string>();
        if (days > 0) parts.Add($"{days}d");
        if (hours > 0) parts.Add($"{hours}h");
        if (minutes > 0) parts.Add($"{minutes}m");
        if (seconds > 0 || parts.Count == 0) parts.Add($"{seconds}s");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Formats a MAC address from raw bytes
    /// </summary>
    /// <param name="bytes">6-byte MAC address</param>
    /// <returns>Formatted MAC address like "aa:bb:cc:dd:ee:ff"</returns>
    public static string FormatMacAddress(byte[] bytes)
    {
        if (bytes.Length != 6)
            return BitConverter.ToString(bytes).Replace("-", ":");

        return string.Join(":", bytes.Select(b => b.ToString("x2")));
    }

    /// <summary>
    /// Formats interface speed from bps to Mbps if appropriate
    /// </summary>
    /// <param name="bps">Speed in bits per second</param>
    /// <returns>Formatted speed string</returns>
    public static string FormatSpeed(long bps)
    {
        if (bps >= 1_000_000)
            return $"{bps / 1_000_000} Mbps";

        return $"{FormatNumber(bps)} bps";
    }

    /// <summary>
    /// Formats a number with thousand separators
    /// </summary>
    /// <param name="number">Number to format</param>
    /// <returns>Formatted number string</returns>
    public static string FormatNumber(long number)
    {
        return number.ToString("N0");
    }

    /// <summary>
    /// Intelligently formats an OctetString based on its content
    /// Detects: printable strings, MAC addresses, IPv4 addresses, hex dumps
    /// </summary>
    /// <param name="octetString">OctetString to format</param>
    /// <param name="mibName">Optional MIB name for context</param>
    /// <returns>Formatted string representation</returns>
    public static string FormatOctetString(OctetString octetString, string? mibName = null)
    {
        var bytes = octetString.GetRaw();

        // Empty string
        if (bytes.Length == 0)
            return "\"\"";

        // Check if it's a printable ASCII string
        if (IsPrintableAscii(bytes))
            return $"\"{Encoding.ASCII.GetString(bytes)}\"";

        // Check for MAC address (6 bytes)
        if (bytes.Length == 6)
            return FormatMacAddress(bytes);

        // Check for IPv4 address (4 bytes)
        if (bytes.Length == 4)
            return string.Join(".", bytes);

        // For small binary data (≤16 bytes), show as hex
        if (bytes.Length <= 16)
            return "0x" + BitConverter.ToString(bytes).Replace("-", " ");

        // For larger binary data, show hex dump with length
        return $"Binary data ({bytes.Length} bytes): 0x" +
               BitConverter.ToString(bytes.Take(16).ToArray()).Replace("-", " ") + "...";
    }

    /// <summary>
    /// Checks if a byte array contains only printable ASCII characters
    /// </summary>
    private static bool IsPrintableAscii(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            // Allow printable ASCII (32-126) and common whitespace (9, 10, 13)
            if (b < 32 || b > 126)
            {
                if (b != 9 && b != 10 && b != 13) // tab, newline, carriage return
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Formats any SNMP variable data type
    /// </summary>
    /// <param name="data">SNMP data object</param>
    /// <param name="mibName">Optional MIB name for context</param>
    /// <returns>Formatted string representation with type info</returns>
    public static (string Value, string Type) FormatSnmpData(ISnmpData data, string? mibName = null)
    {
        return data switch
        {
            Integer32 i32 => (i32.ToInt32().ToString(), "Integer32"),
            Gauge32 g32 => (g32.ToUInt32().ToString(), "Gauge32"),
            Counter32 c32 => (c32.ToUInt32().ToString(), "Counter32"),
            Counter64 c64 => (c64.ToUInt64().ToString(), "Counter64"),
            TimeTicks tt => (FormatUptime(tt.ToUInt32()), "TimeTicks"),
            IP ip => (ip.ToString(), "IpAddress"),
            OctetString os => (FormatOctetString(os, mibName), "OctetString"),
            ObjectIdentifier oid => (oid.ToString(), "ObjectId"),
            Null => ("null", "Null"),
            _ => (data.ToString() ?? "unknown", data.GetType().Name)
        };
    }
}
