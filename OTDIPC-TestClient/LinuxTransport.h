// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include "Transport.h"

#include <expected>
#include <utility>
#include <memory>
#include <string>

class LinuxTransport final : public Transport
{
public:
    LinuxTransport() = delete;
    ~LinuxTransport() override;

    static std::expected<std::unique_ptr<Transport>, std::string> Open() noexcept;
    std::expected<size_t, std::string> Read(void *buffer, size_t bufferSize) noexcept override;

private:
    class unique_fd
    {
    public:
        unique_fd() = default;
        explicit unique_fd(int fd) noexcept : mFD(fd) {}
        ~unique_fd() noexcept;

        unique_fd(const unique_fd &) = delete;
        unique_fd &operator=(const unique_fd &) = delete;

        unique_fd(unique_fd &&other) noexcept;
        unique_fd &operator=(unique_fd &&other) noexcept;

        [[nodiscard]] bool valid() const noexcept { return mFD >= 0; }
        [[nodiscard]] int get() const noexcept { return mFD; }
        int release() noexcept;
        void reset(int fd = -1) noexcept;

        operator int() const noexcept { return mFD; }

    private:
        int mFD{-1};
    };

    explicit LinuxTransport(unique_fd fd) noexcept;
    unique_fd mFD{};
};