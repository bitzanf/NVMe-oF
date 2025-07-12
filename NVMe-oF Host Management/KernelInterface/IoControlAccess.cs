using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using KernelInterface.Interop;

namespace KernelInterface
{
    public class IoControlAccess : IDriverControl, IDisposable
    {
        private readonly SafeFileHandle _handle;

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
    }
}
