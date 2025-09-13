# Driver requests

## GetHostNqn
Id: **1**  
Request format:
```
i32 RequestType
```

Response format:
```
<String>
```

## SetHostNqn
Id: **2**  
Request format:
```
i32 RequestType
<String>
```

Response format:
```

```

## GetAllConnections
Id: **3**  
Request format:
```
i32 RequestType
```

Response format:
```
i32 Count
<DiskDescriptorRes> [$Count]
```

## AddConnection
Id: **4**  
Request format:
```
i32 RequestType
<DiskDescriptorRq>
```

Response format:
```
<Guid>
```

## RemoveConnection
Id: **5**  
Request format:
```
i32 RequestType
<Guid>
```

Response format:
```

```

## ModifyConnection
Id: **6**  
Request format:
```
i32 RequestType
<Guid>
<DiskDescriptorRq>
```

Response format:
```

```

## GetConnectionStatus
Id: **7**  
Request format:
```
i32 RequestType
<Guid>
```

Response format:
```
i32 ConnectionStatus
```

## GetConnection
Id: **8**  
Request format:
```
i32 RequestType
<Guid>
```

Response format:
```
<DiskDescriptorRes>
```

## DiscoveryRequest
Id: **9**  
Request format:
```
i32 RequestType
<NetworkConnection> DiscoveryController
```

Response format:
```

```

## GetDiscoveryResponse
Id: **10**  
Request format:
```
i32 RequestType
```

Response format:
```
i32 Count
<DiskDescriptorRq> [$Count]
```

## GetStatistics
Id: **11**  
Request format:
```
i32 RequestType
```

Response format:
```
f32 PacketsPerSecond
u32 AverageRequestSize
u64 TotalDataTransferred
```

## GetHostNqnSize
Id: **12**  
Request format:
```
i32 RequestType
```

Response format:
```
i32 RequiredByteCount
```

## GetConnectionSize
Id: **13**  
Request format:
```
i32 RequestType
<Guid>
```

Response format:
```
i32 RequiredByteCount
```

## GetAllConnectionsSize
Id: **14**  
Request format:
```
i32 RequestType
```

Response format:
```
i32 RequiredByteCount
```

## GetDiscoveryResponseSize
Id: **15**  
Request format:
```
i32 RequestType
```

Response format:
```
i32 RequiredByteCount
```

# Data types

Format description
```
u8 = Unsigned 8-bit integer
u16 = Unsigned 16-bit integer
i32 = Signed 32-bit integer
u32 = Unsigned 32-bit integer
u64 = Unsigned 64-bit integer
f32 = IEEE754 Single-precision floating point

<Type> = Compound data type description
<Type> [$Size] = Array of "Type" with "Size" elements; "Size" usually i32
```

## String
```
i32 Bytes
u16 [$Bytes / 2] CharacterData
```
> The string length is encoded as number of **bytes**, not **characters**. The characters are stored as UTF-16 code points, as is tradition on Windows.

## Guid
```
u8 [16] GuidBytes
```
> Guid (or UUID) is encoded as a 16-byte array of the raw UUID data.

## NetworkConnection
```
i32 TransportType
i32 AddressFamily
u16 Port
<String> Address
```

## DiskDescriptorRq
```
<NetworkConnection>
<String> Nqn
```
> For passing configuration to the driver. Also used for discovery responses.

## DiskDescriptorRes
```
<Guid>
<NetworkConnection>
<String> Nqn
<String> NtObjectPath
```
> For acquiring disk information from the driver

# Enums

## ConnectionStatus
| Status       | Value |
| ------------ | ----- |
| Disconnected | 0     |
| Connecting   | 1     |
| Connected    | 2     |

## TransportType
| Type | Value |
| ---- | ----- |
| Tcp  | 0     |
| Rdma | 1     |

## AddressFamily
| Family | Value |
| ------ | ----- |
| IPv4   | 0     |
| IPv6   | 1     |
