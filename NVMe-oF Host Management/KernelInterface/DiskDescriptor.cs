using System;

namespace KernelInterface
{
    /// <summary>
    /// Describes kernel-side information about a connection
    /// </summary>
    public class DiskDescriptor
    {
        /// <summary>
        /// Unique identifier of a specific connection
        /// </summary>
        public Guid Guid { get; internal set; }

        /// <summary>
        /// Network connection information
        /// </summary>
        public NetworkConnection NetworkConnection { get; set; } = new NetworkConnection();

        /// <summary>
        /// NVMe Qualified Name of the remote connection
        /// </summary>
        public string Nqn { get; set; }

        /// <summary>
        /// NT Object path that this disk is mounted to
        /// </summary>
        public string NtObjectPath { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A deep copy of this object</returns>
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