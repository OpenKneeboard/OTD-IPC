/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */

#include "Transport.hpp"

#include <algorithm>
#include <chrono>
#include <cstring>
#include <format>
#include <iostream>
#include <print>
#include <ranges>
#include <string_view>
#include <vector>

#include <OTD-IPC/DebugMessage.hpp>
#include <OTD-IPC/DeviceInfo.hpp>
#include <OTD-IPC/Ping.hpp>
#include <OTD-IPC/State.hpp>

namespace {
template <std::size_t N>
std::string_view TruncateNulls(const char (&buf)[N]) {
  return std::string_view(buf, strnlen(buf, N));
}

struct {
  OTDIPC::Messages::DeviceInfo mDevice {};
  OTDIPC::Messages::State mState {};
  struct {
    std::size_t mSequenceNumber {};
    std::chrono::steady_clock::time_point mTimestamp {};
    std::size_t mCount {};

  } mPing;
} gInfo;

std::vector<std::string> gDebugMessages;

[[nodiscard]]
std::string GetDeviceText() {
  const auto& device = gInfo.mDevice;
  return std::format(
    "Device {:04x}: '{}'\n"
    "  Persistent ID: '{}'\n"
    "  {}x{}\n"
    "  max pressure: {}",
    device.nonPersistentTabletId,
    TruncateNulls(device.name),
    TruncateNulls(device.persistentId),
    device.maxX,
    device.maxY,
    device.maxPressure);
}

[[nodiscard]]
std::string GetStateText() {
  const auto& state = gInfo.mState;
  std::string penButtons;
  std::string auxButtons;

  for (int i = 0; i < 32; ++i) {
    if (state.penButtons & (1 << i)) {
      if (!penButtons.empty()) {
        penButtons += " ";
      }
      penButtons += std::to_string(i);
    }

    if (state.auxButtons & (1 << i)) {
      if (!auxButtons.empty()) {
        auxButtons += " ";
      }
      auxButtons += std::to_string(i);
    }
  }

  std::vector<std::string_view> bits;
  using enum OTDIPC::Messages::State::ValidMask;
  if (state.HasData(Position))
    bits.emplace_back("Position");
  if (state.HasData(Pressure))
    bits.emplace_back("Pressure");
  if (state.HasData(PenButtons))
    bits.emplace_back("PenButtons");
  if (state.HasData(AuxButtons))
    bits.emplace_back("AuxButtons");
  if (state.HasData(PenIsNearSurface))
    bits.emplace_back("PenIsNearSurface");
  if (state.HasData(HoverDistance))
    bits.emplace_back("HoverDistance");

  // Not using `join_with` because it's not yet available on macOS
  const auto bitsStr = std::ranges::fold_left(
    bits | std::views::drop(1) | std::views::transform([](auto&& bit) {
      return std::format(" | {}", bit);
    }),
    std::string {bits.empty() ? std::string_view {} : bits.front()},
    std::plus<> {});

  return std::format(
    "{:04x} -> ({}, {})\n"
    "  Hover distance: {} (near: {})\n"
    "  Pressure: {}\n"
    "  Pen buttons: {}\n"
    "  Aux buttons: {}\n"
    "  Valid: {}",
    state.nonPersistentTabletId,
    state.x,
    state.y,
    state.hoverDistance,
    state.penIsNearSurface,
    state.pressure,
    penButtons,
    auxButtons,
    bitsStr);
}

[[nodiscard]]
std::string GetPingText() {
  const auto& ping = gInfo.mPing;

  const auto ago = std::chrono::steady_clock::now() - ping.mTimestamp;
  const auto unzonedTime = std::chrono::time_point_cast<std::chrono::seconds>(
    std::chrono::system_clock::now() - ago);

  // Shout out to Apple in 2026
#if __cpp_lib_chrono >= 201907
  const auto time
    = std::chrono::zoned_time(std::chrono::current_zone(), unzonedTime);
#else
  const auto time = unzonedTime;
#endif

  return std::format(
    "Ping: {} (seq #{}, count #{})", time, ping.mSequenceNumber, ping.mCount);
}

[[nodiscard]]
std::string GetDebugMessageText() {
  if (gDebugMessages.empty()) {
    return {};
  }

  auto ret = std::ranges::fold_left(
    gDebugMessages | std::views::transform([](auto&& msg) {
      return std::format("> {}\n", msg);
    }),
    std::string {},
    std::plus<> {});
  ret.pop_back();
  return "\n---------------\n"
         "\nDebug messages:"
         "\n\n"
    + ret;
}

void Dump() {
  const auto message = std::format(
    "\033[?2026h"// begin synchronized update
    "\033[2J"// clear entire screen
    "\033[1;1H"// cursor to top left
    "{}\n{}\n{}\n{}"
    "\033[?2026l",// end synchronized update
    GetDeviceText(),
    GetStateText(),
    GetPingText(),
    GetDebugMessageText());

  // Avoid flicker on updates: send the whole message in one buffer
  // In buffered mode, Windows Terminal will draw char-at-a-time with noticeable
  // flicker even within a single print statement
  //
  // This isn't needed if the terminal recognizes the synchronized update escape
  // codes above, but as of 2026-01-28 it's a bit too new to depend on, e.g.
  // it's been in Windows Terminal for two weeks
  setvbuf(
    stdout, nullptr, _IOFBF, std::max<std::size_t>(BUFSIZ, message.size()));
  std::print("{}", message);
  fflush(stdout);
  std::setvbuf(stdout, nullptr, _IONBF, 0);
}

void OnMessage(const OTDIPC::Messages::DeviceInfo& info) {
  gInfo.mDevice = info;
  Dump();
}

void OnMessage(const OTDIPC::Messages::State& state) {
  gInfo.mState = state;
  Dump();
}

void OnMessage(const OTDIPC::Messages::Ping& msg) {
  gInfo.mPing.mSequenceNumber = msg.sequenceNumber;
  gInfo.mPing.mTimestamp = std::chrono::steady_clock::now();
  gInfo.mPing.mCount++;
  Dump();
}

void OnMessage(const OTDIPC::Messages::DebugMessage& msg) {
  gDebugMessages.emplace_back(msg.message());
  Dump();
}

template <class T>
void OnMessage(const OTDIPC::Messages::Header* const header) {
  if (header->size < sizeof(T)) {
    std::cerr << std::format(
      "Received message type {} of invalid size {} - expected {}",
      static_cast<std::underlying_type_t<const OTDIPC::Messages::MessageType>>(
        header->messageType),
      header->size,
      sizeof(T))
              << std::endl;
    return;
  }
  OnMessage(*reinterpret_cast<const T*>(header));
}

struct ScopedAlternateBuffer {
  ScopedAlternateBuffer() {
    std::print(
      "\033[?1049h"// Enable alternative buffer
      "\033[?25l"// Disable cursor
    );
  }
  ~ScopedAlternateBuffer() {
    std::print(
      "\033[?1049l"// Disable alternative buffer
      "\033[?25h"// Enable cursor
    );
  }
};

}// namespace

