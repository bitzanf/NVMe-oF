using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable once CheckNamespace
namespace KernelInterface.Interop
{
    // https://www.pinvoke.net/default.aspx/Enums.ACCESS_MASK
    [Flags]
    internal enum AccessMask : uint
    {
        Delete = 0x00010000,
        ReadControl = 0x00020000,
        WriteDac = 0x00040000,
        WriteOwner = 0x00080000,
        Synchronize = 0x00100000,

        StandardRightsRequired = 0x000F0000,

        StandardRightsRead = 0x00020000,
        StandardRightsWrite = 0x00020000,
        StandardRightsExecute = 0x00020000,

        StandardRightsAll = 0x001F0000,

        SpecificRightsAll = 0x0000FFFF,

        AccessSystemSecurity = 0x01000000,

        MaximumAllowed = 0x02000000,

        GenericRead = 0x80000000,
        GenericWrite = 0x40000000,
        GenericExecute = 0x20000000,
        GenericAll = 0x10000000,

        DesktopReadObjects = 0x00000001,
        DesktopCreateWindow = 0x00000002,
        DesktopCreateMenu = 0x00000004,
        DesktopHookControl = 0x00000008,
        DesktopJournalRecord = 0x00000010,
        DesktopJournalPlayback = 0x00000020,
        DesktopEnumerate = 0x00000040,
        DesktopWriteObjects = 0x00000080,
        DesktopSwitchDesktop = 0x00000100,

        WinStaEnumDesktops = 0x00000001,
        WinStaReadAttributes = 0x00000002,
        WinStaAccessClipboard = 0x00000004,
        WinStaCreateDesktop = 0x00000008,
        WinStaWriteAttributes = 0x00000010,
        WinStaAccessGlobalAtoms = 0x00000020,
        WinStaExitWindows = 0x00000040,
        WinStaEnumerate = 0x00000100,
        WinStaReadScreen = 0x00000200,

        WinStaAllAccess = 0x0000037F
    }

    [Flags]
    internal enum FileShareMode : uint
    {
        None = 0x00000000,
        Delete = 0x00000004,
        Read = 0x00000001,
        Write = 0x00000002,
    }

    internal enum FileCreationDisposition : uint
    {
        CreateNew = 1U,
        CreateAlways = 2U,
        OpenExisting = 3U,
        OpenAlways = 4U,
        TruncateExisting = 5U,
    }

    [Flags]
    internal enum FileFlagsAndAttributes : uint
    {
        FileAttributeReadonly = 0x00000001,
        FileAttributeHidden = 0x00000002,
        FileAttributeSystem = 0x00000004,
        FileAttributeDirectory = 0x00000010,
        FileAttributeArchive = 0x00000020,
        FileAttributeDevice = 0x00000040,
        FileAttributeNormal = 0x00000080,
        FileAttributeTemporary = 0x00000100,
        FileAttributeSparseFile = 0x00000200,
        FileAttributeReparsePoint = 0x00000400,
        FileAttributeCompressed = 0x00000800,
        FileAttributeOffline = 0x00001000,
        FileAttributeNotContentIndexed = 0x00002000,
        FileAttributeEncrypted = 0x00004000,
        FileAttributeIntegrityStream = 0x00008000,
        FileAttributeVirtual = 0x00010000,
        FileAttributeNoScrubData = 0x00020000,
        FileAttributeEa = 0x00040000,
        FileAttributePinned = 0x00080000,
        FileAttributeUnpinned = 0x00100000,
        FileAttributeRecallOnOpen = 0x00040000,
        FileAttributeRecallOnDataAccess = 0x00400000,
        FileFlagWriteThrough = 0x80000000,
        FileFlagOverlapped = 0x40000000,
        FileFlagNoBuffering = 0x20000000,
        FileFlagRandomAccess = 0x10000000,
        FileFlagSequentialScan = 0x08000000,
        FileFlagDeleteOnClose = 0x04000000,
        FileFlagBackupSemantics = 0x02000000,
        FileFlagPosixSemantics = 0x01000000,
        FileFlagSessionAware = 0x00800000,
        FileFlagOpenReparsePoint = 0x00200000,
        FileFlagOpenNoRecall = 0x00100000,
        FileFlagFirstPipeInstance = 0x00080000,
        PipeAccessDuplex = 0x00000003,
        PipeAccessInbound = 0x00000001,
        PipeAccessOutbound = 0x00000002,
        SecurityAnonymous = 0x00000000,
        SecurityIdentification = 0x00010000,
        SecurityImpersonation = 0x00020000,
        SecurityDelegation = 0x00030000,
        SecurityContextTracking = 0x00040000,
        SecurityEffectiveOnly = 0x00080000,
        SecuritySqosPresent = 0x00100000,
        SecurityValidSqosFlags = 0x001F0000,
    }

    [Flags]
    internal enum FormatMessageFlags : uint
    {
        AllocateBuffer = 0x00000100,
        IgnoreInserts = 0x00000200,
        FromSystem = 0x00001000,
        ArgumentArray = 0x00002000,
        FromHModule = 0x00000800,
        FromString = 0x00000400
    }

    internal static class Win32Interop
    {
        // https://www.pinvoke.dev/kernel32/createfile
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
            [MarshalAs(UnmanagedType.LPWStr)] string fileName,
            AccessMask desiredAccess,
            FileShareMode shareMode,
            IntPtr securityAttributes,
            FileCreationDisposition creationDisposition,
            FileFlagsAndAttributes flagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool DeviceIoControl(
            SafeFileHandle handle,
            uint ioControlCode,
            IntPtr inBuffer,
            uint inBufferSize,
            IntPtr outBuffer,
            uint outBufferSize,
            out uint bytesReturned,
            IntPtr overlapped
        );

        [DllImport("kernel32.dll", EntryPoint = "FormatMessageW", SetLastError = true)]
        internal static extern uint FormatMessage(
            FormatMessageFlags flags,
            IntPtr source,
            uint messageId,
            uint languageId,
            ref IntPtr buffer,
            uint bufferSize,
            string[] arguments
        );

        // https://pinvoke.net/default.aspx/kernel32/FormatMessage.html
        internal static string FormatWin32Error(int err)
        {
            IntPtr lpMsgBuf = IntPtr.Zero;

            uint dwChars = FormatMessage(
                FormatMessageFlags.AllocateBuffer | FormatMessageFlags.FromSystem | FormatMessageFlags.IgnoreInserts,
                IntPtr.Zero,
                (uint)err,
                0, // Default language
                ref lpMsgBuf,
                0,
                null
            );

            if (dwChars == 0)
            {
                // TODO Handle the error.
                int le = Marshal.GetLastWin32Error();
                return null;
            }

            string sRet = Marshal.PtrToStringUni(lpMsgBuf, (int) dwChars);

            // Free the buffer.
            Marshal.FreeHGlobal(lpMsgBuf);
            return sRet;
        }
    }
}
