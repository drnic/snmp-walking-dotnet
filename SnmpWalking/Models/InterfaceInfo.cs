namespace SnmpWalking.Models;

/// <summary>
/// Represents network interface information from SNMP
/// </summary>
public class InterfaceInfo
{
    /// <summary>
    /// Interface index
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Interface name/description
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Interface operational status (1=Up, 2=Down, 3=Testing)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Interface speed in bits per second
    /// </summary>
    public long Speed { get; set; }

    /// <summary>
    /// Gets the status as a human-readable string
    /// </summary>
    public string StatusText => Status switch
    {
        1 => "Up",
        2 => "Down",
        3 => "Testing",
        _ => "Unknown"
    };
}
