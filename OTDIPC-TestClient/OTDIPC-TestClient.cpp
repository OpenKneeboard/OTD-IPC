/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
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
#include <OTD-IPC/KeepAlive.h>
#include <OTD-IPC/State.h>

void DumpMessage(const OTDIPC::Messages::DeviceInfo* const info) {
  if (!info->isValid) {
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

void DumpMessage(const OTDIPC::Messages::KeepAlive* const msg) {
  std::cout << std::format(
    "{:04x}-{:04x} KEEPALIVE {:#016x}",
    msg->vid,
    msg->pid,
    msg->sequenceNumber) << std::endl;
}

template<std::derived_from<OTDIPC::Messages::Header> T>
void DumpMessage(const OTDIPC::Messages::Header* const header, size_t size) {
  if (size < sizeof(T)) {
    std::cerr << std::format(
      "Received message type {} of invalid size {} - expected {}",
      static_cast<std::underlying_type_t<const OTDIPC::Messages::MessageType>>(header->messageType),
      size,
      sizeof(T)) << std::endl;
    return;
  }
  DumpMessage(reinterpret_cast<const T* const>(header));
}

int main()
{
  auto rawHandle =
    CreateFileW(
      L"\\\\.\\pipe\\com.fredemmott.openkneeboard.OTDIPC/v0.1",
      GENERIC_READ,
      0,
      nullptr,
      OPEN_EXISTING,
      0,
      NULL);
  if (rawHandle == INVALID_HANDLE_VALUE || !rawHandle) {
    std::cerr << std::format("Failed to open pipe: {}", GetLastError()) << std::endl;
    return 1;
  }
  winrt::handle connection{ rawHandle };
  char buffer[1024];

  using namespace OTDIPC::Messages;
  static_assert(sizeof(buffer) >= sizeof(DeviceInfo));
  static_assert(sizeof(buffer) >= sizeof(State));
  auto header = reinterpret_cast<const Header* const>(&buffer);

  DWORD bytesRead {};
  while (ReadFile(connection.get(), buffer, sizeof(buffer), &bytesRead, nullptr)) {
    if (bytesRead != header->size) {
      std::cerr << "header->size != packet size - is named pipe in message mode?" << std::endl;
      return 1;
    }
    switch (header->messageType) {
      case MessageType::DeviceInfo:
        DumpMessage<DeviceInfo>(header, static_cast<size_t>(bytesRead));
        break;
      case MessageType::State:
        DumpMessage<State>(header, static_cast<size_t>(bytesRead));
        break;
      case MessageType::KeepAlive:
        DumpMessage<KeepAlive>(header, static_cast<size_t>(bytesRead));
        break;
    }
  }
  std::cerr << std::format("Read failed: {}", GetLastError()) << std::endl;
  return 0;
}

