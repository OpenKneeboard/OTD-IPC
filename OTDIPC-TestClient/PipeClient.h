// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include <expected>
#include <string>
#include <memory>

class PipeClient
{
public:
    static std::expected<std::unique_ptr<PipeClient>, std::string> Open() noexcept;
    
    virtual ~PipeClient() = default;
    
    virtual std::expected<size_t, std::string> Read(void* buffer, size_t bufferSize) noexcept = 0;
};
