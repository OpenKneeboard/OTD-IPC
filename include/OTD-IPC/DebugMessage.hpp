/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.hpp"

#include <string_view>

namespace OTDIPC::inline V2::Messages {
	struct DebugMessage {
		static constexpr MessageType MESSAGE_TYPE = MessageType::DebugMessage;

		Header header { .messageType = MESSAGE_TYPE };
		char data[1] {};

		std::string_view message() const
		{
			return { data, header.size - offsetof(DebugMessage, data) };
		}
	};
}
