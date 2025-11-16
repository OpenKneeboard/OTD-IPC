/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

#define WIN32_LEAN_AND_MEAN 1
#define NOMINMAX 1
#define UNICODE 1
#define _UNICODE 1

#include <iostream>
#include <format>
#include <Windows.h>
#include <Unknwn.h>
#include <winrt/base.h>

#include <OTD-IPC/DeviceInfo.h>
#include <OTD-IPC/Ping.h>
#include <OTD-IPC/NamedPipePath.h>
#include <OTD-IPC/State.h>
#include <OTD-IPC/DebugMessage.h>

void DumpMessage(const OTDIPC::Messages::DeviceInfo* const info)
{
    if (!info->isValid)
    {
        std::cout << "Received invalid deviceInfo packet" << std::endl;
        return;
    }

    std::wcout << std::format(
        L"Device: {}\n  "
        L"  VID {:04x} PID {:04x}\n"
        L"  {}x{}\n"
        L"  max pressure: {}",
        info->name,
        info->vid,
        info->pid,
        info->maxX,
        info->maxY,
        info->maxPressure) << std::endl;
}

void DumpMessage(const OTDIPC::Messages::State* const state) {
    std::string penButtons;
    std::string auxButtons;

  for (int i = 0; i < 32; ++i) {
    if (state->penButtons & (1 << i)) {
      if (!penButtons.empty()) {
                penButtons += " ";
            }
            penButtons += std::to_string(i);
        }

    if (state->auxButtons & (1 << i)) {
      if (!auxButtons.empty()) {
                auxButtons += " ";
            }
            auxButtons += std::to_string(i);
        }
    }

    std::cout << std::format(
        "{:04x}-{:04x} -> ({}, {}, {}) {} (near: {})\n"
        "  Pen: {}\n"
        "  Aux: {}",
        state->vid,
        state->pid,
        state->x,
        state->y,
        state->hoverDistance,
        state->pressure,
        state->nearProximity,
        penButtons,
        auxButtons) << std::endl;
}

void DumpMessage(const OTDIPC::Messages::Ping* const msg) {
    std::cout << std::format(
        "{:04x}-{:04x} Ping {:#016x}",
        msg->vid,
        msg->pid,
        msg->sequenceNumber) << std::endl;
}

void DumpMessage(const OTDIPC::Messages::DebugMessage* const msg)
{
    std::wcout << msg->message() << std::endl;
}

template <std::derived_from<OTDIPC::Messages::Header> T>
void DumpMessage(const OTDIPC::Messages::Header* const header)
{
    if (header->size < sizeof(T))
    {
        std::cerr << std::format(
            "Received message type {} of invalid size {} - expected {}",
            static_cast<std::underlying_type_t<const OTDIPC::Messages::MessageType>>(header->messageType),
            header->size,
            sizeof(T)) << std::endl;
        return;
    }
    DumpMessage(reinterpret_cast<const T* const>(header));
}

int main()
{
    winrt::file_handle connection{
        CreateFileW(
            OTDIPC::NamedPipePathW,
            GENERIC_READ,
            0,
            nullptr,
            OPEN_EXISTING,
            0,
            nullptr)
    };
    if (!connection)
    {
        std::cerr << std::format("Failed to open pipe: `{}` -> {}", OTDIPC::NamedPipePathA, GetLastError()) <<
            std::endl;
        return 1;
    }
    std::cerr << "Opened pipe" << OTDIPC::NamedPipePathA << std::endl;
    char buffer[1024];

    using namespace OTDIPC::Messages;
    static_assert(sizeof(buffer) >= sizeof(DeviceInfo));
    static_assert(sizeof(buffer) >= sizeof(State));
    const auto header = reinterpret_cast<const Header* const>(buffer);

    DWORD bytesRead{};
    while (ReadFile(connection.get(), buffer, sizeof(Header), &bytesRead, nullptr))
    {
        if (bytesRead != sizeof(Header))
        {
            std::cerr << "bytesRead != sizeof(Header)" << std::endl;
            return 1;
        }
        if (header->size < sizeof(Header))
        {
            std::cerr << std::format("header->size ({}) < sizeof(Header) ({})", header->size, sizeof(Header)) <<
                std::endl;
            return 1;
        }
        if (header->size > sizeof(buffer))
        {
            std::cerr << std::format("header->size ({}) < sizeof(Buffer) ({})", header->size, sizeof(buffer)) <<
                std::endl;
            return 1;
        }
        const DWORD bytesToRead = header->size - sizeof(Header);
        if (!ReadFile(connection.get(), buffer + sizeof(Header), bytesToRead, &bytesRead, nullptr))
        {
            std::cerr << std::format("Failed to read after header: {}", GetLastError()) << std::endl;
            return 1;
        }
        if (bytesRead != bytesToRead)
        {
            std::cerr << std::format("Only read {} bytes after header, needed {}", bytesRead, bytesToRead) << std::endl;
            return 1;
        }

        switch (header->messageType)
        {
        case MessageType::DeviceInfo:
            DumpMessage<DeviceInfo>(header);
            break;
        case MessageType::State:
            DumpMessage<State>(header);
            break;
        case MessageType::Ping:
            DumpMessage<Ping>(header);
            break;
        case MessageType::DebugMessage:
            DumpMessage<DebugMessage>(header);
            break;
        }
    }
    std::cerr << std::format("Read failed: {}", GetLastError()) << std::endl;
    return 0;
}
