using Microsoft.Extensions.Configuration;
using SnmpWalking;
using SnmpWalking.Models;

// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var options = new SnmpOptions();
configuration.GetSection("SnmpOptions").Bind(options);

// Simple command-line parsing
if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
{
    Console.WriteLine("SNMP Walking and Monitoring Tool v1.0.0");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  snmpwalking walk [options] [oid]    Walk SNMP tree");
    Console.WriteLine("  snmpwalking monitor [options]        Monitor device");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --host <host>          Target host (default: 192.168.20.1)");
    Console.WriteLine("  -c, --community <string>   SNMP community (default: public)");
    Console.WriteLine("  -u, --username <string>    SNMPv3 username");
    Console.WriteLine("  -p, --password <string>    SNMPv3 password");
    Console.WriteLine("  --port <port>              SNMP port (default: 161)");
    Console.WriteLine("  --timeout <ms>             Timeout in ms (default: 5000)");
    Console.WriteLine("  --retry <count>            Retry attempts (default: 3)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  snmpwalking walk -h 192.168.1.1 -c public");
    Console.WriteLine("  snmpwalking monitor -u admin -p password");
    return 0;
}

if (args[0] == "--version" || args[0] == "-v")
{
    Console.WriteLine("SnmpWalking v1.0.0");
    return 0;
}

var command = args[0].ToLower();
string? oidArg = null;

// Parse arguments
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-h":
        case "--host":
            if (i + 1 < args.Length) options.Host = args[++i];
            break;
        case "-c":
        case "--community":
            if (i + 1 < args.Length) options.Community = args[++i];
            break;
        case "-u":
        case "--username":
            if (i + 1 < args.Length) options.Username = args[++i];
            break;
        case "-p":
        case "--password":
            if (i + 1 < args.Length) options.Password = args[++i];
            break;
        case "--port":
            if (i + 1 < args.Length) options.Port = int.Parse(args[++i]);
            break;
        case "--timeout":
            if (i + 1 < args.Length) options.TimeoutMs = int.Parse(args[++i]);
            break;
        case "--retry":
            if (i + 1 < args.Length) options.RetryCount = int.Parse(args[++i]);
            break;
        default:
            if (command == "walk" && !args[i].StartsWith("-"))
                oidArg = args[i];
            break;
    }
}

try
{
    switch (command)
    {
        case "walk":
            var walker = new SnmpWalker(options);
            await walker.WalkAsync(oidArg);
            break;
        case "monitor":
            var monitor = new SnmpMonitor(options);
            await monitor.MonitorAsync();
            break;
        default:
            Console.WriteLine($"Unknown command: {command}");
            Console.WriteLine("Use --help for usage information");
            return 1;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;
