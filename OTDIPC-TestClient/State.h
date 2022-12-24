#pragma once

#include "Header.h"

namespace OTDIPC::Messages {

	struct State : Header {
		static constexpr MessageType MESSAGE_TYPE = MessageType::State;

		float x;
		float y;
		uint32_t pressure;
		uint32_t penButtons;
		uint32_t auxButtons;
		uint32_t hoverDistance;
		bool nearProximity;
	};

}
