using System.Runtime.InteropServices;
using KernelInterface;

namespace DataMarshalling.Requests
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AddConnection
    {
        public DriverRequestType RequestType;

        public TransportType
    }
}