using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using SnmpWalking.Formatters;
using SnmpWalking.Models;

namespace SnmpWalking;

/// <summary>
/// Monitors network devices via SNMP
/// </summary>
public class SnmpMonitor
{
    private readonly SnmpOptions _options;
    private readonly IPEndPoint _endpoint;
    private readonly TimeSpan _timeout;

    // Standard SNMP OIDs
    private static class Oids
    {
        public const string SysDescr = "1.3.6.1.2.1.1.1.0";
        public const string SysUpTime = "1.3.6.1.2.1.1.3.0";
        public const string SysContact = "1.3.6.1.2.1.1.4.0";
        public const string SysName = "1.3.6.1.2.1.1.5.0";
        public const string SysLocation = "1.3.6.1.2.1.1.6.0";
        public const string IfNumber = "1.3.6.1.2.1.2.1.0";
        public const string IfDescr = "1.3.6.1.2.1.2.2.1.2";
        public const string IfSpeed = "1.3.6.1.2.1.2.2.1.5";
        public const string IfOperStatus = "1.3.6.1.2.1.2.2.1.8";
    }

    public SnmpMonitor(SnmpOptions options)
    {
        _options = options;
        _endpoint = new IPEndPoint(IPAddress.Parse(options.Host), options.Port);
        _timeout = TimeSpan.FromMilliseconds(options.TimeoutMs);
    }

    /// <summary>
    /// Monitors the device using appropriate SNMP version
    /// </summary>
    public async Task MonitorAsync()
    {
        try
        {
            if (_options.UseV3)
            {
                Console.WriteLine($"Connecting to device at {_options.Host} using SNMPv3...");
                await MonitorV3Async();
            }
            else
            {
                Console.WriteLine($"Connecting to device at {_options.Host} using SNMPv2c...");
                await MonitorV2Async();
            }
        }
        catch (Lextm.SharpSnmpLib.Messaging.TimeoutException)
        {
            Console.WriteLine($"\nError: Request timeout - no response from {_options.Host}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during monitoring: {ex.Message}");
        }
    }

    /// <summary>
    /// Monitors using SNMPv2c
    /// </summary>
    private async Task MonitorV2Async()
    {
        Console.WriteLine();
        Console.WriteLine("=== System Information ===");

        // Get system information
        await DisplaySystemInfoV2Async();

        Console.WriteLine();
        Console.WriteLine("=== Network Interfaces ===");

        // Get interface count
        var ifCount = await GetInterfaceCountV2Async();
        if (ifCount > 0)
        {
            Console.WriteLine($"Number of interfaces: {ifCount}");
            Console.WriteLine();

            // Get interface details
            await DisplayInterfacesV2Async(ifCount);
        }
    }

    /// <summary>
    /// Monitors using SNMPv3
    /// </summary>
    private async Task MonitorV3Async()
    {
        if (string.IsNullOrEmpty(_options.Username) || string.IsNullOrEmpty(_options.Password))
        {
            throw new InvalidOperationException("Username and password are required for SNMPv3");
        }

        Console.WriteLine();
        Console.WriteLine("=== System Information ===");

        await DisplaySystemInfoV3Async();

        Console.WriteLine();
        Console.WriteLine("=== Network Interfaces ===");

        var ifCount = await GetInterfaceCountV3Async();
        if (ifCount > 0)
        {
            Console.WriteLine($"Number of interfaces: {ifCount}");
            Console.WriteLine();

            await DisplayInterfacesV3Async(ifCount);
        }
    }

    private async Task DisplaySystemInfoV2Async()
    {
        var oids = new[]
        {
            new ObjectIdentifier(Oids.SysDescr),
            new ObjectIdentifier(Oids.SysUpTime),
            new ObjectIdentifier(Oids.SysContact),
            new ObjectIdentifier(Oids.SysName),
            new ObjectIdentifier(Oids.SysLocation)
        };

        var result = await GetWithRetryV2Async(oids);

        foreach (var variable in result)
        {
            var label = GetOidLabel(variable.Id.ToString());
            var (value, _) = SnmpValueFormatter.FormatSnmpData(variable.Data);
            Console.WriteLine($"{label}: {value}");
        }
    }

