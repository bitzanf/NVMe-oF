#pragma once
#include <intsafe.h>

#include "Types.hpp"

namespace Concepts {
    namespace detail {
        template <typename, typename>
        constexpr bool IsSame = false;

        template <typename T>
        constexpr bool IsSame<T, T> = true;

        template <typename T1, typename T2>
        concept SameAsImpl = IsSame<T1, T2>;

    }
    template <typename T1, typename T2>
    concept SameAs = detail::SameAsImpl<T1, T2>&& detail::SameAsImpl<T2, T1>;

    template <typename T>
    concept Serializable = requires (const T t, Span<BYTE> buffer, size_t offset) {
        { t.Write(buffer, offset) } -> SameAs<size_t>;
        { t.GetRequiredBufferSize() } -> SameAs<size_t>;
    };
}

namespace DTO {
    size_t WriteString(Span<BYTE> buffer, const UNICODE_STRING& string, size_t offset = 0);
    size_t ReadString(UNICODE_STRING& string, Span<BYTE> buffer, size_t offset = 0);

    size_t InitStringFromBuffer(UNICODE_STRING& string, Span<BYTE> buffer, size_t offset = 0);

    struct NetworkConnection {
        TransportType TransportType;
        AddressFamily AddressFamily;
        UINT16 TransportServiceId;
        UNICODE_STRING TransportAddress;

        size_t Write(Span<BYTE> buffer, size_t offset = 0) const;
        size_t GetRequiredBufferSize() const;

        static size_t Read(NetworkConnection& descriptor, Span<BYTE> buffer, size_t offset = 0);
    };

    struct DiskDescriptor {
        GUID Uuid;
        NetworkConnection NetworkConnection;
        UNICODE_STRING Nqn, NtObjectPath;

        size_t Write(Span<BYTE> buffer, size_t offset = 0) const;
        size_t WriteFull(Span<BYTE> buffer, size_t offset = 0) const;
        size_t GetRequiredBufferSize() const;
        size_t GetRequiredBufferSizeFull() const;

        static size_t Read(DiskDescriptor& descriptor, Span<BYTE> buffer, size_t offset = 0);
    };

    template <Concepts::Serializable T>
    size_t WriteArray(Span<BYTE> buffer, T* array, const size_t count, const size_t offset = 0) {
        if (buffer.Length <= offset) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid offset (points past buffer)\n"));
            return 0;
        }

        if (count > INT32_MAX) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid array (too many items)\n"));
            return 0;
        }

        size_t requiredSize = sizeof(int);
        for (size_t i = 0; i < count; i++) requiredSize += array[i].GetRequiredBufferSize();

        if (buffer.Length - offset < requiredSize) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small for given array)\n"));
            return 0;
        }

        const int iCount = static_cast<int>(count);
        memcpy(buffer.Data + offset, &iCount, sizeof(int));
        size_t totalSize = sizeof(int);

        for (auto i = 0; i < iCount; i++) {
            auto written = array[i].Write(buffer, offset + totalSize);
            if (written == 0) return 0;
            totalSize += written;
        }

        return totalSize;
    }
}
