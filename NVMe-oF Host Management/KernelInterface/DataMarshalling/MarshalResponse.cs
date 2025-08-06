using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KernelInterface.DataMarshalling
{
    public static class MarshalResponse
    {
        public static Statistics GetDriverStatistics(byte[] bytes)
            => StreamWrapper(bytes, reader => new Statistics
            {
                PacketsPerSecond = reader.ReadSingle(),
                AverageRequestSize = reader.ReadUInt32(),
                TotalDataTransferred = reader.ReadUInt64()
            });

        public static DiskDescriptor GetConnection(byte[] bytes)
            => StreamWrapper(bytes, ReadDiskDescriptor);

        public static List<DiskDescriptor> GetAllConnections(byte[] bytes)
            => StreamWrapper(bytes, reader =>
            {
                int count = reader.ReadInt32();
                
                var connections = new List<DiskDescriptor>(count);
                for (int i = 0; i < count; i++) connections.Add(ReadDiskDescriptor(reader));

                return connections;
            });

        public static List<DiskDescriptor> GetDiscoveryResponse(byte[] bytes)
            => StreamWrapper(bytes, reader =>
            {
                int count = reader.ReadInt32();

                // Descriptor only filled with NetworkInfo and Nqn, since it's just a discovery response
                var connections = new List<DiskDescriptor>(count);
                for (int i = 0; i < count; i++)
                {
                    var descriptor = new DiskDescriptor
                    {
                        NetworkConnection = ReadNetworkConnection(reader),
                        Nqn = reader.ReadString()
                    };

                    connections.Add(descriptor);
                }

                return connections;
            });

        public static string GetHostNqn(byte[] bytes)
            => StreamWrapper(bytes, reader => reader.ReadString());

        private static T StreamWrapper<T>(byte[] bytes, Func<BinaryReader, T> callback)
        {
            var stream = new MemoryStream(bytes);
            BinaryReader reader = null;

            try
            {
                reader = new BinaryReader(stream, Encoding.Unicode);
                return callback(reader);
            }
            finally
            {
                reader?.Dispose();
                stream?.Dispose();
            }
        }

        private static DiskDescriptor ReadDiskDescriptor(BinaryReader reader)
        {
            var descriptor = new DiskDescriptor();

            var guidBytes = reader.ReadBytes(Marshal.SizeOf<Guid>());
            descriptor.Guid = new Guid(guidBytes);

            descriptor.NetworkConnection = ReadNetworkConnection(reader);
            descriptor.Nqn = reader.ReadString();
            descriptor.NtObjectPath = reader.ReadString();

            return descriptor;
        }

        private static NetworkConnection ReadNetworkConnection(BinaryReader reader)
        {
            var network = new NetworkConnection
            {
                TransportType = (TransportType)reader.ReadInt32(),
                AddressFamily = (AddressFamily)reader.ReadInt32(),
                TransportServiceId = reader.ReadUInt16(),
                TransportAddress = reader.ReadString()
            };

            return network;
        }
    }
}