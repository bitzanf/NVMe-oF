using KernelInterface;

namespace ManagementApp.Models;

internal class DiskConnectionModel
{
    public DiskDescriptor Descriptor { get; set; }
    public ConnectionStatus ConnectionStatus { get; set; }
}