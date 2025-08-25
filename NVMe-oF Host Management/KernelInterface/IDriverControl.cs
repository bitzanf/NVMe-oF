using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KernelInterface
{
    public interface IDriverControl : IDisposable
    {
        /// <summary>
        /// NVMe Qualified Name of the kernel interface (used remote-side for authentication)
        /// </summary>
        string HostNqn { get; set; }

        /// <summary>
        /// Acquire all configured connections
        /// </summary>
        /// <returns></returns>
        List<DiskDescriptor> GetConfiguredConnections();

        /// <summary>
        /// Add a new connection to the kernel driver, effectively mounting a new disk
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        Guid AddConnection(DiskDescriptor descriptor);

        /// <summary>
        /// Remove a configured connection with the given Guid
        /// </summary>
        /// <param name="connectionId"></param>
        void RemoveConnection(Guid connectionId);

        /// <summary>
        /// Using the descriptor's Guid, rewrite an existing connection's parameters
        /// </summary>
        /// <param name="newDescriptor"></param>
        void ModifyConnection(DiskDescriptor newDescriptor);

        /// <summary>
        /// Get the current status of a connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        ConnectionStatus GetConnectionStatus(Guid connectionId);

        /// <summary>
        /// Get a single connection by Guid
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        DiskDescriptor GetConnection(Guid connectionId);

        /// <summary>
        /// Ask the kernel to perform a service discovery on the given remote
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        Task<List<DiskDescriptor>> DiscoveryRequest(NetworkConnection network);

        /// <summary>
        /// Get current runtime statistics
        /// </summary>
        /// <returns></returns>
        Statistics GetDriverStatistics();
    }
}
