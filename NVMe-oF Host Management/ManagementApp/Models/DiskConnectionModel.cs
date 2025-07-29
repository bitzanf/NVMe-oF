using KernelInterface;

namespace ManagementApp.Models;

internal class DiskConnectionModel
{
    public DiskDescriptor Descriptor { get; init; } = new();
    public ConnectionStatus ConnectionStatus { get; set; }

    public DiskConnectionModel Clone() =>
        new()
        {
            ConnectionStatus = ConnectionStatus,
            Descriptor = Descriptor.Clone()
        };
}