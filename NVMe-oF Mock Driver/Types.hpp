// ReSharper disable CppClangTidyCppcoreguidelinesMissingStdForward
// ReSharper disable CppClangTidyPerformanceEnumSize
#ifndef TYPES_HPP
#define TYPES_HPP

#include <ntdef.h>
#include <wdm.h>
#include <wdf.h>

constexpr UNICODE_STRING SETTINGS_KEY_PATH = RTL_CONSTANT_STRING(L"Settings");
constexpr UNICODE_STRING CONNECTIONS_KEY_PATH = RTL_CONSTANT_STRING(L"Connections");

#define DRIVER_LOG_STR "Mock NVMe-oF Driver: "
#define CHECK_STATUS(expr) { \
    NTSTATUS _temp_status = (expr); \
    if (!NT_SUCCESS(_temp_status)) { \
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR __FUNCTION__ " ERROR Status: 0x%x\n", _temp_status)); \
        return _temp_status; \
    }}


template <typename T>
struct Span {
    T* Data;
    size_t Length;

    size_t ByteLength() const { return Length * sizeof(T); }
};

struct FdoContext {
    WDFQUEUE Queue;

    // !! Owning Span
    Span<WCHAR> StringTempMemory;

    size_t DiskCount;

    [[nodiscard]] UNICODE_STRING MakeTempString() const;

    static NTSTATUS Init(FdoContext* context);
    static void Cleanup(WDFOBJECT object);
};

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(FdoContext, FdoGetContext);

namespace DTO {
    enum class TransportType : int
    {
        Tcp,
        Rdma
    };

    enum class AddressFamily : int
    {
        IPv4,
        IPv6
    };

    enum class ConnectionStatus : int
    {
        Disconnected,
        Connecting,
        Connected
    };
}

bool CheckStatus(NTSTATUS status);

namespace Utils {
    template <typename T>
    struct RemoveReference {
        using Type = T;
    };

    template <typename T>
    struct RemoveReference<T&> {
        using Type = T;
    };

    template <typename T>
    struct RemoveReference<T&&> {
        using Type = T;
    };

    template <typename T>
    using TRemoveReference = typename Utils::RemoveReference<T>::Type;

    template <typename T>
    auto&& Move(T&& obj) { return static_cast<TRemoveReference<T>&&>(obj); }

    template<typename T, size_t Size>
    constexpr size_t GetArrayLength(T(&)[Size]) { return Size; }
}

#endif
