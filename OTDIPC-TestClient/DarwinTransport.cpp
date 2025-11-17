// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT

#include "DarwinTransport.h"

#include <filesystem>
#include <print>
#include <string>

#include <pwd.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>
#include <cerrno>
#include <sysdir.h>
#include <wordexp.h>

namespace
{
    std::expected<std::filesystem::path, std::string> GetApplicationSupportPath() noexcept
    {
        char appSupport[PATH_MAX];
        {
            auto state = sysdir_start_search_path_enumeration(SYSDIR_DIRECTORY_APPLICATION_SUPPORT, SYSDIR_DOMAIN_MASK_USER);
            state = sysdir_get_next_search_path_enumeration(state, appSupport);
        }

        if (!appSupport[0]) {
            return std::unexpected{"Failed to find Application Support directory"};
        }

        // Expand `~/Library/...` to `/Users/..../Library/...`
        wordexp_t p {};
        if (const auto ret = wordexp(appSupport, &p, WRDE_NOCMD); ret != 0) {
            return std::unexpected{std::format("wordexp failed: {} - {}", errno, strerror(errno))};
        }
        std::string expandedAppSupport;
        for (auto i = 0; i < p.we_wordc; ++i) {
            if (i == 0) {
                expandedAppSupport = p.we_wordv[i];
            } else {
                expandedAppSupport = std::format("{} {}", expandedAppSupport, p.we_wordv[i]);
            }
        }
        wordfree(&p);
        return std::filesystem::path{expandedAppSupport};
    }
}

std::expected<std::unique_ptr<Transport>, std::string> Transport::Open() noexcept
{
    return DarwinTransport::Open();
}

std::expected<std::unique_ptr<Transport>, std::string> DarwinTransport::Open() noexcept
{
    const auto appSupport = GetApplicationSupportPath();
    if (!appSupport) {
        return std::unexpected{appSupport.error()};
    }
    const auto path = GetSocketPath(*appSupport);
    if (!path)
    {
        return std::unexpected{path.error()};
    }

    if (!exists(*path))
    {
        return std::unexpected{std::format("Socket `{}` does not exist", path->string())};
    }

    unique_fd fd{::socket(AF_UNIX, SOCK_STREAM, 0)};
    if (!fd.valid())
    {
        return std::unexpected{std::format("socket(AF_UNIX) failed: {}", std::strerror(errno))};
    }

    sockaddr_un addr{};
    addr.sun_family = AF_UNIX;
    const auto pathString = path->string();
    if (pathString.size() >= sizeof(addr.sun_path))
    {
        return std::unexpected{std::format("Socket path too long ({} >= {})", pathString.size(), sizeof(addr.sun_path))};
    }
    std::strncpy(addr.sun_path, pathString.c_str(), sizeof(addr.sun_path) - 1);

    const socklen_t addrlen = static_cast<socklen_t>(offsetof(sockaddr_un, sun_path) + std::strlen(addr.sun_path) + 1);
    if (::connect(fd.get(), reinterpret_cast<const sockaddr*>(&addr), addrlen) != 0)
    {
        return std::unexpected{std::format("connect('{}') failed: {}", pathString, std::strerror(errno))};
    }

    std::println("Connected to unix domain socket: {}", pathString);
    return std::unique_ptr<DarwinTransport>{new DarwinTransport(std::move(fd))};
}

std::expected<size_t, std::string> DarwinTransport::Read(void* buffer, const size_t bufferSize) noexcept
{
    if (!mFD.valid())
    {
        return std::unexpected{"Socket is not connected"};
    }
    size_t total = 0;
    char* out = static_cast<char*>(buffer);
    while (total < bufferSize)
    {
        const size_t toRead = bufferSize - total;
        const ssize_t ret = ::read(mFD.get(), out + total, toRead);
        if (ret < 0)
        {
            if (errno == EINTR) { continue; }
            return std::unexpected{std::format("read failed: {}", std::strerror(errno))};
        }
        if (ret == 0)
        {
            return std::unexpected{"socket closed by peer"};
        }
        total += static_cast<size_t>(ret);
    }
    return total;
}

DarwinTransport::unique_fd::~unique_fd() noexcept
{
    if (mFD >= 0) { ::close(mFD); mFD = -1; }
}

DarwinTransport::unique_fd::unique_fd(unique_fd&& other) noexcept : mFD(std::exchange(other.mFD, -1))
{
}

DarwinTransport::unique_fd& DarwinTransport::unique_fd::operator=(unique_fd&& other) noexcept
{
    if (this != &other)
    {
        if (mFD >= 0) { ::close(mFD); }
        mFD = std::exchange(other.mFD, -1);
    }
    return *this;
}

int DarwinTransport::unique_fd::release() noexcept
{
    return std::exchange(mFD, -1);
}

void DarwinTransport::unique_fd::reset(const int fd) noexcept
{
    if (mFD >= 0) { ::close(mFD); }
    mFD = fd >= 0 ? fd : -1;
}

DarwinTransport::DarwinTransport(unique_fd fd) noexcept : mFD(std::move(fd))
{
}

DarwinTransport::~DarwinTransport() = default;