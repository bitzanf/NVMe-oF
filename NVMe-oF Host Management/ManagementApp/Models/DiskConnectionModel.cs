using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementApp.Models;

internal class DiskConnectionModel
{
    public required Guid Guid { get; init; }
    public required ConnectionStatusEnum ConnectionStatus { get; set; }
    public required TransportTypeEnum TransportType { get; set; }
    public required AddressFamilyEnum AddressFamily { get; set; }
    public required ushort TransportServiceId { get; set; }
    public required string TransportAddress { get; set; }
    public required string Nqn { get; set; }
    public required string NtObjectPath { get; set; }

    public enum ConnectionStatusEnum
    {
        Disconnected,
        Connecting,
        Connected
    }

    public enum TransportTypeEnum
    {
        Tcp,
        Rdma
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum AddressFamilyEnum
    {
        IPv4,
        IPv6
    }
}