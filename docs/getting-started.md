# Getting Started

## Installing OpenTabletDriver

First, remove any other tablet drivers; this includes any drivers from:
- Wacom
- Huion
- VRKneeboard

[TabletDriverCleanup](https://github.com/X9VoiD/TabletDriverCleanup) is a useful tool to make sure previous drivers have been completely removed.

Once you have removed other tablet drivers, [install OpenTabletDriver](https://opentabletdriver.net/Wiki/Install/Windows), and configure your tablet however you want it outside of OpenKneeboard.

## Installing OpenKneeboard OTD-IPC

Download `OpenKneeboard-OTC-IPC-vVERSION.zip` from [the latest release](https://github.com/OpenKneeboard/OTD-IPC/releases/latest) - but don't unzip it.

Open OpenTabletDriver, then open it's plugin manager:

![](getting-started/open-plugin-manager.png)

Drag-and-drop the OpenKneeboard-OTC-IPC .zip from your Downloads folder to the plugin manager Window; you should then see "OpenKneeboard OTD-IPC" in the list on the left, and when you click it, you should see something like this:

![](getting-started/with-otd-ipc.png)

Finally, close the plugin manager window.

## Enabling OpenKneeboard OTD-IPC

![](getting-started/filter-settings.png)

1. Select the tablet you want to use with OpenKneeboard in the bottom left
2. Switch to the filters tab
3. Select OpenKneeboard
4. Tick the 'Enable OpenKneeboard (OTD-IPC)' box
5. Click 'Apply' to apply your changes; click 'Save' too to automatically enable OTD-IPC when OpenTabletDriver starts.

Repeat these steps for each tablet that you want to use with OpenKneeboard.

When OpenKneeboard (or another application using OTD-IPC) is active, everything you do with the tablet will be sent to OpenKneeboard instead of your usual OpenTabletDriver output and bindings.
