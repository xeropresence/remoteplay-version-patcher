# remoteplay-version-patcher

This program will patch RemotePlay.exe to update its file and product version to the latest version provided by Sony.

What this lets you do is use older versions of Remote Play without updating.

# What it does

When you run the patcher, it:

1. Looks for RemotePlay.exe in the same folder as the patcher.
2. If no local copy exists, looks for an installed PS Remote Play copy through the Windows registry.
3. Calls Sony's Remote Play version endpoint:

   https://remoteplay.dl.playstation.net/remoteplay/module/win/rp-version-win.json

4. Reads the current version from that response.
5. Updates only the executable's file version and product version resources.

The patcher does not download or install Remote Play, modify any other files, or send your local executable to Sony. It only makes a remote call to fetch Sony's current version metadata.

# Usage

[Download the latest release](https://github.com/xeropresence/remoteplay-version-patcher/releases/)

Run remoteplay-version-patcher.exe as admin.

It will first look for RemotePlay.exe in the same folder as the patcher. If it finds it there, it will patch that copy.

If there is no local copy, it will attempt to locate the installed RemotePlay.exe through the Windows registry. If found, it will patch the installed executable in place.

If the patcher cannot locate RemotePlay.exe through the registry, place RemotePlay.exe in the same folder as the patcher and run it again. After patching that copied executable, return the patched executable to the Remote Play folder.

# Thanks and credits

Based on the idea from this reddit post

https://www.reddit.com/r/remoteplay/comments/r24n95/tutorial_how_to_fix_ps_remote_play_4508250_and/

FindRemotePlay function updated from MysteryDash/Offline-PS4-Remote-Play
