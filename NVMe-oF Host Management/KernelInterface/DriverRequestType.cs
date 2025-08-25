namespace KernelInterface
{
    /// <summary>
    /// This enum must remain exactly the same as on the kernel side <br />
    /// Each value represents a specific action we request from the kernel
    /// </summary>
    internal enum DriverRequestType : int
    {
        None = 0,

        GetHostNqn,
        SetHostNqn,

        GetAllConnections,
        AddConnection,
        RemoveConnection,
        ModifyConnection,

        GetConnectionStatus,
        GetConnection,

        DiscoveryRequest,
        GetDiscoveryResponse,

        GetStatistics,

        GetHostNqnSize,
        GetConnectionSize,
        GetAllConnectionsSize,
        GetDiscoveryResponseSize
    }
}