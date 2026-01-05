# OTD-IPC Protocol Documentation

**Version:** v2.20260105.02

This document describes the V2 protocol used for communication between OpenTabletDriver and client applications.

## Overview

The V2 protocol enables exclusive, real-time access to tablet state from OpenTabletDriver. When a client connects, the active OpenTabletDriver output mode is disabled, giving the client direct access to tablet input.

### Transport Layer

Communication uses Unix domain sockets on all platforms (Windows, macOS, Linux). The socket path varies by implementation to support multiple servers, and is discovered through a file-based discovery mechanism (see [Discovery Mechanism](#discovery-mechanism)).


**Legacy Note:** The obsolete V1 protocol used Windows named pipes in message mode; later versions moved to Unix domain sockets on all platforms for consistency, as Windows gained support for Unix sockets in the Windows 10 October 2018 Update.

### Connection Model

- **Exclusive Access:** Only one client may connect at a time
- **Server-Initiated Messages:** The server pushes state updates to the client as they occur
- **Persistent Connection:** Client maintains a long-lived connection to receive continuous updates

## Protocol Flow

### 1. Discovery Phase

Before connecting, the client must discover the socket path:

1. Read `%LOCALAPPDATA%/otd-ipc/servers/v2/default.txt` to get the default implementation ID
2. Read `%LOCALAPPDATA%/otd-ipc/servers/v2/available/{id}.txt` to get server metadata including the socket path
3. Extract the `SOCKET=` value from the metadata file

See [Discovery Mechanism](#discovery-mechanism) for details.

### 2. Connection Phase

The client connects to the Unix domain socket at the discovered path.

### 3. Handshake Phase

Upon client connection, the server sends:

1. **DebugMessage** containing implementation identification (e.g., `"OTD-IPC: 'OTDIPC' v1.0.0 running on 'OpenTabletDriver' v0.6.0"`)
2. **DeviceInfo** message if a tablet device is currently connected (optional, only sent if device is present)

### 4. Operational Phase

After handshake, the server continuously streams:

- **State** messages whenever tablet state changes (position, pressure, buttons, proximity)
- **Ping** messages periodically to verify connection health
- **DeviceInfo** messages when the tablet device changes (connect/disconnect/switch)

The client reads and processes messages in a continuous loop.

### Message Flow Diagram

```
Client                          Server
  |                               |
  |-- Connect to socket --------->|
  |                               |
  |<-- DebugMessage --------------|  (implementation ID and version)
  |<-- DeviceInfo ----------------|  (if device present)
  |                               |
  |<-- State ---------------------|  (on tablet events)
  |<-- State ---------------------|
  |<-- Ping ----------------------|  (periodic keepalive)
  |<-- State ---------------------|
  |<-- DeviceInfo ----------------|  (on device change)
  |<-- State ---------------------|
  |           ...                 |
```

The `DebugMessage` is optional, but strongly recommended for debugging and visbility.

## Message Types

All messages follow the same structure: a header followed by message-specific data.

| Message Type | ID | Description | Direction |
|--------------|-----|-------------|-----------|
| **DeviceInfo** | 1 | Tablet device information | Server → Client |
| **State** | 2 | Current tablet state | Server → Client |
| **Ping** | 3 | Connection keepalive | Server → Client |
| **DebugMessage** | 4 | Variable-length debug string | Server → Client |
| **Experimental** | 5 | GUID-identified experimental messages | Server ↔ Client |

### Message Details

#### Header

Present in all messages. Contains message identification and routing information.

**Fields:**
- `messageType` (uint32): Message type ID from the table above
- `size` (uint32): Total message size in bytes, including header
- `nonPersistentTabletId` (uint32): Tablet identifier (may change on disconnect/reconnect)

#### DeviceInfo (ID: 1)

Sent when a client connects (if device present) or when the tablet device changes.

**Fields:**
- All Header fields
- `maxX` (float): Maximum X coordinate in device units
- `maxY` (float): Maximum Y coordinate in device units
- `maxPressure` (uint32): Maximum pressure value
- `vendorId` (uint16): USB Vendor ID *(deprecated, use persistentId)*
- `productId` (uint16): USB Product ID *(deprecated, use persistentId)*
- `persistentId` (char[256]): Unique device identifier string (UTF-8)
    - this will be null-terminated if the string is shorter than 256 bytes
- `name` (char[256]): Human-readable device name (UTF-8)
    - this will be null-terminated if the string is shorter than 256 bytes

#### State (ID: 2)

Sent whenever tablet state changes. Contains position, pressure, button, and proximity information.

**Fields:**
- All Header fields
- `validBits` (ValidMask): Bitmask indicating which fields contain valid data
- `x` (float): X position in device units
- `y` (float): Y position in device units
- `pressure` (uint32): Pen pressure
- `penButtons` (uint32): Pen button state (bitmask, bit N = button N)
- `auxButtons` (uint32): Auxiliary button state (bitmask, bit N = button N)
- `hoverDistance` (uint32): Distance above surface in device-specific units
- `penIsNearSurface` (bool): Whether pen is in proximity of tablet surface

**ValidMask values:**

- `None` = 0
- `PositionX` = 1 << 0,
- `PositionY` = 1 << 1,
- `Pressure` = 1 << 2,
- `PenButtons` = 1 << 3,
- `AuxButtons` = 1 << 4,
- `PenIsNearSurface` = 1 << 5,
- `HoverDistance` = 1 << 6,

Clients MUST check `validBits` before using each field. Only fields with corresponding bits set contain valid data.

#### Ping (ID: 3)

Sent periodically by the server to maintain connection and verify client responsiveness.

**Fields:**
- All Header fields
- `sequenceNumber` (uint64): Incrementing sequence number

Clients should read and ignore Ping messages. If pings stop arriving, the connection may be broken.

#### DebugMessage (ID: 4)

Variable-length message containing UTF-8 text for debugging/logging purposes.

**Structure:**
- Header with `size` field indicating total message size
- UTF-8 text data (length = `size - sizeof(Header)`)

The text is NOT null-terminated. Use the size field to determine text length.

#### Experimental (ID: 5)

Reserved for private experimentation. Format:
- Header
- GUID (16 bytes in Win32/C# binary layout)
- Variable-length payload

The GUID identifies the actual type of the message, and the meaning of the payload.

**Not recommended for production use.** Consider contributing new message types to OTD-IPC instead.

## Binary Format

### Encoding Rules

- **Structure Layout:** All structures use natural alignment (C/C++ default, C# `StructLayout.LayoutKind.Sequential` with `Pack=0`)
- **Byte Order:** Native endianness (little-endian on x86/x64/ARM platforms)
- **Integer Types:**
  - `uint16_t` / `UInt16`: 16-bit unsigned integer
  - `uint32_t` / `UInt32`: 32-bit unsigned integer
  - `uint64_t` / `UInt64`: 64-bit unsigned integer
- **Floating Point:** IEEE 754 single-precision (32-bit)
- **Boolean:** Single byte (0 = false, non-zero = true)
- **Strings:** Fixed-size buffers containing null-terminated UTF-8 text

### Size Validation

Implementations MUST:
1. Verify that the message size from the header matches the number of bytes read
2. Verify that the message size is AT LEAST as large as expected for the struct
3. Accept messages larger than expected (forward compatibility - server may send extended messages)

### Reference Implementations

| Type | C++                                     | C#                              |
|------|-----------------------------------------|---------------------------------|
| `enum MessageType` | [.hpp](../include/OTD-IPC/MessageType.hpp) | [.cs](../OTDIPC/V2/MessageType.cs) |
| `struct Header` | [.hpp](../include/OTD-IPC/Header.hpp)      | [.cs](../OTDIPC/V2/Header.cs)      |
| ✉️ `struct DeviceInfo` | [.hpp](../include/OTD-IPC/DeviceInfo.hpp)  | [.cs](../OTDIPC/V2/DeviceInfo.cs)  |
| ✉️ `struct Ping` | [.hpp](../include/OTD-IPC/Ping.hpp)        | [.cs](../OTDIPC/V2/Ping.cs)        |
| ✉️ `struct State` | [.hpp](../include/OTD-IPC/State.hpp)       | [.cs](../OTDIPC/V2/State.cs)       |

**Example Client:** See [OTDIPC-TestClient.cpp](../OTDIPC-TestClient/OTDIPC-TestClient.cpp) for a complete C++20 example implementation.

## Discovery Mechanism

The V2 protocol uses a file-based discovery mechanism to locate available servers and their socket paths.

### Cross-Platform Paths

All discovery files are located under `{LocalApplicationData}/otd-ipc/servers/v2`; `{LocalApplicationData}` is resolved as follows:
- **Windows:** `%LOCALAPPDATA%`
- **Linux:** `$XDG_DATA_DIR` (typically `~/.local/share`)
- **macOS:** `{ApplicationSupport}` (typically `~/Library/Application Support`)

### Directory Structure

```
{LocalApplicationData}/
└── otd-ipc/
    └── servers/
        └── v2/
            ├── default.txt           # Contains default implementation ID
            └── available/
                └── {id}.txt          # Metadata file per implementation
```

### Discovery Process

#### Client Discovery

1. **Read default ID:** Open `{LocalApplicationData}/otd-ipc/servers/v2/default.txt`
2. **Parse ID:** Read the file contents and trim whitespace to get the implementation ID
3. **Read metadata:** Open `{LocalApplicationData}/otd-ipc/servers/v2/available/{id}.txt`
4. **Extract socket path:** Parse metadata file for the line beginning with `SOCKET=`
5. **Connect:** Open Unix domain socket connection to the extracted path

#### Server Publishing

When the server starts:

1. **Choose a socket path:**:implementations should choose a socket path unique to their implementation; for example,
    while `OpenKneeboard/OTD-IPC` uses `otd-ipc/sock`, *no other implementations should create their sockets inside
    the `otd-ipc` folder
2. **Create socket:** Bind to Unix domain socket at chosen path
3. **Publish metadata:** Write metadata file to `{LocalApplicationData}/otd-ipc/servers/v2/available/{implementation-id}.txt`
4. **Set default (if needed):** If `default.txt` doesn't exist, create it with this server's implementation ID

### Metadata File Format

The metadata file is a UTF-8 text file with `\n` line endings, containing `KEY=VALUE` pairs, one per line.

**Required Fields:**
- `ID`: Implementation identifier (e.g., `otd-ipc.openkneeboard.com`)
- `SOCKET`: Absolute path to Unix domain socket

**Optional Fields:**
- `NAME`: Human-readable server name
- `SEMVER`: Semantic version string
- `DEBUG_VERSION`: Detailed version information
- `HOMEPAGE`: URL to project homepage

**Example:**
```
ID=otd-ipc.openkneeboard.com
NAME=OpenTabletDriver OTD-IPC Plugin
SEMVER=1.0.0+revision.42
DEBUG_VERSION=OTD-IPC v1.0.0.42/OpenTabletDriver v0.6.4.0
HOMEPAGE=https://github.com/OpenKneeboard/OTD-IPC
SOCKET=/home/user/.local/share/otd-ipc/sock
```

### Implementation Notes

- Servers MUST NOT replace an existing `default.txt` on regular startup
- Servers MAY notify the user that they are not default, and provide an opt-in path
- Servers MUST NOT persist an opt-in choice to replace the default
- Servers MAY replace an existing `default.txt` on installation, as long as 'installation' is not considered part of regular startup

- Servers SHOULD clean up their metadata file on clean shutdown
- Clients SHOULD handle stale files gracefully
- Clients MUST handle strings that completely fill fixed-sized buffers gracefully; these strings only have a terminating null if they are shorter than the buffer size
- Servers SHOULD delete existing socket files before binding (Unix requirement)
- Clients SHOULD verify the socket is connectable before assuming a server is available
- Clients SHOULD attempt to use the default implementation, unless otherwise specified
- Clients MAY use other discoverable implementations, e.g. if the default is unreachable or due to explicit user selection
- Servers MUST make the `ID=` field of the metadata file match the implementation ID component of the filename
- Servers MUST use an absolute path for the `SOCKET=` field
- All implementations MUST ignore unrecognized message types
- All implementations MUST allow messages to be larger than expected for forwards compatibility
  - implementations MUST ignore the additional bytes
- Implementations MUST NOT append additional data to the messages, unless either:
  - the extensions have been accepted and merged into this protocol specification
  - the message type is `Experimental` (5), and the GUID is unique to the implementation
- Servers MUST write discovery text files using the UTF-8 encoding
- Servers MUST use `\n` (LF) at the end of each line in discovery text files
- Servers MUST NOT use `\r\n` (CRLF) as line endings
- Servers MAY include a newline (`\n`) at the end of the file/the final line
- Client SHOULD handle discovery files both with and without a trailing newline
- Multiple servers MAY publish different metadata files simultaneously, but only one default can exist

## Implementation Checklist

### Client Implementation

- [ ] Implement discovery mechanism to find socket path
- [ ] Connect to Unix domain socket
- [ ] Read messages in a loop
- [ ] Parse Header from each message
- [ ] Validate `size` field matches bytes read
- [ ] Validate `size` >= expected struct size
- [ ] Switch on `messageType` to handle each message
- [ ] Check `validBits` in State messages before using fields
- [ ] Handle connection errors gracefully
- [ ] Ignore unknown message types (forward compatibility)

### Server Implementation

- [ ] Create Unix domain socket at chosen path
- [ ] Clean up old socket file before binding
- [ ] Publish discovery metadata file
- [ ] Set default.txt if it doesn't exist
- [ ] Accept client connection (one at a time)
- [ ] Send DebugMessage with implementation ID and version information on connect
- [ ] Send DeviceInfo on connect (if device present)
- [ ] Send State messages when tablet state changes
- [ ] Send Ping messages periodically
- [ ] Send DeviceInfo when device changes
- [ ] Handle client disconnect gracefully
- [ ] Accept next client after disconnect

## Version History

- **V2** (current): Unix domain sockets, discovery mechanism, improved device identification
- **V1** (obsolete): Windows named pipes, fixed pipe name, limited to Windows

Applications should implement V2 only. V1 support is maintained for backward compatibility but not recommended for new implementations.
