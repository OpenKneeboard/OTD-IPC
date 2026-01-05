/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.hpp"

namespace OTDIPC::V1::Messages {

	struct DeviceInfo : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::DeviceInfo;

		bool isValid;
		float maxX;
		float maxY;
		uint32_t maxPressure;
		wchar_t name[64];
	};

}
