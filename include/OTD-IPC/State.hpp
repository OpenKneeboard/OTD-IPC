/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include <utility>

#include "Header.hpp"

namespace OTDIPC::inline V2::Messages {
    struct State : Header {
        static constexpr MessageType MESSAGE_TYPE = MessageType::State;

        enum class ValidMask : uint32_t {
            None = 0,
            PositionX = 1 << 0,
            PositionY = 1 << 1,
            Pressure = 1 << 2,
            PenButtons = 1 << 3,
            AuxButtons = 1 << 4,
            PenIsNearSurface = 1 << 5,
            HoverDistance = 1 << 6,

            Position = PositionX | PositionY,
        };

        ValidMask validBits;

        float x;
        float y;

        uint32_t pressure;
        uint32_t penButtons;
        uint32_t auxButtons;
        uint32_t hoverDistance;
        bool penIsNearSurface;

        constexpr bool HasData(const ValidMask mask) const noexcept {
            const auto underlying = std::to_underlying(mask);
            return (underlying & std::to_underlying(validBits)) == underlying;
        }
    };

    constexpr State::ValidMask operator|(const State::ValidMask lhs, const State::ValidMask rhs) noexcept {
        return static_cast<State::ValidMask>(std::to_underlying(lhs) | std::to_underlying(rhs));
    }

    constexpr State::ValidMask operator&(const State::ValidMask lhs, const State::ValidMask rhs) noexcept {
        return static_cast<State::ValidMask>(std::to_underlying(lhs) & std::to_underlying(rhs));
    }

    constexpr State::ValidMask operator~(const State::ValidMask mask) noexcept {
        return static_cast<State::ValidMask>(~std::to_underlying(mask));
    }

}
