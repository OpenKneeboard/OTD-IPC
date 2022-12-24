#pragma once

#include "MessageType.h"

namespace OTDIPC::Messages {

	struct Header {
		MessageType messageType;
		uint32_t size;
		uint16_t vid;
		uint16_t pid;
	};

}
