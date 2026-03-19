# Protocol

**Version:** v2.20260205.01

**Status:** Draft

This document describes the OTD-IPC V2 protocol used for communication between OpenTabletDriver and client applications.

**Warning:** As of 2026-03-19, all published releases of OTD-IPC implement the v1 protocol, not this protocol.

## Overview

The V2 protocol enables exclusive background access to tablet state from OpenTabletDriver. When a client connects, the active OpenTabletDriver output mode is disabled, giving the client direct access to tablet input.

If your application does not *require* exclusive background access to the tablet, you should not use OTD-IPC; instead, you should look at:

- tablet/pen/'pointer' input APIs in the UI framework you are using
- platform-specific input APIs such as Windows Ink or WinTab

Exclusive background access ignores most user settings and plugins, including bindings, smoothing, etc; most applications do not want this behavior.

OpenKneeboard is an example of an application that *does* require this kind of access to the tablet:

- as a virtual reality tool, monitors and windows are not visible to the user; monitor mapping and other similar options do not make sense
- the OS cursor is not visible in VR, so moving the OS cursor does not make sense, and neither do related behaviors like 'window under cursor'
- as an external overlay for games, it must be able to access the tablet even when the game window is on top of it
- it is essentially treating the tablet as a game controller

However, because of these choices, OpenKneeboard does need to reimplement some basic optional features that are usually provided by the driver, such as rotation and bindings.

### Transport Layer

