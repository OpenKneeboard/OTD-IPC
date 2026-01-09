# OpenKneeboard OTD-IPC

This is a filter plugin for [OpenTabletDriver], allowing 1 program at a time to directly read the state of the tablet. When in use, the active OpenTabletDriver output mode is disabled.

It is primarily intended for use with [OpenKneeboard], but OpenKneeboard is not required.

Currently released versions are only usable on Windows. `master` is also usable on macOS and Linux, though is a work in progress.

For more information, see [the project website](https://otd-ipc.openkneeboard.com).

## License

OpenKneeboard OTD-IPC is licensed under [the MIT license](LICENSE); however, note that the plugin uses interfaces defined in OpenTabletDriver itself, which are licensed under [their own terms](OpenTabletDriver-LICENSE), which may apply to the compiled plugin as-distributed.

[Discord]: https://go.openkneeboard.com/discord
[OpenKneeboard]: https://github.com/OpenKneeboard/OpenKneeboard
[OpenTabletDriver]: https://opentabletdriver.net/