int main() {
  std::setvbuf(stdout, nullptr, _IONBF, 0);
  const auto connection = Transport::Open();
  if (!connection) {
    std::cerr << connection.error() << std::endl;
    return 1;
  }
  char buffer[1024];

  using namespace OTDIPC::Messages;
  static_assert(sizeof(buffer) >= sizeof(DeviceInfo));
  static_assert(sizeof(buffer) >= sizeof(State));
  const auto header = reinterpret_cast<const Header* const>(buffer);

  {
    const auto dbgMessage = reinterpret_cast<DebugMessage*>(buffer);
    *dbgMessage = {};
    constexpr std::string_view kMessage = "Hello from OTDIPC-TestClient";
    dbgMessage->header.size = sizeof(Header) + kMessage.size();
    memcpy(dbgMessage->data, kMessage.data(), kMessage.size());
    if (const auto written
        = (*connection)->Write(buffer, dbgMessage->header.size);
        !written) {
      std::cerr << "Failed to write client hello: " << written.error()
                << std::endl;
      return EXIT_FAILURE;
    }
  }

  const ScopedAlternateBuffer useAlternateBuffer;

  while (true) {
    const auto result = (*connection)->Read(buffer, sizeof(Header));
    if (!result) {
      std::cerr << result.error() << std::endl;
      return 1;
    }
    if (const auto bytesRead = result.value(); bytesRead != sizeof(Header)) {
      std::cerr << std::format(
        "Read {} bytes instead of {} bytes", bytesRead, sizeof(Header))
                << std::endl;
      return 1;
    }

    if (header->size < sizeof(Header)) {
      std::cerr << std::format(
        "header->size ({}) < sizeof(Header) ({})", header->size, sizeof(Header))
                << std::endl;
      return 1;
    }
    if (header->size > sizeof(buffer)) {
      std::cerr << std::format(
        "header->size ({}) < sizeof(Buffer) ({})", header->size, sizeof(buffer))
                << std::endl;
      return 1;
    }
    const auto bytesToRead = header->size - sizeof(Header);
    if (const auto bodyResult
        = (*connection)->Read(buffer + sizeof(Header), bytesToRead);
        !bodyResult) {
      std::cerr << std::format(
        "Failed to read after header: {}", bodyResult.error())
                << std::endl;
      return 1;
    }

    switch (header->messageType) {
      case MessageType::DeviceInfo:
        OnMessage<DeviceInfo>(header);
        break;
      case MessageType::State:
        OnMessage<State>(header);
        break;
      case MessageType::Ping:
        OnMessage<Ping>(header);
        break;
      case MessageType::DebugMessage:
        OnMessage<DebugMessage>(header);
        break;
      case MessageType::Experimental:
        break;
    }
  }
}
