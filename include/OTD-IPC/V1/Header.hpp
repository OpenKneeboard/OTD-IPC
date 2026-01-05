/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
 */
#pragma once

#include "MessageType.hpp"

namespace OTDIPC::V1::Messages {
  struct Header {
    MessageType messageType;
    uint32_t size;
    uint16_t vid;
    uint16_t pid;
  };
}
