# BasisUniversalUnity

Demo project, integrating [Binomial LLC](http://www.binomial.info)'s [Basis Universal transcoder](https://github.com/BinomialLLC/basis_universal) into a Unity game engine project.

Special thanks to Binomial and everyone involved in making Basis Universal available!

## Building the native library

Check out this repository and make sure the sub-module `basis_universal` is also cloned.

### macOS

#### Prerequisites

You'll need [CMake](https://cmake.org) and Xcode (and its command line tools) installed.

#### Building the native library

Open up a terminal and navigate into the repository.

```
cd /path/to/BasisUniversalUnity
```

Create a subfolder `build`, enter it and call CMake like this:

```
mkdir build
cd build
cmake .. -G Xcode
```

This will generate an Xcode project called `open basisu_transcoder.xcodeproj`. Open it and build it.

This does the following:
- A library named `basisu.bundle` will be built and installed to `BasisUniversalUnity/BasisUniversalUnity/Assets/Plugins/x86_64`. This one is for usage in the Unity Editor and macOS standalone builds (latter untested).
- The source code for the transcoder + wrapper is copied to `BasisUniversalUnity/BasisUniversalUnity/Assets/Plugins/WebGL`. This way it gets compiled and included when you build the Unity project for WebGL.
- Two sample basis files get copied to the StreamingAssets folder.

### Other platforms (Linux,Windows)

Not tested at the moment. Probably needs some minor tweaks to run.

## Running the Unity project

This can only work if you've build the native library before.

In the project view, navigate to the file `BasisUniversalUnity/BasisUniversalUnity/Assets/Plugins/x86_64/basisu`, select it and make sure the option *Load on startup* is checked.

Other than that, open the `SampleScene` and click play. Two basis textures on planes should appear in front of you.

## Building Unity project

Just build like a regular project.

Note: Only WebGL is tested at the moment.

## Support

Like this demo? You can show your appreciation and ...

[![Buy me a coffee](https://az743702.vo.msecnd.net/cdn/kofi1.png?v=0)](https://ko-fi.com/C0C3BW7G)

## TODO

### Platform support
- Extending (transcoded) texture format support (now it's just BC1 and BC3)
- iOS support
- Android support

### General
- Remove memory leaks
- Create proper C# API
- Create DownloadHandler that provides a Texture2D
- Make a package (library)

### Basis Universal library
- Build for iOS
- Build for Android
- Watch out for useful changes in in Basis Universal project
  + public C bindings/interface
  + Separate encoder/transcoder libs
  + Multi platform support