    private async Task DisplaySystemInfoV3Async()
    {
        var oids = new[]
        {
            new ObjectIdentifier(Oids.SysDescr),
            new ObjectIdentifier(Oids.SysUpTime),
            new ObjectIdentifier(Oids.SysContact),
            new ObjectIdentifier(Oids.SysName),
            new ObjectIdentifier(Oids.SysLocation)
        };

        var result = await GetWithRetryV3Async(oids);

        foreach (var variable in result)
        {
            var label = GetOidLabel(variable.Id.ToString());
            var (value, _) = SnmpValueFormatter.FormatSnmpData(variable.Data);
            Console.WriteLine($"{label}: {value}");
        }
    }

    private async Task<int> GetInterfaceCountV2Async()
    {
        var result = await GetWithRetryV2Async(new[] { new ObjectIdentifier(Oids.IfNumber) });
        if (result.Count > 0 && result[0].Data is Integer32 count)
        {
            return count.ToInt32();
        }
        return 0;
    }

    private async Task<int> GetInterfaceCountV3Async()
    {
        var result = await GetWithRetryV3Async(new[] { new ObjectIdentifier(Oids.IfNumber) });
        if (result.Count > 0 && result[0].Data is Integer32 count)
        {
            return count.ToInt32();
        }
        return 0;
    }

    private async Task DisplayInterfacesV2Async(int count)
    {
        // Get all interface names first
        var ifDescrOid = new ObjectIdentifier(Oids.IfDescr);
        var interfaces = new List<InterfaceInfo>();

        // Use Walk to get all interface names
        var ifNames = new List<Variable>();
        await Task.Run(() => Messenger.Walk(
            VersionCode.V2,
            _endpoint,
            new OctetString(_options.Community),
            ifDescrOid,
            ifNames,
            (int)_timeout.TotalMilliseconds,
            WalkMode.WithinSubtree));

        foreach (var variable in ifNames)
        {
            var index = ExtractIndex(variable.Id.ToString(), Oids.IfDescr);

            if (variable.Data is OctetString name)
            {
                interfaces.Add(new InterfaceInfo
                {
                    Index = index,
                    Name = name.ToString()
                });
            }
        }

        // Get status and speed for each interface
        foreach (var iface in interfaces)
        {
            var statusOid = new ObjectIdentifier($"{Oids.IfOperStatus}.{iface.Index}");
            var speedOid = new ObjectIdentifier($"{Oids.IfSpeed}.{iface.Index}");

            var result = await GetWithRetryV2Async(new[] { statusOid, speedOid });

            if (result.Count >= 1 && result[0].Data is Integer32 status)
            {
                iface.Status = status.ToInt32();
            }

            if (result.Count >= 2 && result[1].Data is Gauge32 speed)
            {
                iface.Speed = speed.ToUInt32();
            }

            // Display interface info
            Console.WriteLine($"Interface {iface.Index}: {iface.Name}");
            Console.WriteLine($"  Status: {iface.StatusText}");
            Console.WriteLine($"  Speed: {SnmpValueFormatter.FormatSpeed(iface.Speed)}");
            Console.WriteLine();
        }
    }

