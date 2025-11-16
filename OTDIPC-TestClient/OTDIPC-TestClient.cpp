/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

#include "Transport.h"

#include <iostream>
#include <format>
#include <string.h>
#include <print>

#include <OTD-IPC/DeviceInfo.h>
#include <OTD-IPC/Ping.h>
#include <OTD-IPC/State.h>
#include <OTD-IPC/DebugMessage.h>

template<std::size_t N>
static std::string_view TruncateNulls(const char (&buf)[N]) {
    return std::string_view(buf, strnlen(buf, N));
}

void DumpMessage(const OTDIPC::Messages::DeviceInfo* const info)
{
    if (!info->isValid)
    {
        std::println("Received invalid deviceInfo");
        return;
    }

    std::println(
        "Device {:04x}: {}\n"
        "  Persistent ID: {}\n"
        "  VID {:04x} PID {:04x}\n"
        "  {}x{}\n"
        "  max pressure: {}",
        info->nonPersistentTabletId,
        TruncateNulls(info->name),
        TruncateNulls(info->persistentId),
        info->vendorId,
        info->productId,
        info->maxX,
        info->maxY,
        info->maxPressure);
}

void DumpMessage(const OTDIPC::Messages::State* const state)
{
    std::string penButtons;
    std::string auxButtons;

    for (int i = 0; i < 32; ++i)
    {
        if (state->penButtons & (1 << i))
        {
            if (!penButtons.empty())
            {
                penButtons += " ";
            }
            penButtons += std::to_string(i);
        }

        if (state->auxButtons & (1 << i))
        {
            if (!auxButtons.empty())
            {
                auxButtons += " ";
            }
            auxButtons += std::to_string(i);
        }
    }

    std::println(
        "{:04x} -> ({}, {}, {}) {} (near: {})\n"
        "  Pen: {}\n"
        "  Aux: {}",
        state->nonPersistentTabletId,
        state->x,
        state->y,
        state->hoverDistance,
        state->pressure,
        state->nearProximity,
        penButtons,
        auxButtons);
}

void DumpMessage(const OTDIPC::Messages::Ping* const msg)
{
    std::println(
        "{:08x} Ping {:#016x}",
        msg->nonPersistentTabletId,
        msg->sequenceNumber);
}

void DumpMessage(const OTDIPC::Messages::DebugMessage* const msg)
{
    std::println("{}", msg->message());
}

template <class T>
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
    std::setvbuf(stdout, nullptr, _IONBF, 0);
    const auto connection = Transport::Open();
    if (!connection)
    {
        std::cerr << connection.error() << std::endl;
        return 1;
    }
    char buffer[1024];

    using namespace OTDIPC::Messages;
    static_assert(sizeof(buffer) >= sizeof(DeviceInfo));
    static_assert(sizeof(buffer) >= sizeof(State));
    const auto header = reinterpret_cast<const Header* const>(buffer);

    while (true) {
        const auto result = (*connection)->Read(buffer, sizeof(Header));
        if (!result)
        {
            std::cerr << result.error() << std::endl;
            return 1;
        }
        if (const auto bytesRead = result.value(); bytesRead != sizeof(Header))
        {
            std::cerr << std::format("Read {} bytes instead of {} bytes", bytesRead, sizeof(Header)) << std::endl;
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
        const auto bytesToRead = header->size - sizeof(Header);
        if (const auto bodyResult = (*connection)->Read(buffer + sizeof(Header), bytesToRead); !bodyResult)
        {
            std::cerr << std::format("Failed to read after header: {}", bodyResult.error()) << std::endl;
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
}
