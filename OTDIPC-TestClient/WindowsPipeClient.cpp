// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#include "WindowsPipeClient.h"

#include <OTD-IPC/NamedPipePath.h>
#include <print>

std::expected<std::unique_ptr<PipeClient>, std::string> PipeClient::Open() noexcept
{
    return WindowsPipeClient::Open();
}

std::expected<std::unique_ptr<PipeClient>, std::string> WindowsPipeClient::Open() noexcept
{
    winrt::file_handle connection{
        CreateFileW(
            OTDIPC::NamedPipePathW,
            GENERIC_READ,
            0,
            nullptr,
            OPEN_EXISTING,
            0,
            nullptr)
    };
    if (!connection)
    {
        return std::unexpected{std::format("Failed to open pipe: `{}` -> {}", OTDIPC::NamedPipePathA, GetLastError())};
    }
    std::println("Connected to pipe: {}", OTDIPC::NamedPipePathA);
    return std::unique_ptr<WindowsPipeClient>{new WindowsPipeClient(std::move(connection))};
}

std::expected<size_t, std::string> WindowsPipeClient::Read(void* buffer, const size_t bufferSize) noexcept
{
    DWORD bytesRead {};
    if (!ReadFile(mConnection.get(), buffer, static_cast<DWORD>(bufferSize), &bytesRead, nullptr))
    {
        return std::unexpected{std::format("Failed to read from pipe: {}", GetLastError())};
    }
    return static_cast<size_t>(bytesRead);
}

WindowsPipeClient::WindowsPipeClient(winrt::file_handle connection) noexcept : mConnection(std::move(connection))
{
}
