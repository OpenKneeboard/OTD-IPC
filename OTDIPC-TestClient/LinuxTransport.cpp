// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT

#include "LinuxTransport.h"

#include <filesystem>
#include <print>
#include <string>

#include <pwd.h>
#include <sys/un.h>
#include <cerrno>
#include "PosixSocket.hpp"


namespace
{
    std::expected<std::filesystem::path, std::string> GetApplicationSupportPath() noexcept
    {
        if (const auto dataDir = getenv("XDG_DATA_HOME"))
        {
            return dataDir;
        }

        auto home = getenv("HOME");
        if (!home) {
            home = getpwuid(getuid())->pw_dir;
        }

        return std::filesystem::path{home} / ".local" / "share";
    }
}

std::expected<std::unique_ptr<Transport>, std::string> Transport::Open() noexcept
{
    return LinuxTransport::Open();
}

std::expected<std::unique_ptr<Transport>, std::string> LinuxTransport::Open() noexcept
{
    const auto appSupport = GetApplicationSupportPath();
    if (!appSupport)
    {
        return std::unexpected{appSupport.error()};
    }
    const auto path = GetSocketPath(*appSupport);
    if (!path)
    {
        return std::unexpected{path.error()};
    }

    auto fd = PosixSocket::ConnectToUnixSocket(*path);
    if (!fd) {
        return std::unexpected{fd.error()};
    }

    std::println("Connected to unix socket at `{}`", path->string());

    return std::unique_ptr<LinuxTransport>{new LinuxTransport(*std::move(fd))};
}

std::expected<size_t, std::string> LinuxTransport::Read(void *buffer, const size_t bufferSize) noexcept
{
    return PosixSocket::Read(*mFD, buffer, bufferSize);
}

LinuxTransport::LinuxTransport(unique_fd fd) noexcept : mFD(std::move(fd))
{
}

LinuxTransport::~LinuxTransport() = default;

