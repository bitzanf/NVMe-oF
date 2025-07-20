using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementApp.Models;

internal class DiskConnectionModel
{
    public ConnectionStatusEnum ConnectionStatus { get; set; }

    public TransportTypeEnum TransportType { get; set; }

    public AddressFamilyEnum AddressFamily { get; set; }

    public short TransportServiceId { get; set; }

    public string TransportAddress { get; set; }

    public string Nqn { get; set; }

    public string NtObjectPath { get; set; }

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