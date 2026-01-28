// Copyright 2026 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT

#include "PosixSocket.hpp"

#include <cstring>
#include <format>
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>

namespace PosixSocket {

std::expected<unique_fd, std::string> ConnectToUnixSocket(
  const std::filesystem::path& path) {
  if (!exists(path)) {
    return std::unexpected {
      std::format("Socket `{}` does not exist", path.string())};
  }

  unique_fd fd {::socket(AF_UNIX, SOCK_STREAM, 0)};
  if (!fd.valid()) {
    return std::unexpected {
      std::format("socket(AF_UNIX) failed: {}", std::strerror(errno))};
  }

  sockaddr_un addr {};
  addr.sun_family = AF_UNIX;
  const auto pathString = path.string();
  if (pathString.size() >= sizeof(addr.sun_path)) {
    return std::unexpected {std::format(
      "Socket path too long ({} >= {})",
      pathString.size(),
      sizeof(addr.sun_path))};
  }
  std::ranges::copy(pathString, addr.sun_path);

  const socklen_t addrlen = static_cast<socklen_t>(
    offsetof(sockaddr_un, sun_path) + pathString.size() + 1);
  if (
    ::connect(fd.get(), reinterpret_cast<const sockaddr*>(&addr), addrlen)
    != 0) {
    return std::unexpected {std::format(
      "connect('{}') failed: {}", pathString, std::strerror(errno))};
  }

  return fd;
}

std::expected<size_t, std::string>
Read(int fd, void* buffer, const size_t bufferSize) noexcept {
  if (fd < 0) {
    return std::unexpected {"Socket is not connected"};
  }
  size_t total = 0;
  char* out = static_cast<char*>(buffer);
  while (total < bufferSize) {
    const size_t toRead = bufferSize - total;
    const ssize_t ret = ::read(fd, out + total, toRead);
    if (ret < 0) {
      if (errno == EINTR) {
        continue;
      }
      return std::unexpected {
        std::format("read failed: {}", std::strerror(errno))};
    }
    if (ret == 0) {
      return std::unexpected {"socket closed by peer"};
    }
    total += static_cast<size_t>(ret);
  }
  return total;
}

std::expected<void, std::string>
Write(int fd, void const* buffer, const size_t bufferSize) noexcept {
  if (fd < 0) {
    return std::unexpected {"Socket is not connected"};
  }
  const ssize_t ret = ::write(fd, buffer, bufferSize);
  if (ret < 0) {
    return std::unexpected {std::format("write failed: {}", std::strerror(errno))};
  }
  return {};
}

}// namespace PosixSocket
