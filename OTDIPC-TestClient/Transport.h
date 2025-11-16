// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include <expected>
#include <string>
#include <memory>

class Transport
{
public:
    static std::expected<std::unique_ptr<Transport>, std::string> Open() noexcept;
    
    virtual ~Transport() = default;
    
    virtual std::expected<size_t, std::string> Read(void* buffer, size_t bufferSize) noexcept = 0;
};
