using System;

namespace KernelInterface
{
    public class DiskDescriptor
    {
        // TODO: Fix ugly hack
        public Guid Guid { get; /*internal*/ set; }
        public NetworkConnection NetworkConnection { get; set; }
        public string Nqn { get; set; }
        public string NtObjectPath { get; /*internal*/ set; }

        // TODO: public constructor for new connections, no guid
        // TODO: internal constructor for driver loading, sets guid from marshalled data
    }
}