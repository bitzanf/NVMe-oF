namespace KernelInterface
{
    public struct Statistics
    {
        public float PacketsPerSecond { get; set; }
        public uint AverageRequestSize { get; set; }
        public uint TotalDataTransferred { get; set; }
    }
}