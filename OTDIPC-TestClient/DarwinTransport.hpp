// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT
#pragma once

#include "Transport.hpp"

#include "unique_fd.hpp"

#include <expected>
#include <memory>
#include <string>

class DarwinTransport final : public Transport {
 public:
  DarwinTransport() = delete;
  ~DarwinTransport() override;

  static std::expected<std::unique_ptr<Transport>, std::string> Open() noexcept;
  std::expected<size_t, std::string> Read(
    void* buffer,
    size_t bufferSize) noexcept override;
  std::expected<void, std::string> Write(
    void const* buffer,
    size_t bufferSize) noexcept override;

 private:
  explicit DarwinTransport(unique_fd fd) noexcept;
  unique_fd mFD {};
};