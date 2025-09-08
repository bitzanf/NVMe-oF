using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KernelInterface.DataMarshalling
{
    public static class MarshalResponse
    {
        /// <summary>
        /// f32 PacketsPerSecond <br />
        /// u32 AverageRequestSize <br />
        /// u64 TotalDataTransferred
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Statistics GetDriverStatistics(byte[] bytes)
            => StreamWrapper(bytes, reader => new Statistics
            {
                PacketsPerSecond = reader.ReadSingle(),
                AverageRequestSize = reader.ReadUInt32(),
                TotalDataTransferred = reader.ReadUInt64()
            });

        /// <summary>
        /// &lt;DiskDescriptor&gt;
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static DiskDescriptor GetConnection(byte[] bytes)
            => StreamWrapper(bytes, ReadDiskDescriptor);

        /// <summary>
        /// i32 Count <br />
        /// &lt;DiskDescriptor&gt; [$Count]
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static List<DiskDescriptor> GetAllConnections(byte[] bytes)
            => StreamWrapper(bytes, reader =>
            {
                int count = reader.ReadInt32();
                
                var connections = new List<DiskDescriptor>(count);
                for (int i = 0; i < count; i++) connections.Add(ReadDiskDescriptor(reader));

                return connections;
            });

        /// <summary>
        /// i32 Count <br />
        /// (&lt;NetworkConnection&gt;,&lt;String&gt; NQN) [$Count]
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static List<DiskDescriptor> GetDiscoveryResponse(byte[] bytes)
            => StreamWrapper(bytes, reader =>
            {
                int count = reader.ReadInt32();

                // Descriptor only filled with NetworkInfo and Nqn, since it's just a discovery response
                var connections = new List<DiskDescriptor>(count);
                for (int i = 0; i < count; i++)
                {
                    var connection = ReadNetworkConnection(reader);
                    var nqn = ReadString(reader);

                    var descriptor = new DiskDescriptor
                    {
                        NetworkConnection = connection,
                        Nqn = nqn
                    };

                    connections.Add(descriptor);
                }

                return connections;
            });

        /// <summary>
        /// &lt;String&gt;
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetHostNqn(byte[] bytes)
            => StreamWrapper(bytes, ReadString);

        /// <summary>
        /// Reads the specified object from the given byte array via the specified reading callback
        /// </summary>
        /// <typeparam name="T">Type to extract from the byte array</typeparam>
        /// <param name="bytes">Byte array returned from kernel</param>
        /// <param name="callback">Reading callback</param>
        /// <returns></returns>
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

        /// <summary>
        /// u8[16] Guid <br />
        /// &lt;NetworkConnection&gt; <br />
        /// &lt;String&gt; NQN <br />
        /// &lt;String&gt; NtObjectPath
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static DiskDescriptor ReadDiskDescriptor(BinaryReader reader)
        {
            var descriptor = new DiskDescriptor();

            var guidBytes = reader.ReadBytes(Marshal.SizeOf<Guid>());
            descriptor.Guid = new Guid(guidBytes);

            descriptor.NetworkConnection = ReadNetworkConnection(reader);
            descriptor.Nqn = ReadString(reader);
            descriptor.NtObjectPath = ReadString(reader);

            return descriptor;
        }

        /// <summary>
        /// i32 TransportType <br />
        /// i32 AddressFamily <br />
        /// u16 Port <br />
        /// &lt;String&gt; Address
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static NetworkConnection ReadNetworkConnection(BinaryReader reader)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var network = new NetworkConnection();

            // Ensure correct loading order
            network.TransportType = (TransportType)reader.ReadInt32();
            network.AddressFamily = (AddressFamily)reader.ReadInt32();
            network.TransportServiceId = reader.ReadUInt16();
            network.TransportAddress = ReadString(reader);

            return network;
        }

        /// <summary>
        /// Since <see cref="BinaryReader.ReadString"/> doesn't actually care about the stream's encoding (and reads 7-bit ASCII), we need this ugly hack
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static string ReadString(BinaryReader reader)
        {
            var nBytes = reader.ReadInt32();
            var chars = reader.ReadChars(nBytes / sizeof(char));
            return new string(chars);
        }
    }
}