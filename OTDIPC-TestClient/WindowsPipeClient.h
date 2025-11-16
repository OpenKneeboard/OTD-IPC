// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include "PipeClient.h"

#define WIN32_LEAN_AND_MEAN 1
#define NOMINMAX 1
#define UNICODE 1
#define _UNICODE 1

#include <Windows.h>
#include <Unknwn.h>
#include <winrt/base.h>

class WindowsPipeClient final : public PipeClient
{
public:
    WindowsPipeClient() = delete;
    ~WindowsPipeClient() override = default;
    
    static std::expected<std::unique_ptr<PipeClient>, std::string> Open() noexcept;
    std::expected<size_t, std::string> Read(void* buffer, size_t bufferSize) noexcept override;
private:
    explicit WindowsPipeClient(winrt::file_handle connection) noexcept;
    winrt::file_handle mConnection;
};