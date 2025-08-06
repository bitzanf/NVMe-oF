namespace KernelInterface
{
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