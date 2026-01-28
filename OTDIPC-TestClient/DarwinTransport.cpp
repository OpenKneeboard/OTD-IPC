// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT

#include "DarwinTransport.hpp"

#include <filesystem>
#include <print>
#include <string>

#include <cerrno>
#include <pwd.h>
#include <sys/un.h>
#include <sysdir.h>
#include <wordexp.h>

#include "PosixSocket.hpp"

namespace {
std::expected<std::filesystem::path, std::string>
GetApplicationSupportPath() noexcept {
  char appSupport[PATH_MAX];
  {
    auto state = sysdir_start_search_path_enumeration(
      SYSDIR_DIRECTORY_APPLICATION_SUPPORT, SYSDIR_DOMAIN_MASK_USER);
    state = sysdir_get_next_search_path_enumeration(state, appSupport);
  }

  if (!appSupport[0]) {
    return std::unexpected {"Failed to find Application Support directory"};
  }

  // Expand `~/Library/...` to `/Users/..../Library/...`
  wordexp_t p {};
  if (const auto ret = wordexp(appSupport, &p, WRDE_NOCMD); ret != 0) {
    return std::unexpected {
      std::format("wordexp failed: {} - {}", errno, strerror(errno))};
  }
  std::string expandedAppSupport;
  for (auto i = 0; i < p.we_wordc; ++i) {
    if (i == 0) {
      expandedAppSupport = p.we_wordv[i];
    } else {
      expandedAppSupport
        = std::format("{} {}", expandedAppSupport, p.we_wordv[i]);
    }
  }
  wordfree(&p);
  return std::filesystem::path {expandedAppSupport};
}
}// namespace

std::expected<std::unique_ptr<Transport>, std::string>
Transport::Open() noexcept {
  return DarwinTransport::Open();
}

std::expected<std::unique_ptr<Transport>, std::string>
DarwinTransport::Open() noexcept {
  const auto appSupport = GetApplicationSupportPath();
  if (!appSupport) {
    return std::unexpected {appSupport.error()};
  }
  const auto path = GetSocketPath(*appSupport);
  if (!path) {
    return std::unexpected {path.error()};
  }

  auto fd = PosixSocket::ConnectToUnixSocket(*path);
  if (!fd) {
    return std::unexpected {fd.error()};
  }

  std::println("Connected to unix domain socket: {}", path->string());
  return std::unique_ptr<DarwinTransport> {new DarwinTransport(*std::move(fd))};
}

std::expected<size_t, std::string> DarwinTransport::Read(
  void* buffer,
  const size_t bufferSize) noexcept {
  return PosixSocket::Read(*mFD, buffer, bufferSize);
}

std::expected<void, std::string> DarwinTransport::Write(
  const void* buffer,
  const size_t bufferSize) noexcept {
  return PosixSocket::Write(*mFD, buffer, bufferSize);
}

DarwinTransport::DarwinTransport(unique_fd fd) noexcept : mFD(std::move(fd)) {
}

DarwinTransport::~DarwinTransport() = default;