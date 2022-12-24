/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: ISC
 */
#pragma once

#include "Header.h"

namespace OTDIPC::Messages {

	struct KeepAlive : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::KeepAlive;
		uint64_t sequenceNumber;
	};

}
