# BasisUniversalUnity

Demo project, integrating [Binomial LLC](http://www.binomial.info)'s [Basis Universal transcoder](https://github.com/BinomialLLC/basis_universal) into a Unity game engine project.

Special thanks to Binomial and everyone involved in making Basis Universal available!

## Building the native library

Check out this repository and make sure the sub-module `basis_universal` is also cloned.

### Prerequisites

You'll need [CMake](https://cmake.org)

All build platform variants will have an `install` build target, that does the following:

This does the following:

- The final library will be installed to the correct place within `BasisUniversalUnity/BasisUniversalUnity/Assets/Plugins`.
- The source code for the transcoder + wrapper is copied to `BasisUniversalUnity/BasisUniversalUnity/Assets/Plugins/WebGL`. This way it gets compiled and included when you build the Unity project for WebGL.
- Two sample basis files get copied to the StreamingAssets folder.

### macOS

Install Xcode and its command line tools.

#### Build (for macOS Unity Editor and standalone builds)

Open up a terminal and navigate into the repository.

```bash
cd /path/to/BasisUniversalUnity
```

Create a subfolder `build`, enter it and call CMake like this:

```bash
mkdir build
cd build
cmake .. -G Xcode
```

This will generate an Xcode project called `basisu_transcoder.xcodeproj`.

Open it and build it (target `ALL_BUILD` or `basisu`).

After this was successful, build the target `install`.

### Build for iOS

You'll need Xcode and its command line tools installed.

Create a subfolder `build_ios`, enter it and call CMake like this:

```bash
mkdir build_ios
cd build_ios
cmake .. \
-DCMAKE_TOOLCHAIN_FILE="../cmake/ios.toolchain.cmake"
```

This will generate an Xcode project called `basisu_transcoder.xcodeproj`.

Open it and build it (target `ALL_BUILD` or `basisu`).

After this was successful, build the target `install`.

### Build for Android

You'll need the Android NDK

Create a subfolder `build_android_arm64`, enter it and call CMake like this:

```bash
mkdir build_android_arm64
cd build_android_arm64
cmake .. \
-DANDROID_ABI=arm64-v8a \
-DCMAKE_BUILD_TYPE=RelWithDebInfo \
-DANDROID_NDK=/path/to/your/android/sdk/ndk-bundle \
-DCMAKE_TOOLCHAIN_FILE=/path/to/your/android/sdk/ndk-bundle/build/cmake/android.toolchain.cmake \
-DANDROID_STL=c++_static
```

Replace `/path/to/your/android/sdk/ndk-bundle` with the actual path to your Android NDK install.

To build and install

```bash
make && make install
```

### Other platforms (Linux,Windows,iOS)

Not tested at the moment. Probably needs some minor tweaks to run.

## Running the Unity project

This can only work if you've build the native library before.

In the project view, navigate to the file `BasisUniversalUnity/BasisUniversalUnity/Assets/Plugins/x86_64/basisu`, select it and make sure the option *Load on startup* is checked.

Other than that, open the `SampleScene` and click play. Two basis textures on planes should appear in front of you.

## Building Unity project

Just build like a regular project.

Note: Only WebGL and Android is tested at the moment.

## Support

Like this demo? You can show your appreciation and ...

[![Buy me a coffee](https://az743702.vo.msecnd.net/cdn/kofi1.png?v=0)](https://ko-fi.com/C0C3BW7G)

## License

Copyright (c) 2019 Andreas Atteneder, All Rights Reserved.
Licensed under the Apache License, Version 2.0 (the "License");
you may not use files in this repository except in compliance with the License.
You may obtain a copy of the License at

   <http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

## Third party

References [Binomial LLC](http://www.binomial.info)'s [Basis Universal transcoder](https://github.com/BinomialLLC/basis_universal) (released under the terms of Apache License 2.0)

Uses Alexander Widerberg's CMake iOS toolchain ( <https://github.com/leetal/ios-cmake> )
