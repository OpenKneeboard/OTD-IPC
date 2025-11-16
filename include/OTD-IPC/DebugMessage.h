/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.h"

#include <string_view>

namespace OTDIPC::Messages {

	struct DebugMessage : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::DebugMessage;

		char first {};
		
		std::string_view message() const
		{
			return { &first, size - offsetof(DebugMessage, first) };
		}
	};

}
