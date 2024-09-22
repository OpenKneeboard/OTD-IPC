/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.h"

namespace OTDIPC::Messages {

  struct State : Header {
    static constexpr MessageType MESSAGE_TYPE = MessageType::State;

    bool positionValid;
    float x;
    float y;

    bool pressureValid;
    uint32_t pressure;

    bool penButtonsValid;
    uint32_t penButtons;

    bool auxButtonsValid;
    uint32_t auxButtons;

    bool proximityValid;
    uint32_t hoverDistance;
    bool nearProximity;

  };

}
