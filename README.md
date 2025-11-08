# SNMP Walking

A modern C# .NET CLI tool for SNMP walking and network device monitoring.

## Features

- **SNMP Walk**: Walk SNMP trees starting at any OID
- **Device Monitoring**: Query system information and network interfaces
- **SNMPv2c Support**: Community-based authentication
- **SNMPv3 Support**: Secure authentication with MD5/DES
- **Configuration File**: Optional `appsettings.json` for defaults
- **Retry Logic**: Configurable retry attempts for failed operations
- **Smart Formatting**: Intelligent formatting of SNMP data types (MAC addresses, uptime, speeds, etc.)

## Requirements

- .NET 9.0 SDK
- Network access to SNMP-enabled devices

## Building

```bash
cd SnmpWalking
dotnet build
```

## Running

```bash
dotnet run --project SnmpWalking -- [command] [options]
```

Or build and run the executable directly:

```bash
dotnet build
./SnmpWalking/bin/Debug/net9.0/SnmpWalking [command] [options]
```

## Usage

### Help

```bash
dotnet run --project SnmpWalking -- --help
```

### Walk SNMP Tree

Walk the system MIB tree on a device:

```bash
dotnet run --project SnmpWalking -- walk --host 192.168.1.1 --community public
```

Walk a specific OID:

```bash
dotnet run --project SnmpWalking -- walk --host 192.168.1.1 --community public 1.3.6.1.2.1.2
```

### Monitor Device

Monitor device with SNMPv2c:

```bash
dotnet run --project SnmpWalking -- monitor --host 192.168.1.1 --community public
```

Monitor device with SNMPv3:

```bash
dotnet run --project SnmpWalking -- monitor --host 192.168.1.1 --username admin --password secret
```

### Options

- `-h, --host <host>` - Target host IP address (default: 192.168.20.1)
- `-c, --community <string>` - SNMP community string for SNMPv2c (default: public)
- `-u, --username <string>` - SNMPv3 username
- `-p, --password <string>` - SNMPv3 password
- `--port <port>` - SNMP port (default: 161)
- `--timeout <ms>` - Timeout in milliseconds (default: 5000)
- `--retry <count>` - Number of retry attempts (default: 3)

## Configuration

You can set defaults in `appsettings.json`:

```json
{
  "SnmpOptions": {
    "Host": "192.168.20.1",
    "Community": "public",
    "WalkOid": "1.3.6.1.2.1.1",
    "RetryCount": 3,
    "TimeoutMs": 5000,
    "Port": 161
  }
}
```

Command-line arguments override configuration file values.

## Project Structure

```
SnmpWalking/
├── Program.cs                    # CLI entry point
├── SnmpWalker.cs                 # SNMP walk operations
├── SnmpMonitor.cs                # Device monitoring operations
├── Models/
│   ├── SnmpOptions.cs            # Configuration model
│   └── InterfaceInfo.cs          # Interface data model
├── Formatters/
│   └── SnmpValueFormatter.cs     # SNMP data formatting utilities
└── appsettings.json              # Configuration file
```

## SNMP Library

This project uses **Lextm.SharpSnmpLib** (v12.5.7), a mature open-source SNMP library for .NET.

## Security Notes

**Warning**: This tool uses MD5 and DES for SNMPv3, which are considered deprecated and insecure by modern standards. These are used for compatibility with older SNMP devices. For production use, consider upgrading to SHA/AES if your devices support it.

The warnings during build about obsolete authentication providers are expected and can be safely ignored for compatibility purposes.

## License

MIT License
