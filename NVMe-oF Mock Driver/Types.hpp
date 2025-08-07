#define DRIVER_LOG_STR "Mock NVMe-oF Driver: "

#ifndef TYPES_HPP
#define TYPES_HPP

namespace NvmeOFMockDriver {
    template <typename T>
    struct Span {
        T* Data;
        size_t Length;
    };
}

bool CheckStatus(NTSTATUS status);

#endif
