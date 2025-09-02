#pragma once
#include <intsafe.h>

#include "Types.hpp"

namespace NvmeOFMockDriver {
    namespace Concepts {
        namespace detail {
            template <typename, typename>
            constexpr bool IsSame = false;

            template <typename T>
            constexpr bool IsSame<T, T> = true;

            template <typename T1, typename T2>
            concept SameAsImpl = IsSame<T1, T2>;

            template <typename T1, typename T2>
            concept SameAs = SameAsImpl<T1, T2>&& SameAsImpl<T2, T1>;
        }

        template <typename T>
        concept Writable = requires (const T t, Span<BYTE> buffer, size_t offset) {
            { t.Write(buffer, offset) } -> detail::SameAs<size_t>;
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

            static size_t Read(NetworkConnection& descriptor, Span<BYTE> buffer, size_t offset = 0);
        };

        struct DiskDescriptor {
            NetworkConnection NetworkConnection;
            UNICODE_STRING Nqn;

            size_t Write(Span<BYTE> buffer, size_t offset = 0) const;

            static size_t Read(DiskDescriptor& descriptor, Span<BYTE> buffer, size_t offset = 0);
        };

        template <Concepts::Writable T>
        size_t WriteArray(Span<BYTE> buffer, T* array, const size_t count, const size_t offset = 0) {
            if (buffer.Length <= offset) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid offset (points past buffer)\n"));
                return 0;
            }

            if (buffer.Length - offset < sizeof(T) * count + sizeof(int)) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small for given item count)\n"));
                return 0;
            }

            if (count > INT32_MAX) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid array (too many items)\n"));
                return 0;
            }

            const int iCount = static_cast<int>(count);
            auto p = buffer.Data + offset;
            const auto pStart = p;

            memcpy(p, &iCount, sizeof(int));
            p += sizeof(int);

            for (auto i = 0; i < iCount; i++) {
                memcpy(p, &array[i], sizeof(T));
                p += sizeof(T);
            }

            return p - pStart;
        }
    }
}
