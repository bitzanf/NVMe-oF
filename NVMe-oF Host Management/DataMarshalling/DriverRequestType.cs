namespace DataMarshalling
{
    public enum DriverRequestType
    {
        None = 0,

        GetHostNqn,
        SetHostNqn,

        GetConnections,
        AddConnection,
        RemoveConnection,
        ModifyConnection,

        GetConnectionStatus,

        DiscoveryRequest,

        GetStatistics
    }
}