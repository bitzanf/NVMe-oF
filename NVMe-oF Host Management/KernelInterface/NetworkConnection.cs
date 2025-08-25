using System.Diagnostics.CodeAnalysis;

namespace KernelInterface
{
    /// <summary>
    /// Contains the network information necessary to connect to a remote
    /// </summary>
    public class NetworkConnection
    {
        /// <summary>
        /// Connection type (TCP / RDMA)
        /// </summary>
        public TransportType TransportType { get; set; }

        /// <summary>
        /// IP Address family
        /// </summary>
        public AddressFamily AddressFamily { get; set; }

        /// <summary>
        /// Network port
        /// </summary>
        public ushort TransportServiceId { get; set; }

        /// <summary>
        /// IP Address
        /// </summary>
        public string TransportAddress { get; set; }

        public NetworkConnection Clone() => (NetworkConnection)MemberwiseClone();
    }

    public enum TransportType : int
    {
        Tcp,
        Rdma
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum AddressFamily : int
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