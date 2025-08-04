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
    public sealed class IoControlAccess : IDriverControl, IDisposable
    {
        private readonly SafeFileHandle _handle;

        // http://www.ioctls.net/
        private const uint IoctlMiniPortProcessServiceIrp = 0x4d038;

        public string HostNqn { get; set; }

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

        public IntPtr GetRawHandle() => _handle.DangerousGetHandle();
        
        public List<DiskDescriptor> GetConfiguredConnections()
        {
            throw new NotImplementedException();
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
        {
            throw new NotImplementedException();
        }

        public ConnectionStatus GetConnectionStatus(Guid connectionId)
        {
            const int sizeExpected = sizeof(int);
            ConnectionStatus status = ConnectionStatus.Disconnected;

            RequestWrapper(
                sizeExpected,
                () => MarshalRequest.GetConnectionStatus(connectionId),
                bytes =>
                {
                    if (bytes.Length != sizeExpected) throw new Exception("GetConnectionStatus returned incorrect response length!");
                    var iStatus = BitConverter.ToInt32(bytes, 0);
                    status = (ConnectionStatus)iStatus;
                }
            );

            return status;
        }

        public DiskDescriptor GetConnection(Guid connectionId)
        {
            throw new NotImplementedException();
        }

        public Task<List<DiskDescriptor>> DiscoveryRequest(NetworkConnection network)
        {
            throw new NotImplementedException();
        }

        public Statistics GetDriverStatistics()
        {
            throw new NotImplementedException();
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
    }
}
