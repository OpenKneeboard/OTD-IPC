// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include "Transport.hpp"

#include <expected>
#include <utility>
#include <memory>
#include <string>

#include "unique_fd.hpp"

class LinuxTransport final : public Transport
{
public:
    LinuxTransport() = delete;
    ~LinuxTransport() final;

    static std::expected<std::unique_ptr<Transport>, std::string> Open() noexcept;
    std::expected<size_t, std::string> Read(void *buffer, size_t bufferSize) noexcept override;

private:
    explicit LinuxTransport(unique_fd fd) noexcept;
    unique_fd mFD{};
};