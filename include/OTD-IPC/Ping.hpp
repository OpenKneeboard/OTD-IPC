/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.hpp"

namespace OTDIPC::Messages::inline V2 {

	struct Ping : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::Ping;
		uint64_t sequenceNumber;
	};

}