Communication uses Unix domain sockets on all platforms (Windows, macOS, Linux). The socket path varies by implementation to support multiple servers, and is discovered through a file-based discovery mechanism (see [Discovery Mechanism](#discovery-mechanism)).

**Legacy Note:** The obsolete V1 protocol used Windows named pipes in message mode; later versions moved to Unix domain sockets on all platforms for consistency, as Windows gained support for Unix sockets in the Windows 10 October 2018 Update.

### Connection Model

- **Exclusive Access:** Only one client may connect at a time
- **Server-Initiated Messages:** The server pushes state updates to the client as they occur
- **Persistent Connection:** Client maintains a long-lived connection to receive continuous updates

This is primarily intended for exclusive access while gaming; as such, while in-use, servers SHOULD:

- expose the full tablet area, ignoring any active area clipping or monitor mapping, unless the user has explicitly indicated they want this to affect OTD-IPC
- expose all buttons, suppressing their usual behavior, unless the user has explicitly indicated they want to use their usual bindings while OTD-IPC is active

## Implementation Names, IDs, and Versions

Names, IDs, and versions serve two primary purposes in this protocol:

- human-readable values: useful for debug logs and user interfaces, e.g. for allowing the user to select between implementations, or showing error messages
- machine-readable values: also useful for debug logs. Allows unambiguous identification of peers

Separate fields are used for these, both in server ↔ client 'Hello' messages, and in server metadata:

- **implementationID**: a unique identifier for the implementation
  - implementations MAY use any UTF-8 string that uniquely identifies the implementation
  - implementations SHOULD use a developer-readable 'owned' identifier, e.g. `myproject.mydomain.com`, `github.com/myusername/myproject', etc
  - implementations MUST NOT routinely change this value
    - it would perhaps be appropriate after a major rewrite
    - implementations MUST NOT change this value after purely user-facing rebrands
- **humanReadableName**: a user-friendly 'product name'
  - implementations SHOULD prefer showing this over 'implementation ID' in most user-facing situations; a counter-example would be debug logs
  - implementations SHOULD NOT use this to identify specific implementations in code
- **compatibilityVersion**: `uint8_t` (0-255): machine-readable version suitable for gating behavior changes/workarounds
  - implementations SHOULD initially use `1` as the value
  - implementations MAY increment this value when major issues are fixed (i.e. if it is known or likely that other implementations have blocked or added workarounds for previous versions)
  - implementations MUST NOT change this value as a routine part of their release process
  - this field is intentionally too small to support reasonable encodings of regular version numbers, as they do not fit the requirements above
- **humanReadableVersion**: a user-friendly version identifier
  - implementations MAY store any valid UTF-8 string here
  - implementations MUST treat this as an opaque-string
  - implementations MUST NOT assume this string matches any particular pattern, e.g. `a.b.c.d` or semver
  - implementations MUST NOT use this field to identify specific versions in code, except if the `implementationID` is recognized as an implementation that does not use the `compatibilityVersion` field correctly

If implementations need to match another implementation (e.g. to add implementation-specific workarounds or block known-buggy versions):

- implementations SHOULD check the `implementationID` exactly matches
- implementations SHOULD restore standard behavior for later (unrecognized) `compatibilityVersion` values 
- implementations MAY unconditionally match a given `implementationID` (or use other factors) if the `compatibilityVersion` is not used in a way that is fit for purpose, e.g. if the problematic implementation routinely updates it without fixing blocking issues


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

Upon client connection, the server:

1. SHOULD send **Hello** containing implementation identification
2. MUST send **DeviceInfo** message if a tablet device is currently connected
3. MUST NOT send **DeviceInfo** message if no tablet device is connected

Upon connection, the client:

1. SHOULD send **Hello** containing implementation identification

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
  |--------------------- Hello -->|  (client implementation ID and version)
  |<-- Hello ---------------------|  (server implementation ID and version)
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

The `Hello` is optional, but strongly recommended for debugging and visibility.

The client and server can both send `DebugMessage` at any time

## Message Types

All messages follow the same structure: a header followed by message-specific data.

| Message Type | ID | Description                           | Direction |
|--------------|-----|---------------------------------------|-----------|
| **DeviceInfo** | 1 | Tablet device information             | Server → Client |
| **State** | 2 | Current tablet state                  | Server → Client |
| **Ping** | 3 | Connection keepalive                  | Server → Client |
| **DebugMessage** | 4 | Variable-length UTF-8 string          | Server ↔ Client |
| **Experimental** | 5 | GUID-identified experimental messages | Server ↔ Client |
| **Hello** | 6 | Implementation identifier | Server ↔ Client |

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
- `persistentId` (char[256]): Unique device identifier string (UTF-8)
    - this will be null-terminated if the string is shorter than 256 bytes
- `name` (char[256]): Human-readable device name (UTF-8)
    - this will be null-terminated if the string is shorter than 256 bytes

#### State (ID: 2)

Sent whenever tablet state changes. Contains position, pressure, button, and proximity information.

**Fields:**
- All Header fields
- `validBits` (ValidMask): Bitmask indicating which fields contain valid data
- `x` (float): Horizontal distance from left edge in device units
- `y` (float): Vertical distance from top edge in device units
- `pressure` (uint32): Pen pressure
- `penButtons` (uint32): Pen button state (bitmask, bit N = button N). Button 0 (LSB) is reserved for the pen tip
- `auxButtons` (uint32): Auxiliary button state (bitmask, bit N = button N)
- `hoverDistance` (uint32): Distance above surface in device-specific units
- `penIsNearSurface` (bool): Whether pen is in proximity of tablet surface

Notes:

- *(0, 0)* is the top left corner; coordinates increase to the right and down.
- button 0 MUST be set if the pen is in contact with the surface
- button 0 MUST NOT be set if the pen is not in contact surface
- if there is no actual pen tip button, the server MUST synthesize button 0 from pressure data, and bit-shift the other buttons (if any) along

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

- Both clients and servers MAY send this at any time
- Both clients and servers SHOULD send this message shortly after connection, identifying the implementation and version

#### Experimental (ID: 5)

Reserved for private experimentation. Format:
- Header
- GUID (16 bytes in Win32/C# binary layout)
- Variable-length payload

The GUID identifies the actual type of the message, and the meaning of the payload.

**Not recommended for production use.** Consider contributing new message types to OTD-IPC instead.

##### Hello (ID: 6)

- Header
- `uint64_t`: protocolVersion, `0xAAYYYYMMDDBB` - e.g. `v2.20260203.01` -> `0x0220260301`
- 256-byte string: humanReadableName
- 256-byte string: humanReadableVersion
- 256-byte string: implementationID
  - this SHOULD be a developer-readable 'owned' ID, e.g. a domain name, project URL, or `yourusername.github.io/yourproject`
  - for server → client Hello, this SHOULD match the name in the metadata files
- `uint8_t`: compatiblityVersion
  - '1' should be used as an initial value
  - implementations SHOULD NOT update this value as a regular part of this release process
  - implementations SHOULD update this value if major issues are fixed that may have led to other implementations choosing to blacklist previous versions
  - this field is intentionally too small to support reasonable encodings of actual version numbers, to encourage the behavior described above

This message SHOULD be sent by both the client and server shortly after connection.

Implementations MAY log these values.
Implementations MUST NOT change behavior based on these values, except for:

 - logging and similar metrics
 - working around known issues in other implementations

### Encoding Rules

- **Structure Layout:** All structures use natural alignment (C/C++ default, C# `StructLayout.LayoutKind.Sequential` with `Pack=0`)
- **Byte Order:** Native endianness (little-endian on x86/x64/ARM platforms)
- **Integer Types:**
  - `uint16_t` / `UInt16`: 16-bit unsigned integer
  - `uint32_t` / `UInt32`: 32-bit unsigned integer
  - `uint64_t` / `UInt64`: 64-bit unsigned integer
- **Floating Point:** IEEE 754 single-precision (32-bit)
- **Boolean:** Single byte (0 = false, non-zero = true)
- **Strings:** arrays of UTF-8 bytes. Fixed-length strings are null-terminated, UNLESS the string completely fills the field, in which case there is no terminator

### Size Validation

Implementations MUST:
1. Verify that the message size from the header matches the number of bytes read
2. Verify that the message size is AT LEAST as large as expected for the struct
3. Accept messages larger than expected (forward compatibility - server may send extended messages)

|------|-----------------------------------------|---------------------------------|
| `enum MessageType` | [.hpp](../include/OTD-IPC/MessageType.hpp) | [.cs](../OTDIPC/V2/MessageType.cs) |
| `struct Header` | [.hpp](../include/OTD-IPC/Header.hpp)      | [.cs](../OTDIPC/V2/Header.cs)      |
| ✉️ `struct DeviceInfo` | [.hpp](../include/OTD-IPC/DeviceInfo.hpp)  | [.cs](../OTDIPC/V2/DeviceInfo.cs)  |
| ✉️ `struct Ping` | [.hpp](../include/OTD-IPC/Ping.hpp)        | [.cs](../OTDIPC/V2/Ping.cs)        |
| ✉️ `struct State` | [.hpp](../include/OTD-IPC/State.hpp)       | [.cs](../OTDIPC/V2/State.cs)       |
| ✉️ `struct DebugMessage` | [.hpp](../include/OTD-IPC/DebugMessage.hpp)       | [.cs](../OTDIPC/V2/DebugMessage.cs)       |
| ✉️ `struct Hello` | [.hpp](../include/OTD-IPC/Hello.hpp)       | [.cs](../OTDIPC/V2/Hello.cs)       |

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
- `PROTOCOL_VERSION`: AA.YYYYMMDD.BB, e.g `2.20260203.01`
- `HUMAN_READABLE_NAME`: Human-readable server name
- `HUMAN_READABLE_VERSION`: Human-readable version number
- `COMPATIBLITY_VERSION`: 0-255 value, suitable for numeric comparisons to identify major bugfixes. If absent, `0` should be assumed
- `HOMEPAGE`: URL to project homepage

**Example:**
```
ID=otd-ipc.openkneeboard.com
HUMAN_READABLE_NAME=OpenTabletDriver OTD-IPC Plugin
HUMAN_READABLE_VERSION=OTD-IPC v1.0.0.42/OpenTabletDriver v0.6.4.0
COMPATIBLITY_VERSION=1
HOMEPAGE=https://github.com/OpenKneeboard/OTD-IPC
SOCKET=/home/user/.local/share/otd-ipc/sock
```

### Implementation Notes

- Servers MUST NOT replace an existing `default.txt` on regular startup
- Servers MAY notify the user that they are not default, and provide an opt-in path
- Servers MUST NOT persist an opt-in choice to replace the default
- Servers MAY replace an existing `default.txt` on installation, as long as 'installation' is not considered part of regular startup
- Servers MUST accept messages from the clients
- Servers MUST fully consume all messages from the clients, including ones with an unrecognized message type; the size from the header should be used to read the payload
- Servers MUST NOT require that message types are recognized

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
- All implementations MUST NOT change behavior based on IDs, versions, or other equivalent checks, except to work around known issues
  - Implementations SHOULD make reasonable efforts to report any discovered issues to the maintainers of the problematic implementation
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
- [ ] Send Hello with implementation ID and version information on connect
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
- [ ] Send Hello with implementation ID and version information on connect
- [ ] Send DeviceInfo on connect (if device present)
- [ ] Send State messages when tablet state changes
- [ ] Send Ping messages periodically
- [ ] Send DeviceInfo when device changes
- [ ] Fully consume all messages from the client using the header size
- [ ] Handle client disconnect gracefully
- [ ] Accept next client after disconnect

## Version History

- **V2** (current): Unix domain sockets, discovery mechanism, improved device identification
- **V1** (obsolete): Windows named pipes, fixed pipe name, limited to Windows

Applications should implement V2 only. V1 support is maintained for backward compatibility but not recommended for new implementations.