    private async Task DisplayInterfacesV3Async(int count)
    {
        var ifDescrOid = new ObjectIdentifier(Oids.IfDescr);
        var interfaces = new List<InterfaceInfo>();

        // Discovery for SNMPv3
        var discovery = Messenger.GetNextDiscovery(SnmpType.GetBulkRequestPdu);
        var report = discovery.GetResponse((int)_timeout.TotalMilliseconds, _endpoint);
        var auth = new MD5AuthenticationProvider(new OctetString(_options.Password!));
        var priv = new DESPrivacyProvider(new OctetString(_options.Password!), auth);

        // Use BulkWalk to get all interface names
        var ifNames = new List<Variable>();
        await Task.Run(() => Messenger.BulkWalk(
            VersionCode.V3,
            _endpoint,
            new OctetString(_options.Username!),
            OctetString.Empty, // context name
            ifDescrOid,
            ifNames,
            (int)_timeout.TotalMilliseconds,
            10, // max repetitions
            WalkMode.WithinSubtree,
            priv,
            report));

        foreach (var variable in ifNames)
        {
            var index = ExtractIndex(variable.Id.ToString(), Oids.IfDescr);

            if (variable.Data is OctetString name)
            {
                interfaces.Add(new InterfaceInfo
                {
                    Index = index,
                    Name = name.ToString()
                });
            }
        }

        // Get status and speed for each interface
        foreach (var iface in interfaces)
        {
            var statusOid = new ObjectIdentifier($"{Oids.IfOperStatus}.{iface.Index}");
            var speedOid = new ObjectIdentifier($"{Oids.IfSpeed}.{iface.Index}");

            var result = await GetWithRetryV3Async(new[] { statusOid, speedOid });

            if (result.Count >= 1 && result[0].Data is Integer32 status)
            {
                iface.Status = status.ToInt32();
            }

            if (result.Count >= 2 && result[1].Data is Gauge32 speed)
            {
                iface.Speed = speed.ToUInt32();
            }

            Console.WriteLine($"Interface {iface.Index}: {iface.Name}");
            Console.WriteLine($"  Status: {iface.StatusText}");
            Console.WriteLine($"  Speed: {SnmpValueFormatter.FormatSpeed(iface.Speed)}");
            Console.WriteLine();
        }
    }

    private async Task<IList<Variable>> GetWithRetryV2Async(ObjectIdentifier[] oids)
    {
        return await Task.Run(() =>
        {
            var variables = oids.Select(oid => new Variable(oid)).ToList();
            return Messenger.Get(
                VersionCode.V2,
                _endpoint,
                new OctetString(_options.Community),
                variables,
                (int)_timeout.TotalMilliseconds);
        });
    }

    private async Task<IList<Variable>> GetWithRetryV3Async(ObjectIdentifier[] oids)
    {
        if (string.IsNullOrEmpty(_options.Username) || string.IsNullOrEmpty(_options.Password))
        {
            throw new InvalidOperationException("Username and password are required for SNMPv3");
        }

        // For now, use BulkWalk with maxRepetitions=1 as a workaround for Get with V3
        // This is more complex than needed but works with the available API
        return await Task.Run(() =>
        {
            var results = new List<Variable>();
            var discovery = Messenger.GetNextDiscovery(SnmpType.GetBulkRequestPdu);
            var report = discovery.GetResponse((int)_timeout.TotalMilliseconds, _endpoint);

            var auth = new MD5AuthenticationProvider(new OctetString(_options.Password));
            var priv = new DESPrivacyProvider(new OctetString(_options.Password), auth);

            // Get each OID individually
            foreach (var oid in oids)
            {
                var singleResult = new List<Variable>();
                Messenger.BulkWalk(
                    VersionCode.V3,
                    _endpoint,
                    new OctetString(_options.Username),
                    OctetString.Empty,
                    oid,
                    singleResult,
                    (int)_timeout.TotalMilliseconds,
                    1, // max repetitions
                    WalkMode.WithinSubtree,
                    priv,
                    report);

                if (singleResult.Count > 0)
                {
                    results.Add(singleResult[0]);
                }
            }

            return (IList<Variable>)results;
        });
    }

    private static string GetOidLabel(string oid) => oid switch
    {
        Oids.SysDescr => "System Description",
        Oids.SysUpTime => "SNMP Uptime",
        Oids.SysContact => "System Contact",
        Oids.SysName => "System Name",
        Oids.SysLocation => "System Location",
        _ => oid
    };

    private static int ExtractIndex(string fullOid, string baseOid)
    {
        var indexPart = fullOid.Substring(baseOid.Length + 1);
        return int.TryParse(indexPart, out var index) ? index : 0;
    }
}
