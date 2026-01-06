cmake_minimum_required(VERSION 3.25..4.2 FATAL_ERROR)
project(OTDIPC-Test-Client LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_EXTENSIONS OFF)

file(GLOB PROTO_HEADERS "${CMAKE_SOURCE_DIR}/../include/OTD-IPC/*.hpp")

add_executable(
  otd-ipc-test-client
  OTDIPC-TestClient.cpp
  Transport.hpp
  Transport.cpp
  "${PROTO_HEADERS}"
)
target_include_directories(
  otd-ipc-test-client
  PRIVATE
  "${CMAKE_SOURCE_DIR}/../include"
)

set(
  POSIX_SOURCES
  PosixSocket.cpp PosixSocket.hpp
  unique_fd.hpp
)

if (WIN32)
  target_sources(
    otd-ipc-test-client
    PRIVATE
    WindowsTransport.cpp
    WindowsTransport.hpp
  )
elseif(APPLE)
  target_sources(
    otd-ipc-test-client
    PRIVATE
    DarwinTransport.cpp
    DarwinTransport.hpp
    "${POSIX_SOURCES}"
  )
elseif(LINUX)
  target_sources(
    otd-ipc-test-client
    PRIVATE
    LinuxTransport.cpp
    LinuxTransport.hpp
    "${POSIX_SOURCES}"
  )
endif()

if (MSVC)
  target_compile_options(
    otd-ipc-test-client
    PRIVATE
    /EHsc
  )
endif ()
