// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include "Transport.hpp"

#define WIN32_LEAN_AND_MEAN 1
#define NOMINMAX 1

#include <WinSock2.h>
#include <WS2tcpip.h>
#include <afunix.h>
#include <utility>

class WindowsTransport final : public Transport
{
public:
    WindowsTransport() = delete;
    ~WindowsTransport() override;
    
    static std::expected<std::unique_ptr<Transport>, std::string> Open() noexcept;
    std::expected<size_t, std::string> Read(void* buffer, size_t bufferSize) noexcept override;
private:
    class unique_socket {
    public:
        unique_socket() = default;
        explicit unique_socket(const SOCKET socket) noexcept : mSocket(socket ? socket : INVALID_SOCKET) {}
        ~unique_socket() noexcept { if (mSocket != INVALID_SOCKET) { ::closesocket(mSocket); mSocket = INVALID_SOCKET; } }

        unique_socket(const unique_socket&) = delete;
        unique_socket& operator=(const unique_socket&) = delete;

        unique_socket(unique_socket&& other) noexcept;

        unique_socket& operator=(unique_socket&& other) noexcept;

        [[nodiscard]] bool valid() const noexcept { return mSocket != INVALID_SOCKET; }
        [[nodiscard]] SOCKET get() const noexcept { return mSocket; }
        SOCKET release() noexcept;

        void reset(SOCKET socket = INVALID_SOCKET) noexcept;
        operator SOCKET() const noexcept { return mSocket; }

    private:
        SOCKET mSocket{INVALID_SOCKET};
    };

    explicit WindowsTransport(unique_socket s) noexcept;
private:
    unique_socket mSocket{};
};