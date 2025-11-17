// Copyright 2025 Fred Emmott <fred@fredemmott.com>
// SPDX-License-Identifier: MIT

#include "Transport.h"

#include <format>
#include <fstream>
#include <sstream>
#include <ranges>

std::expected<std::filesystem::path, std::string> Transport::GetSocketPath(
    const std::filesystem::path& localAppDataRoot) noexcept
{
    const auto discoveryRoot = localAppDataRoot / "otd-ipc" / "servers" / "v2";
    const auto defaultIDPath = discoveryRoot / "default.txt";
    if (!exists(defaultIDPath))
    {
        return std::unexpected{std::format("Default ID path `{}` does not exist", defaultIDPath.string())};
    }

    std::stringstream buffer;
    {
        std::ifstream file(defaultIDPath);
        if (!file.is_open())
        {
            return std::unexpected{std::format("Failed to open default ID path `{}`", defaultIDPath.string())};
        }

        buffer << file.rdbuf();
    }
    std::string defaultID = buffer.str();
    erase_if(defaultID, &isspace);

    const auto metadataPath = discoveryRoot / "available" / std::format("{}.txt", defaultID);
    if (!std::filesystem::exists(metadataPath))
    {
        return std::unexpected{std::format("Metadata path `{}` does not exist", metadataPath.string())};
    }

    buffer = {};
    {
        std::ifstream file(metadataPath);
        if (!file.is_open())
        {
            return std::unexpected{std::format("Failed to open metadata path `{}`", metadataPath.string())};
        }
        buffer << file.rdbuf();
    }
    const std::string metadata = buffer.str();

    for (auto&& line : std::views::split(metadata, '\n'))
    {
        std::string_view sv{line};
        if (!sv.starts_with("SOCKET="))
        {
            continue;
        }
        sv.remove_prefix(sizeof("SOCKET=") - 1);
        if (sv.ends_with('\n'))
        {
            sv.remove_suffix(1);
        }
        return std::filesystem::path{sv};
    }
    return std::unexpected{std::format("Failed to find SOCKET in metadata file {}", metadataPath.string())};
}
