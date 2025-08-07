using System;

namespace KernelInterface
{
    public class DiskDescriptor
    {
        public Guid Guid { get; internal set; }
        public NetworkConnection NetworkConnection { get; set; } = new NetworkConnection();
        public string Nqn { get; set; }
        public string NtObjectPath { get; internal set; }

        public DiskDescriptor Clone() =>
            new DiskDescriptor
            {
                Guid = Guid,
                NetworkConnection = NetworkConnection.Clone(),
                Nqn = Nqn,
                NtObjectPath = NtObjectPath
            };
    }
}