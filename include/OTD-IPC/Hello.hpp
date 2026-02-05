/*
 * Copyright (c) 2026 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.hpp"

namespace OTDIPC::inline V2::Messages {
  struct Hello {
    static constexpr MessageType MESSAGE_TYPE = MessageType::Hello;

    const Header header { MESSAGE_TYPE , sizeof(Hello) };

    // 0xAAYYYYMMDDBB;
    uint64_t protocolVersion {};
    char humanReadableName[256] {};
    char humanReadableVersion[256] {};
    char implementationID[256] {};
    uint8_t compatibilityVersion {};
  };
}
