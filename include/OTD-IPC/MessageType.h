#pragma once

#include <cinttypes>

namespace OTDIPC::Messages {

	enum class MessageType : uint32_t {
		DeviceInfo = 1,
		State = 2,
	};

}
