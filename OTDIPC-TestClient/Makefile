CC?=cc
CXX?=c++
PROTO_HEADERS:=$(wildcard ../include/OTD-IPC/*.hpp)

ifneq (,$(findstring cl,$(CXX)))
    # MSVC Flags
    REQUIRED_CXX_FLAGS:=/std:c++23preview /EHsc /MD /nologo
    CXX_OUT_FLAGS = "/Fe:$@"
    PLATFORM:=Windows
    PLATFORM_HEADERS:=
    PLATFORM_SOURCES:=
else
    REQUIRED_CXX_FLAGS:=--std=c++23
    CXX_OUT_FLAGS = -o "$@"
	PLATFORM:=$(shell uname -s)
    PLATFORM_HEADERS:=unique_fd.hpp PosixSocket.hpp
    PLATFORM_SOURCES:=PosixSocket.cpp
endif

all: otd-ipc-test-client

proto-headers: $(PROTO_HEADERS)

otd-ipc-test-client: OTDIPC-TestClient.cpp Transport.cpp Transport.hpp ${PLATFORM}Transport.hpp ${PLATFORM}Transport.cpp $(PROTO_HEADERS) ${PLATFORM_HEADERS}
	"${CXX}" ${CXX_OUT_FLAGS} ${REQUIRED_CXX_FLAGS} -I ../include OTDIPC-TestClient.cpp Transport.cpp ${PLATFORM}Transport.cpp ${PLATFORM_SOURCES}

clean:
	rm -f otd-ipc-test-client

.PHONY: all clean proto-headers