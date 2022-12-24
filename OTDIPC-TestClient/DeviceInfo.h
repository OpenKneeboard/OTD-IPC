#pragma once

#include "Header.h"

namespace OTDIPC::Messages {

	struct DeviceInfo : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::DeviceInfo;

		bool isValid;
		float maxX;
		float maxY;
		uint32_t maxPressure;
		wchar_t name[64];
	};

}
