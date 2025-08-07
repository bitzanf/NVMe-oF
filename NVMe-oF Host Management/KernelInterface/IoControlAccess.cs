using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KernelInterface.DataMarshalling;
using KernelInterface.Interop;

namespace KernelInterface
{
    public sealed class IoControlAccess : IDriverControl
    {
        private readonly SafeFileHandle _handle;

        // http://www.ioctls.net/
        private const uint IoctlMiniPortProcessServiceIrp = 0x4d038;

        public string HostNqn
        {
            get
            {
                int sizeExpected = 0;
                RequestWrapper(
                    sizeof(int),
                    MarshalRequest.GetHostNqnSize,
                    bytes => sizeExpected = BitConverter.ToInt32(bytes, 0)
                );

                CheckResponseLength(sizeof(int), sizeExpected, nameof(MarshalRequest.GetHostNqnSize));

                string nqn = string.Empty;
                RequestWrapper(
                    sizeExpected,
                    MarshalRequest.GetHostNqn,
                    bytes => nqn = MarshalResponse.GetHostNqn(bytes)
                );

                return nqn;
            }
            set => RequestWrapper(
                    0,
                    () => MarshalRequest.SetHostNqn(value),
                    null
                );
        }

        public IoControlAccess(string devicePath)
        {
            _handle = Win32Interop.CreateFile(
                devicePath,
                AccessMask.GenericRead | AccessMask.GenericWrite,
                FileShareMode.Read | FileShareMode.Write,
                IntPtr.Zero,
                FileCreationDisposition.OpenExisting,
                0,
                IntPtr.Zero
            );

            Win32Interop.ThrowIfLastWin32Error();
        }

        public void Dispose()
        {
            _handle?.Dispose();
        }

        public List<DiskDescriptor> GetConfiguredConnections()
        {
            int sizeExpected = 0;
            RequestWrapper(
                sizeof(int),
                MarshalRequest.GetAllConnectionsSize,
                bytes => sizeExpected = BitConverter.ToInt32(bytes, 0)
            );

            CheckResponseLength(sizeof(int), sizeExpected, nameof(MarshalRequest.GetAllConnectionsSize));

            List<DiskDescriptor> connections = null;
            RequestWrapper(
                sizeExpected,
                MarshalRequest.GetAllConnections,
                bytes => connections = MarshalResponse.GetAllConnections(bytes)
            );

            return connections;
        }

        public Guid AddConnection(DiskDescriptor descriptor)
        {
            int sizeExpected = Marshal.SizeOf<Guid>();
            Guid guid = Guid.Empty;

            RequestWrapper(
                sizeExpected,
                () => MarshalRequest.AddConnection(descriptor),
                bytes => guid = new Guid(bytes)
            );

            return guid;
        }

        public void RemoveConnection(Guid connectionId) =>
            RequestWrapper(0, () => MarshalRequest.RemoveConnection(connectionId), null);

        public void ModifyConnection(DiskDescriptor newDescriptor)
            => RequestWrapper(0, () => MarshalRequest.ModifyConnection(newDescriptor), null);

        public ConnectionStatus GetConnectionStatus(Guid connectionId)
        {
            const int sizeExpected = sizeof(int);
            ConnectionStatus status = ConnectionStatus.Disconnected;

            RequestWrapper(
                sizeExpected,
                () => MarshalRequest.GetConnectionStatus(connectionId),
                bytes =>
                {
                    CheckResponseLength(sizeExpected, bytes.Length, nameof(MarshalRequest.GetConnectionStatus));

                    var iStatus = BitConverter.ToInt32(bytes, 0);
                    status = (ConnectionStatus)iStatus;
                }
            );

            return status;
        }

        public DiskDescriptor GetConnection(Guid connectionId)
        {
            int sizeExpected = 0;
            RequestWrapper(
                sizeof(int),
                () => MarshalRequest.GetConnectionSize(connectionId),
                bytes => sizeExpected = BitConverter.ToInt32(bytes, 0)
            );

            if (sizeExpected == 0) throw new Exception($"No connection found for {connectionId:B}!");

            DiskDescriptor descriptor = null;
            RequestWrapper(
                sizeExpected,
                () => MarshalRequest.GetConnection(connectionId),
                bytes => descriptor = MarshalResponse.GetConnection(bytes)
            );

            return descriptor;
        }

