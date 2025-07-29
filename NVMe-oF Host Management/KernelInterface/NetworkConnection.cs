using System.Diagnostics.CodeAnalysis;

namespace KernelInterface
{
    public class NetworkConnection
    {
        public TransportType TransportType { get; set; }
        public AddressFamily AddressFamily { get; set; }
        public ushort TransportServiceId { get; set; }
        public string TransportAddress { get; set; }

        public NetworkConnection Clone() => (NetworkConnection)MemberwiseClone();
    }

    public enum TransportType
    {
        Tcp,
        Rdma
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum AddressFamily
    {
        IPv4,
        IPv6
    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected
    }
}