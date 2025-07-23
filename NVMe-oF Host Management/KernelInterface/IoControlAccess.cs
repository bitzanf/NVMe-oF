using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using KernelInterface.Interop;

namespace KernelInterface
{
    public class IoControlAccess : IDriverControl, IDisposable
    {
        private readonly SafeFileHandle _handle;

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

            var err = Marshal.GetLastWin32Error();

            if (err != 0) throw new Exception(Win32Interop.FormatWin32Error(err));
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
            throw new NotImplementedException();
        }

        public void RemoveConnection(Guid connectionId)
        {
            throw new NotImplementedException();
        }

        public void ModifyConnection(DiskDescriptor newDescriptor)
        {
            throw new NotImplementedException();
        }

        public ConnectionStatus GetConnectionStatus(Guid connectionId)
        {
            throw new NotImplementedException();
        }

        public Task<List<DiskDescriptor>> DiscoveryRequest(NetworkConnection network)
        {
            throw new NotImplementedException();
        }
    }
}
