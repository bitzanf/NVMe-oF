// ReSharper disable CppClangTidyClangDiagnosticExtraSemiStmt
#include "DTOs.hpp"

#define DTO_OFFSET_CHECK \
    if (buffer.Length <= offset) { \
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid offset (points past buffer)\n")); \
        return 0; \
    }

#define DTO_COMMON_HEADER \
    DTO_OFFSET_CHECK \
    auto [bfrData, bfrLength] = buffer; \
    bfrLength -= offset; \
    bfrData += offset;

namespace NvmeOFMockDriver::DTO {
    size_t WriteString(Span<BYTE> buffer, const UNICODE_STRING& string, const size_t offset) {
        DTO_COMMON_HEADER;

        const UINT32 len = string.Length;

        if (bfrLength < sizeof(len) + len) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small)\n"));
            return 0;
        }

        memcpy(bfrData, &len, sizeof(len));
        bfrData += sizeof(len);

        memcpy(bfrData, string.Buffer, len);
        return sizeof(len) + len;
    }

    size_t ReadString(UNICODE_STRING& string, const Span<BYTE> buffer, const size_t offset) {
        DTO_COMMON_HEADER;

        UINT32 len;
        if (bfrLength < sizeof(len)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small)\n"));
            return 0;
        }

        memcpy(&len, bfrData, sizeof(len));
        if (len >= UINT16_MAX) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (string too long)\n"));
            return 0;
        }

        if (len > bfrLength - sizeof(len)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small for given string length)\n"));
            return 0;
        }

        if (len > string.MaximumLength) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " String is of insufficient size for the given data\n"));
            return 0;
        }

        const auto strBytes = min(len, string.MaximumLength);

        memcpy(string.Buffer, bfrData + sizeof(len), strBytes);
        string.Length = static_cast<USHORT>(strBytes);

        return strBytes + sizeof(len);
    }

    size_t InitStringFromBuffer(UNICODE_STRING& string, const Span<BYTE> buffer, const size_t offset) {
        DTO_COMMON_HEADER

        UINT32 len;
        if (bfrLength < sizeof(len)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small)\n"));
            return 0;
        }

        memcpy(&len, bfrData, sizeof(len));
        if (len >= UINT16_MAX) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid string length (too long)\n"));
            return 0;
        }

        string.Buffer = reinterpret_cast<PWSTR>(bfrData + sizeof(len));
        string.Length = string.MaximumLength = static_cast<USHORT>(len) * sizeof(WCHAR);

        return sizeof(len) + string.Length;
    }

    size_t NetworkConnection::Read(NetworkConnection& descriptor, const Span<BYTE> buffer, const size_t offset) {
        DTO_COMMON_HEADER;

        if (bfrLength < sizeof(TransportType) + sizeof(AddressFamily) + sizeof(TransportServiceId)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small)\n"));
            return 0;
        }

        memcpy(&descriptor.TransportType, bfrData, sizeof(TransportType));
        size_t len = sizeof(TransportType);

        memcpy(&descriptor.AddressFamily, bfrData + len, sizeof(AddressFamily));
        len += sizeof(AddressFamily);

        memcpy(&descriptor.TransportServiceId, bfrData + len, sizeof(TransportServiceId));
        len += sizeof(TransportServiceId);

        const auto strLen = InitStringFromBuffer(descriptor.TransportAddress, buffer, offset + len);
        if (strLen == 0) return 0;

        return len + strLen;
    }

    size_t NetworkConnection::Write(Span<BYTE> buffer, const size_t offset) const {
        DTO_COMMON_HEADER;

        if (bfrLength < sizeof(TransportType) + sizeof(AddressFamily) + sizeof(TransportServiceId)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small)\n"));
            return 0;
        }

        memcpy(bfrData, &TransportType, sizeof(TransportType));
        size_t len = sizeof(TransportType);

        memcpy(bfrData + len, &AddressFamily, sizeof(AddressFamily));
        len += sizeof(AddressFamily);

        memcpy(bfrData + len, &TransportServiceId, sizeof(TransportServiceId));
        len += sizeof(TransportServiceId);

        const auto strLen = WriteString(buffer, TransportAddress, offset + len);
        if (strLen == 0) return 0;

        return len + strLen;
    }

    size_t DiskDescriptor::Read(DiskDescriptor& descriptor, const Span<BYTE> buffer, const size_t offset) {
        DTO_OFFSET_CHECK;

        const auto size = NetworkConnection::Read(descriptor.NetworkConnection, buffer, offset);

        if (buffer.Length <= offset + size) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid offset (points past buffer)\n"));
            return 0;
        }

        const auto strLen = InitStringFromBuffer(descriptor.Nqn, buffer, offset + size);
        if (strLen == 0) return 0;

        return size + strLen;
    }

    size_t DiskDescriptor::Write(Span<BYTE> buffer, const size_t offset) const {
        DTO_OFFSET_CHECK;

        const auto netLen = NetworkConnection.Write(buffer, offset);
        if (netLen == 0) return 0;

        const auto nqnLen = WriteString(buffer, Nqn, offset + netLen);
        if (nqnLen == 0) return 0;

        return netLen + nqnLen;
    }
}
