// Copyright 2026 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include <unistd.h>
#include <utility>

class unique_fd {
public:
    unique_fd() = default;

    explicit unique_fd(const int fd) noexcept : mFD(fd) {
    }

    ~unique_fd() noexcept {
        reset();
    };

    unique_fd(const unique_fd &) = delete;

    unique_fd &operator=(const unique_fd &) = delete;

    unique_fd(unique_fd &&other) noexcept : mFD(other.release()) {
    }

    unique_fd &operator=(unique_fd &&other) noexcept {
        mFD = other.release();
        return *this;
    }

    [[nodiscard]] bool valid() const noexcept { return mFD >= 0; }
    [[nodiscard]] int get() const noexcept { return mFD; }

    [[nodiscard]]
    int release() noexcept {
        return std::exchange(mFD, InvalidFD);
    }

    void reset(int fd = InvalidFD) noexcept {
        if (valid()) {
            ::close(mFD);
        }
        mFD = fd;
    }

    [[nodiscard]]
    int operator*() const noexcept {
        return get();
    }
    operator bool() const noexcept { return valid(); }

private:
    static constexpr int InvalidFD = -1;
    int mFD{InvalidFD};
};
