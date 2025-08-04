using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KernelInterface
{
    public interface IDriverControl
    {
        string HostNqn { get; set; }

        List<DiskDescriptor> GetConfiguredConnections();
        Guid AddConnection(DiskDescriptor descriptor);
        void RemoveConnection(Guid connectionId);
        void ModifyConnection(DiskDescriptor newDescriptor);

        ConnectionStatus GetConnectionStatus(Guid connectionId);
        DiskDescriptor GetConnection(Guid connectionId);

        Task<List<DiskDescriptor>> DiscoveryRequest(NetworkConnection network);

        Statistics GetDriverStatistics();
    }
}
