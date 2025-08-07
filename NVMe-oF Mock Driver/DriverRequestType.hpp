#pragma once

enum class DriverRequestType : int  // NOLINT(performance-enum-size)
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
};