CC=cc
CXX=c++
CXX_STANDARD=23
PLATFORM:=$(shell uname -s)
PROTO_HEADERS:=$(wildcard ../include/OTD-IPC/*.h)

all: otd-ipc-test-client

proto-headers: $(PROTO_HEADERS)

otd-ipc-test-client: OTDIPC-TestClient.cpp PipeClient.h ${PLATFORM}PipeClient.h ${PLATFORM}PipeClient.cpp $(PROTO_HEADERS)
	"${CXX}" -o "$@" "--std=c++${CXX_STANDARD}" -I ../include OTDIPC-TestClient.cpp ${PLATFORM}PipeClient.cpp

clean:
	rm -f otd-ipc-test-client

.PHONY: all clean proto-headers