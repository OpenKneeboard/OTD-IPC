/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.hpp"

#include <string_view>

namespace OTDIPC::Messages {
	struct DebugMessage {
		static constexpr MessageType MESSAGE_TYPE = MessageType::DebugMessage;

		Header header { .messageType = MESSAGE_TYPE };
		char first {};

		std::string_view message() const
		{
			return { &first, header.size - offsetof(DebugMessage, first) };
		}
	};
}
