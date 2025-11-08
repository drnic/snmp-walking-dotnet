namespace SnmpWalking.Models;

/// <summary>
/// Configuration options for SNMP operations
/// </summary>
public class SnmpOptions
{
    /// <summary>
    /// Target host IP address or hostname
    /// </summary>
    public string Host { get; set; } = "192.168.20.1";

    /// <summary>
    /// SNMP community string for SNMPv2c
    /// </summary>
    public string Community { get; set; } = "public";

    /// <summary>
    /// Username for SNMPv3 authentication
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for SNMPv3 authentication
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// OID to walk (default is system tree)
    /// </summary>
    public string WalkOid { get; set; } = "1.3.6.1.2.1.1";

    /// <summary>
    /// Number of retry attempts for failed SNMP operations
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Timeout in milliseconds for SNMP operations
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// SNMP port (default 161)
    /// </summary>
    public int Port { get; set; } = 161;

    /// <summary>
    /// Whether to use SNMPv3 instead of SNMPv2c
    /// </summary>
    public bool UseV3 => !string.IsNullOrEmpty(Username);
}
