/*
 * Copyright (c) 2022 Fred Emmott <fred@fredemmott.com>
 *
 * SPDX-License-Identifier: MIT
 */
#pragma once

#include "Header.hpp"

namespace OTDIPC::inline V2::Messages {

	struct DeviceInfo : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::DeviceInfo;
		static constexpr std::size_t PersistentIdMaxLength = 256;
		static constexpr std::size_t NameMaxLength = 256;

		float maxX {};
		float maxY {};
		uint32_t maxPressure {};
		uint16_t vendorId {}; // Deprecated: use persistentId instead. Used to allow matching with other data sources
		uint16_t productId {}; // Deprecated: use persistentId instead. Used to allow matching with other data sources
		
		char persistentId[PersistentIdMaxLength] {};
		char name[NameMaxLength] {};
	};

}
