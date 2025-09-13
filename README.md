# NVMe-oF Kernel Driver Management

The application serves as a control interface to a proposed kernel-mode [NVMe-oF](https://en.wikipedia.org/wiki/NVM_Express#NVMe-oF) driver for Windows NT 10.0+ (**Windows 10** &amp; **Windows 11**). A driver mockup for testing is provided, however this only delivers the minimum viable product; only serving static data, with no actual NVMe functionality. Nevertheless, it allows for full usability of the management application.

The user interface is available in English and Czech, the language is set automatically based on the system's default display language. The interface is split into multiple views, selectable from the side navigation menu. The user is expected to have a basic familiarity with NVMe-oF and its concepts (NQN, network addresses, transport types, etc.).

## UI View Description
### Connected Disks
The first (and default) view is the *Connected Disks*. This view allows the user to see all their configured remote disks and their connection status (Connected / Disconnected / Connecting) at a glance. It also allows the user to view details about each disk, including network connection information, the connection's internal UUID or the local and remote storage paths (NT Device Path and NQN).
A refresh button is also available, which forces the application to reload all information from the driver.

### Disk Setup
The *Disk Setup* page is meant for basic connection management, allowing the user to add new connections or remove and modify existing ones. The reload button, again, forces the application to reload all information from the driver. When deleting a connection, the user is first prompted with details about the specific disk about to be removed, and upon confirmation the disk is staged for deletion.  
At this point, there are *unsaved changes* on the current page. If the user tries to leave this page, they are reminded about the unsaved changes and can either discard them and leave, or stay on the page and may choose save the changes. A button is also present to discard the changes, which rolls back the view to the last saved changes.  
Adding and editing disks transfers the user to a separate page meant exactly for this application.

### Disk Editing
Adding and editing disks works in a similar way (when editing, the current settings are prefilled into the editing fields). In both cases, the user is reminded about any potentially unsaved changes upon trying to exit the page prior to saving the connection details. A basic data validation scheme is employed, ensuring the user cannot pass any obviously invalid data to the kernel driver (such as an empty network address).  
Upon saving the new connection, the driver is immediately notified about the changes and can begin establishing the actual NVMe-oF connection.

### Statistics
The *Statistics* page displays basic statistics about the driver's performance, such as the network packet throughput, average device request size, or total data transferred through the driver. A refresh button is once again provided for tracking the value changes over time.

### Quick Connection
The *Quick Connection* page is a simple (essentially) one-click solution for NVMe discovery. The user can fill in the Discovery Controller network details, and upon successful connection can choose any of the discovered remote storages for connection. In basic view, only the NQN is displayed, however a detailed view of the remote network parameters is available. Upon clicking the Add button next to a desired remote, the driver is once again immediately notified to begin connecting to it.

### Settings
The *Settings* page allows the user to control some parameters of the kernel driver, mainly its Host NQN used to connect to the remote disks. It also allows the user to start the driver service if it is not running, although this requires administrator privileges.

## Error Handling
The application can detect the existence of its associated kernel driver, even if the device itself is disabled. In that case, the user is prompted to go to *Settings* and start the driver service. This feature is however very experimental, as the driver does not seem to get initialized correctly if it was previously manually disabled through its virtual device.

If the driver is not present at all, the user is notified about this fact, and the application exits, as there really isn't anything to do without the associated driver.

Any error originated at runtime (in a form of an otherwise uncaught C# exception) is caught and displayed to the user as an error notification (along with the stacktrace for debugging purposes).

# Technical Details
The application is written in C# (.NET 9 and .NET Standard 2.0). It is split in 2 parts, the actual UI application (utilizing WinUI 3 and Windows Community Toolkit) and the kernel interface library. The kernel interface library communicates with the driver using `DeviceIoControl` (`DllImport` from `kernel32.dll`). Due to the nature of the driver, a single IO Control Code is utilized (`IOCTL_MINIPORT_PROCESS_SERVICE_IRP`) and all application requests are encoded into the request buffer.  
Since the response buffers are allocated by the interface library, the driver is, in many cases, first queried about the data size, and then about the actual data (especially in the case of strings or arrays of objects). The individual application requests (and expected responses) are documented [in a separate file](kernel-api.md).

# Driver Mockup for Testing
The provided driver mockup is a simple WDF driver, which creates a virtual device used for the IO Control communication (available on the path `\\.\NvmeOfController`). Note that this is just a user-accessible symbolic link to the actual WDF-managed device file. Under **Device Manager**, the device is visible under System devices as `NVMe-oF_MockDriver Device`.

The driver stores all configuration in the Windows Registry, under its Software key (the actual key is, again, WDF-managed, but easily accessible through (for example) **System Informer**). Driver settings (such as the NQN) are stored under the `Settings` key and user connections under the `Connections` key. Every connection has its own UUID key, which then stores the actual connection information. The supplied statistics information are fully hard-coded and cannot be changed.