// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#include "WindowsTransport.h"

#include <filesystem>
#include <print>
#include <string>

#include "Shlobj.h"

#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Shell32.lib")

namespace
{
    struct TaskMemDeleter
    {
        void operator()(auto p) const noexcept { CoTaskMemFree(p); }
    };


    bool file_exists(const std::filesystem::path& path) noexcept
    {
        std::error_code ec;
        if (std::filesystem::exists(path, ec)) { return true; }
        // Error on unix sockets - https://github.com/microsoft/STL/issues/4077
        return ec.value() == ERROR_CANT_ACCESS_FILE;
    }

    std::expected<std::filesystem::path, std::string> GetLocalAppDataPath() noexcept
    {
        std::unique_ptr<wchar_t, TaskMemDeleter> buffer;
        if (const auto hr = SHGetKnownFolderPath(FOLDERID_LocalAppData, KF_FLAG_DEFAULT, nullptr, std::out_ptr(buffer));
            FAILED(hr))
        {
            return std::unexpected{std::format("SHGetKnownFolderPath failed with HRESULT {}", hr)};
        }
        return std::filesystem::path{buffer.get()};
    }
}

std::expected<std::unique_ptr<Transport>, std::string> Transport::Open() noexcept
{
    return WindowsTransport::Open();
}

std::expected<std::unique_ptr<Transport>, std::string> WindowsTransport::Open() noexcept
{
    const auto localAppDataRoot = GetLocalAppDataPath();
    if (!localAppDataRoot)
    {
        return std::unexpected{localAppDataRoot.error()};
    }
    const auto path = Transport::GetSocketPath(*localAppDataRoot);
    if (!path)
    {
        return std::unexpected{path.error()};
    }

    if (!file_exists(*path))
    {
        return std::unexpected{std::format("Socket `{}` does not exist", path->string())};
    }

    // Initialize Winsock once; if another part already initialized, it's fine
    WSADATA wsaData{};
    const auto wsa = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (wsa != 0)
    {
        return std::unexpected{std::format("WSAStartup failed: {}", wsa)};
    }

    unique_socket s{socket(AF_UNIX, SOCK_STREAM, 0)};
    if (!s.valid())
    {
        const auto err = WSAGetLastError();
        WSACleanup();
        return std::unexpected{std::format("socket(AF_UNIX) failed: {}", err)};
    }

    sockaddr_un addr{};
    addr.sun_family = AF_UNIX;
    const auto pathString = path->string();
    strncpy_s(addr.sun_path, pathString.data(), pathString.size());

    // Compute address length: family + path including NUL
    const int addrlen = static_cast<int>(offsetof(sockaddr_un, sun_path) + strlen(addr.sun_path) + 1);
    if (connect(s, reinterpret_cast<sockaddr*>(&addr), addrlen) == SOCKET_ERROR)
    {
        const auto err = WSAGetLastError();
        closesocket(s);
        WSACleanup();
        return std::unexpected{std::format("connect('{}') failed: {}", pathString, err)};
    }

    std::println("Connected to unix domain socket: {}", pathString);
    return std::unique_ptr<WindowsTransport>{new WindowsTransport(std::move(s))};
}

std::expected<size_t, std::string> WindowsTransport::Read(void* buffer, const size_t bufferSize) noexcept
{
    if (!mSocket.valid())
    {
        return std::unexpected{"Socket is not connected"};
    }
    // Read exactly bufferSize bytes unless error/closed
    size_t total = 0;
    char* out = static_cast<char*>(buffer);
    while (total < bufferSize)
    {
        const int toRead = static_cast<int>(bufferSize - total);
        const int ret = recv(mSocket.get(), out + total, toRead, 0);
        if (ret == SOCKET_ERROR)
        {
            const auto err = WSAGetLastError();
            return std::unexpected{std::format("recv failed: {}", err)};
        }
        if (ret == 0)
        {
            return std::unexpected{"socket closed by peer"};
        }
        total += static_cast<size_t>(ret);
    }
    return total;
}

WindowsTransport::unique_socket::unique_socket(unique_socket&& other) noexcept : mSocket(
    std::exchange(other.mSocket, INVALID_SOCKET))
{
}

WindowsTransport::unique_socket& WindowsTransport::unique_socket::operator=(unique_socket&& other) noexcept
{
    if (this != &other)
    {
        if (mSocket != INVALID_SOCKET) { ::closesocket(mSocket); }
        mSocket = std::exchange(other.mSocket, INVALID_SOCKET);
    }
    return *this;
}

SOCKET WindowsTransport::unique_socket::release() noexcept
{
    return std::exchange(mSocket, INVALID_SOCKET);
}

void WindowsTransport::unique_socket::reset(const SOCKET socket) noexcept
{
    if (mSocket != INVALID_SOCKET) { ::closesocket(mSocket); }
    mSocket = socket ? socket : INVALID_SOCKET;
}

WindowsTransport::WindowsTransport(unique_socket s) noexcept : mSocket(std::move(s))
{
}

WindowsTransport::~WindowsTransport()
{
    WSACleanup();
}
