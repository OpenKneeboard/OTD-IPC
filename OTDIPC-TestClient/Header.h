#pragma once

#include "MessageType.h"

namespace OTDIPC::Messages {

	struct Header {
		MessageType messageType;
		uint16_t vid;
		uint16_t pid;
	};

}
