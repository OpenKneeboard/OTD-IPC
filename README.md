# OpenKneeboard OTD-IPC

This is an OutputMode plugin for [OpenTabletDriver], allowing 1 program at a time to directly read the state of the tablet.

It is primarily intended for use with [OpenKneeboard].

## Getting Help

I make this for my own use, and I share this in the hope others find it useful; I'm not able to commit to support, bug fixes, or feature development.    

Support may be available from the community via [Discord].

## Protocol

Communication is over a named pipe in message mode. The named pipe is called:
- `"com.fredemmott.openkneeboard.OTDIPC/v0.1"` in .Net named pipe APIs
- `"\\\\.\\pipe\\com.fredemmott.openkneeboard.OTDIPC/v0.1"` when using `CreateFile()` or similar APIs, e.g. in C++

One client is supported at a time - this is intended to be an exclusive mode.

Messages are defined as structs, and every message stars with a header containing the message type and size.

Implementations SHOULD verify that the message size is the same as the size in the header, and that the size is AT LEAST as large as the expected size of the struct. The server MAY send extended messages which are larger than expected.

If a device is connected, the server will send a `DeviceInfo` message when a client connects to the named pipe.

If state is available, the server will send a `State` message when a client connects to the named pipe.

### Data types

- MessageType enum: [C++](include/OTD-IPC/MessageType.h) - [C#](OTDIPC/MessageType.cs)
- Header: [C++](include/OTD-IPC/Header.h) - [C#](OTDIPC/Header.cs)
- DeviceInfo message: [C++](include/OTD-IPC/DeviceInfo.h) - [C#](OTDIPC/DeviceInfo.cs)
- State message: [C++](include/OTD-IPC/State.h) - [C#](OTDIPC/State.cs)

## License

OpenKneeboard OTD-IPC is licensed under [the ISC license]; however, note that the plugin uses interfaces defined in OpenTabletDriver itself, which are licensed under [their own terms](OpenTabletDriver-LICENSE), which may apply to the plugin as a whole.

[Discord]: https://go.openkneeboard.com/discord
[OpenKneeboard]: https://github.com/OpenKneeboard/OpenKneeboard
[OpenTabletDriver]: https://opentabletdriver.net/