        public Task<List<DiskDescriptor>> DiscoveryRequest(NetworkConnection network) => Task.Run(() =>
        {
            RequestWrapper(
                0,
                () => MarshalRequest.DiscoveryRequest(network),
                null
            );

            int sizeExpected = 0;
            RequestWrapper(
                sizeof(int),
                MarshalRequest.GetDiscoveryResponseSize,
                bytes => sizeExpected = BitConverter.ToInt32(bytes, 0)
            );

            CheckResponseLength(sizeof(int), sizeExpected, nameof(MarshalRequest.GetDiscoveryResponseSize));

            List<DiskDescriptor> connections = null;
            RequestWrapper(
                sizeExpected,
                MarshalRequest.GetDiscoveryResponse,
                bytes => connections = MarshalResponse.GetDiscoveryResponse(bytes)
            );

            return connections;
        });

        public Statistics GetDriverStatistics()
        {
            int sizeExpected = Marshal.SizeOf<Statistics>();
            var statistics = new Statistics();

            RequestWrapper(
                sizeExpected,
                MarshalRequest.GetDriverStatistics,
                bytes =>
                {
                    CheckResponseLength(sizeExpected, bytes.Length, nameof(MarshalRequest.GetDriverStatistics));

                    statistics = MarshalResponse.GetDriverStatistics(bytes);
                }
            );

            return statistics;
        }

        private void Ioctl(
            IntPtr inBuffer,
            uint inBufferSize,
            IntPtr outBuffer,
            uint outBufferSize,
            out uint bytesReturned
        )
        {
            var success = Win32Interop.DeviceIoControl(
                _handle,
                IoctlMiniPortProcessServiceIrp,
                inBuffer,
                inBufferSize,
                outBuffer,
                outBufferSize,
                out bytesReturned,
                IntPtr.Zero
            );

            if (success == 0) Win32Interop.ThrowIfLastWin32Error();
        }

        private void RequestWrapper(
            int outputBufferRequestedSize,
            Func<MemoryStream> makeRequest,
            Action<byte[]> processResponse
        )
        {
            if (makeRequest == null)
                throw new ArgumentException("Request preparation callback must not be null!", nameof(makeRequest));

            MemoryStream request = null;
            GCHandle requestBytesHandle;
            IntPtr responsePtr = IntPtr.Zero;

            // This allocation should probably outlive the GCHandle that is pinning it...
            // ReSharper disable once TooWideLocalVariableScope
            byte[] requestBytes;

            try
            {
                request = makeRequest();
                requestBytes = request.GetBuffer();
                requestBytesHandle = GCHandle.Alloc(requestBytes, GCHandleType.Pinned);

                if (outputBufferRequestedSize != 0)
                    responsePtr = Marshal.AllocHGlobal(outputBufferRequestedSize);

                Ioctl(
                    requestBytesHandle.AddrOfPinnedObject(),
                    checked((uint)request.Length),
                    responsePtr,
                    checked((uint)outputBufferRequestedSize),
                    out var bytesReturned
                );

                if (outputBufferRequestedSize == 0) return;
                
                var managedResponseArray = new byte[bytesReturned];
                Marshal.Copy(responsePtr, managedResponseArray, 0, checked((int)bytesReturned));

                processResponse?.Invoke(managedResponseArray);
            }
            finally
            {
                if (requestBytesHandle.IsAllocated) requestBytesHandle.Free();
                if (responsePtr != IntPtr.Zero) Marshal.FreeHGlobal(responsePtr);
                
                request?.Dispose();
            }
        }

        private static void CheckResponseLength(int expected, int actual, string requestName)
        {
            if (expected != actual) throw new Exception(
                    $"{requestName} returned incorrect response length (expected {expected}, was {actual})!"
                );
        }
    }
}
