// Copyright 2026 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include <filesystem>
#include <expected>
#include <string>

#include "unique_fd.hpp"

namespace PosixSocket {
std::expected<unique_fd, std::string> ConnectToUnixSocket(const std::filesystem::path&);
std::expected<size_t, std::string> Read(int fd, void *buffer, size_t bufferSize) noexcept;
std::expected<void, std::string> Write(int fd, void const *buffer, size_t bufferSize) noexcept;
}
