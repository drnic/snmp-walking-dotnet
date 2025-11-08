using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using SnmpWalking.Formatters;
using SnmpWalking.Models;

namespace SnmpWalking;

/// <summary>
/// Performs SNMP walk operations
/// </summary>
public class SnmpWalker
{
    private readonly SnmpOptions _options;

    public SnmpWalker(SnmpOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Walks an SNMP tree starting at the specified OID
    /// </summary>
    /// <param name="startOid">Starting OID (defaults to options.WalkOid)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task WalkAsync(string? startOid = null)
    {
        var oid = startOid ?? _options.WalkOid;
        var endpoint = new IPEndPoint(IPAddress.Parse(_options.Host), _options.Port);
        var timeout = _options.TimeoutMs;

        Console.WriteLine($"Walking SNMP tree starting at {oid} on {_options.Host}...");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        var count = 0;
        var startOidObj = new ObjectIdentifier(oid);

        await Task.Run(() =>
        {
            try
            {
                var results = new List<Variable>();

                if (_options.UseV3)
                {
                    WalkV3(endpoint, startOidObj, results, timeout);
                }
                else
                {
                    WalkV2(endpoint, startOidObj, results, timeout);
                }

                foreach (var variable in results)
                {
                    DisplayVariable(variable);
                    count++;
                }

                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"Total OIDs found: {count}");
            }
            catch (Lextm.SharpSnmpLib.Messaging.TimeoutException)
            {
                Console.WriteLine($"\nError: Request timeout - no response from {_options.Host}");
                if (count > 0)
                    Console.WriteLine($"Total OIDs found before timeout: {count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError during walk: {ex.Message}");
                if (count > 0)
                    Console.WriteLine($"Total OIDs found before error: {count}");
            }
        });
    }

    private void WalkV2(IPEndPoint endpoint, ObjectIdentifier oid, IList<Variable> results, int timeout)
    {
        Messenger.Walk(
            VersionCode.V2,
            endpoint,
            new OctetString(_options.Community),
            oid,
            results,
            timeout,
            WalkMode.WithinSubtree);
    }

    private void WalkV3(IPEndPoint endpoint, ObjectIdentifier oid, IList<Variable> results, int timeout)
    {
        if (string.IsNullOrEmpty(_options.Username) || string.IsNullOrEmpty(_options.Password))
        {
            throw new InvalidOperationException("Username and password are required for SNMPv3");
        }

        var discovery = Messenger.GetNextDiscovery(SnmpType.GetBulkRequestPdu);
        var report = discovery.GetResponse(timeout, endpoint);

        var auth = new MD5AuthenticationProvider(new OctetString(_options.Password));
        var priv = new DESPrivacyProvider(new OctetString(_options.Password), auth);

        Messenger.BulkWalk(
            VersionCode.V3,
            endpoint,
            new OctetString(_options.Username),
            OctetString.Empty, // context name
            oid,
            results,
            timeout,
            10, // max repetitions
            WalkMode.WithinSubtree,
            priv,
            report);
    }

    private void DisplayVariable(Variable variable)
    {
        var (value, type) = SnmpValueFormatter.FormatSnmpData(variable.Data, variable.Id.ToString());

        Console.WriteLine($"{variable.Id}");
        Console.WriteLine($"  OID: {variable.Id}");
        Console.WriteLine($"  Value: {value} [{type}]");
        Console.WriteLine();
    }
}
