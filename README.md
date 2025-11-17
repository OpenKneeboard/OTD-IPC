# OpenKneeboard OTD-IPC

This is a filter plugin for [OpenTabletDriver], allowing 1 program at a time to directly read the state of the tablet. When in use, the active OpenTabletDriver output mode is disabled.

It is primarily intended for use with [OpenKneeboard], but OpenKneeboard is not required.

## Getting Started

See [the Getting Started guide](docs/getting-started.md).

## Getting Help

I make this for my own use, and I share this in the hope others find it useful; I'm not able to commit to support, bug fixes, or feature development.    

Support may be available from the community via [Discord].

## Protocol

Current release versions are only usable on Windows. `master` is also usable on MacOS, though is a volatile work in progress; it should be relatively straightforward to also make `master` also work on Linux.


In the current Windows-only version, communication is over a named pipe in message mode. The named pipe is called:
- `"com.fredemmott.openkneeboard.OTDIPC/v0.1"` in .Net named pipe APIs
- `"\\.\pipe\com.fredemmott.openkneeboard.OTDIPC/v0.1"` when using `CreateFile()` or similar APIs, e.g. in C++

In `master`, communication is over a Unix socket with a varying path, to allow multiple server implementations. Socket paths can be [discovered](https://github.com/OpenKneeboard/OTD-IPC/issues/18#issuecomment-3542327853).

One client is supported at a time - this is intended to be an exclusive mode.

Messages are defined as structs, and every message stars with a header containing the message type and size.

Implementations SHOULD verify that the message size is the same as the size in the header, and that the size is AT LEAST as large as the expected size of the struct. The server MAY send extended messages which are larger than expected.

If a device is connected, the server will send a `DeviceInfo` message when a client connects to the named pipe.

If state is available, the server will send a `State` message when a client connects to the named pipe.

A C++20 [example client](OTDIPC-TestClient/OTDIPC-TestClient.cpp) is included.

### Data types

| Type | C++ | C# |
|------|-----|----|
| `enum MessageType` | [.h](include/OTD-IPC/MessageType.h) | [.cs](OTDIPC/MessageType.cs) |
| `struct Header` | [.h](include/OTD-IPC/Header.h) | [.cs](OTDIPC/Header.cs) |
| ✉️ `struct DeviceInfo` | [.h](include/OTD-IPC/DeviceInfo.h) | [.cs](OTDIPC/DeviceInfo.cs) |
| ✉️ `struct Ping` | [.h](include/OTD-IPC/Ping.h) | [.cs](OTDIPC/Ping.cs) |
| ✉️ `struct State` | [.h](include/OTD-IPC/State.h) | [.cs](OTDIPC/State.cs) |

## License

OpenKneeboard OTD-IPC is licensed under [the MIT license](LICENSE); however, note that the plugin uses interfaces defined in OpenTabletDriver itself, which are licensed under [their own terms](OpenTabletDriver-LICENSE), which may apply to the compiled plugin as-distributed.

[Discord]: https://go.openkneeboard.com/discord
[OpenKneeboard]: https://github.com/OpenKneeboard/OpenKneeboard
[OpenTabletDriver]: https://opentabletdriver.net/
