namespace KernelInterface
{
    public struct Statistics
    {
        /// <summary>
        /// Average network throughput
        /// </summary>
        public float PacketsPerSecond { get; set; }

        /// <summary>
        /// Average data access request size
        /// </summary>
        public uint AverageRequestSize { get; set; }

        /// <summary>
        /// Total data transferred excluding service information (TCP arbitration etc.)
        /// </summary>
        public ulong TotalDataTransferred { get; set; }
    }
}