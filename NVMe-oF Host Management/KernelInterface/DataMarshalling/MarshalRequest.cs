using System;
using System.IO;
using System.Text;

namespace KernelInterface.DataMarshalling
{
    internal static class MarshalRequest
    {
        /// <summary>
        /// i32 RequestType <br />
        /// &lt;NetworkInfo&gt; <br />
        /// &lt;String&gt; Nqn
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public static MemoryStream AddConnection(DiskDescriptor descriptor)
            => StreamWrapper(DriverRequestType.AddConnection, writer => Write(writer, descriptor));

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;Guid&gt;
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public static MemoryStream RemoveConnection(Guid connectionId)
            => StreamWrapper(DriverRequestType.RemoveConnection, writer => Write(writer, connectionId));

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;Guid&gt;
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public static MemoryStream GetConnectionStatus(Guid connectionId)
            => StreamWrapper(DriverRequestType.GetConnectionStatus, writer => Write(writer, connectionId));

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetDriverStatistics()
            => StreamWrapper(DriverRequestType.GetStatistics, null);

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;Guid&gt; <br />
        /// &lt;Descriptor&gt;
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static MemoryStream ModifyConnection(DiskDescriptor descriptor)
            => StreamWrapper(DriverRequestType.ModifyConnection, writer =>
            {
                if (descriptor.Guid == Guid.Empty) throw new InvalidOperationException("Modified Guid must be set!");

                Write(writer, descriptor.Guid);
                Write(writer, descriptor);
            });

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;Guid&gt;
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public static MemoryStream GetConnectionSize(Guid connectionId)
            => StreamWrapper(DriverRequestType.GetConnectionSize, writer => Write(writer, connectionId));

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;Guid&gt;
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public static MemoryStream GetConnection(Guid connectionId)
            => StreamWrapper(DriverRequestType.GetConnection, writer => Write(writer, connectionId));

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetAllConnectionsSize()
            => StreamWrapper(DriverRequestType.GetAllConnectionsSize, null);

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetAllConnections()
            => StreamWrapper(DriverRequestType.GetAllConnections, null);

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;NetworkConnection&gt;
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public static MemoryStream DiscoveryRequest(NetworkConnection network)
            => StreamWrapper(DriverRequestType.DiscoveryRequest, writer => Write(writer, network));

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetDiscoveryResponseSize()
            => StreamWrapper(DriverRequestType.GetDiscoveryResponseSize, null);

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetDiscoveryResponse()
            => StreamWrapper(DriverRequestType.GetDiscoveryResponse, null);

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetHostNqn()
            => StreamWrapper(DriverRequestType.GetHostNqn, null);

        /// <summary>
        /// i32 RequestType
        /// </summary>
        /// <returns></returns>
        public static MemoryStream GetHostNqnSize()
            => StreamWrapper(DriverRequestType.GetHostNqnSize, null);

        /// <summary>
        /// i32 RequestType <br />
        /// &lt;String&gt; NQN
        /// </summary>
        /// <param name="nqn"></param>
        /// <returns></returns>
        public static MemoryStream SetHostNqn(string nqn)
            => StreamWrapper(DriverRequestType.SetHostNqn, writer => Write(writer, nqn));


        /// <summary>
        /// Writes the request ID and calls the actual writing callback, which generates the request-specific data
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private static MemoryStream StreamWrapper(DriverRequestType request, Action<BinaryWriter> callback)
        {
            var stream = new MemoryStream();
            BinaryWriter writer = null;

            try
            {
                writer = new BinaryWriter(stream, Encoding.Unicode, true);
                
                writer.Write((int)request);
                callback?.Invoke(writer);
            }
            finally
            {
                writer?.Dispose();
            }

            return stream;
        }

        /// <summary>
        /// i32 TransportType <br />
        /// i32 AddressFamily <br />
        /// u16 Port <br />
        /// &lt;String&gt; AddressStr
        /// </summary>
        /// <param name="network">the <see cref="NetworkConnection"/> to append</param>
        /// <param name="writer"></param>
        private static void Write(BinaryWriter writer, NetworkConnection network)
        {
            writer.Write((int)network.TransportType);
            writer.Write((int)network.AddressFamily);
            writer.Write(network.TransportServiceId);
            Write(writer, network.TransportAddress ?? string.Empty);
        }

        /// <summary>
        /// &lt;NetworkInfo&gt; <br />
        /// &lt;String&gt; Nqn
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="descriptor"></param>
        private static void Write(BinaryWriter writer, DiskDescriptor descriptor)
        {
            Write(writer, descriptor.NetworkConnection);
            Write(writer, descriptor.Nqn);
        }

        /// <summary>
        /// u8[16] Guid
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="guid"></param>
        private static void Write(BinaryWriter writer, Guid guid)
        {
            var guidBytes = guid.ToByteArray();
            writer.Write(guidBytes);
        }

        /// <summary>
        /// i32 Length <br />
        /// u16[Length] Data
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="str"></param>
        private static void Write(BinaryWriter writer, string str)
        {
            var bytes = Encoding.Unicode.GetBytes(str);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }
}