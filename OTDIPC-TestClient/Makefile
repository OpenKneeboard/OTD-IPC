CC?=cc
CXX?=c++
PROTO_HEADERS:=$(wildcard ../include/OTD-IPC/*.h)

ifneq (,$(findstring cl,$(CXX)))
    # MSVC Flags
    REQUIRED_CXX_FLAGS:=/std:c++23preview /EHsc /MD /nologo
    CXX_OUT_FLAG:="/Fe:"
    PLATFORM:=Windows
else
    REQUIRED_CXX_FLAGS:=--std=c++23
    CXX_OUT_FLAG:="-o " # Space is mandatory
	PLATFORM:=$(shell uname -s)
endif

all: otd-ipc-test-client

proto-headers: $(PROTO_HEADERS)

otd-ipc-test-client: OTDIPC-TestClient.cpp Transport.cpp Transport.h ${PLATFORM}Transport.h ${PLATFORM}Transport.cpp $(PROTO_HEADERS)
	"${CXX}" "${CXX_OUT_FLAG}$@" ${REQUIRED_CXX_FLAGS} -I ../include OTDIPC-TestClient.cpp Transport.cpp ${PLATFORM}Transport.cpp

clean:
	rm -f otd-ipc-test-client

.PHONY: all clean proto-headers