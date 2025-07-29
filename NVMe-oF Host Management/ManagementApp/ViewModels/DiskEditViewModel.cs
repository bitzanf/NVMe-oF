using KernelInterface;
using ManagementApp.Models;
using System;
namespace ManagementApp.ViewModels;

internal partial class DiskEditViewModel(DiskConnectionModel? diskConnection) : ObservableBase
{
    private bool _hasChanges;

    public Guid Guid => Model.Descriptor.Guid;

    public string NtObjectPath => Model.Descriptor.NtObjectPath;

    public DiskConnectionModel Model { get; } = diskConnection ?? new();

    public bool HasChanges
    {
        get => _hasChanges;
        set => SetField( ref _hasChanges, value);
    }

    public string Nqn
    {
        get => Model.Descriptor.Nqn;
        set
        {
            Model.Descriptor.Nqn = value;
            HasChanges = true;
            OnPropertyChanged();
        }
    }

    public TransportType TransportType
    {
        get => Model.Descriptor.NetworkConnection.TransportType;
        set
        {
            Model.Descriptor.NetworkConnection.TransportType = value;
            HasChanges = true;
            OnPropertyChanged();
        }
    }

    public AddressFamily AddressFamily
    {
        get => Model.Descriptor.NetworkConnection.AddressFamily;
        set
        {
            Model.Descriptor.NetworkConnection.AddressFamily = value;
            HasChanges = true;
            OnPropertyChanged();
        }
    }

    public ushort TransportServiceId
    {
        get => Model.Descriptor.NetworkConnection.TransportServiceId;
        set
        {
            Model.Descriptor.NetworkConnection.TransportServiceId = value;
            HasChanges = true;
            OnPropertyChanged();
        }
    }

    public string TransportAddress
    {
        get => Model.Descriptor.NetworkConnection.TransportAddress;
        set
        {
            Model.Descriptor.NetworkConnection.TransportAddress = value;
            HasChanges = true;
            OnPropertyChanged();
        }
    }
}