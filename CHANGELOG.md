# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 
### Changed
- WebGL library is built with Emscripten 2.0.19 now
- Minimum required version is Unity 2021.2

## [1.1.2] - 2021-10-27
### Added
- Error message when users try to run KtxUnity 1.x Unity >=2021.2 combination targeting WebGL

## [1.1.1] - 2021-07-16
### Changed
- Updated KTX-Software-Unity to [0.4.2](https://github.com/atteneder/KTX-Software-Unity/releases/tag/v0.4.2) (only the relevant iOS binaries)
### Fixed
- Bitcode is now embed in all iOS binaries (fixes #37)

## [1.1.0] - 2021-07-02
### Added
- Support for Lumin / Magic Leap
- Support for Apple Silicon on macOS via a universal library 
### Changed
- Updated KTX-Software-Unity to [0.4.1](https://github.com/atteneder/KTX-Software-Unity/releases/tag/v0.4.1)
### Fixed
- Prevent crash during mipmap reverting on recent llvm/emscripten versions
- Not transcoding to ETC1/ETC2/BC1 if resolution is not a multiple of four
- Switched to data-model-independent types in C binding to avoid crashes on certain platforms

## [1.0.0] - 2021-02-03
### Added
- Support for Universal Windows Platform (x64,x86,ARM,ARM64)
### Changed
- Switched API to `async` calls that return a `TextureResult` directly (instead of onTextureLoaded event)
- Doesn't require a MonoBehaviour for running coroutines anymore
- Raised minimum required version to 2019.2 (the version that switched to scripting runtime version .NET 4.6)
- Updated KTX-Software-Unity native libs to [0.3.0](https://github.com/atteneder/KTX-Software-Unity/releases/tag/v0.3.0)

## [0.9.1] - 2020-11-12
### Changed
- Updated KTX-Software-Unity native libs to [0.2.4](https://github.com/atteneder/KTX-Software-Unity/releases/tag/v0.2.4)
### Fixed
- Added missing native functions (on Linux)

## [0.9.0] - 2020-11-04
### Added
- Support for Universal Windows Platform (x64)
- Expressive error messages when loading unsupported KTX 1.0 or non-supercompressed KTX 2.0 file
### Changed
- Updated KTX-Software-Unity native libs to [0.2.2](https://github.com/atteneder/KTX-Software-Unity/releases/tag/v0.2.2)
### Fixed
- Added missing basis transcoding functions on Windows (fixes #21)
- UASTC mode with alpha channel

## [0.8.2] - 2020-10-23
- No changes. Bump release to trick OpenUPM to create package for 0.8.1

## [0.8.1] - 2020-10-23
### Fixed
- Removed annoying warning about `UnityPackage.meta` file

## [0.8.0] - 2020-10-23
### Added
- Texture orientation is now exposed. This allows users to correct (=flip) them (fixes #18)
- Support for KTX specification 2.0 pr-draft2 (fixes #16)
- Support for Basis Universal UASTC supercompression mode (higher quality)
### Changed
- Native binary libs are now provided by [KTX-Software-Unity 0.1.0](https://github.com/atteneder/KTX-Software-Unity/releases/tag/v0.1.0)
- The KTX specification changed (from ~draft20 to pr-draft2), thus older KTX files cannot be loaded anymore.
- Unsupported basis file texture types (non 2D Images) raise a proper error now
- Removed support for 32-bit Desktop platforms (Windows, Linux)

## [0.7.0] - 2020-04-26
### Added
- Support for linear sampling
- Support for `ETC1_RGB` with sRGB sampling via `RGB_ETC2_SRGB`
- Support for `ETC2_EAC_R11` and `ETC2_EAC_RG11` (there's no interface yet to explicitly choose one- or two-channel textures)

## [0.6.0] - 2020-03-01
### Added
- Support for mip-map levels
### Changed
- Updated KTX library (now at KTX 2.0 specification draft 18)

## [0.5.0] - 2020-02-22
### Added
- Support for Universal Windows Platform (not verified/tested myself)
### Changed
- `TextureBase.LoadBytesRoutine` is public now to allow deeper integration
### Fixed
- Ensured backwards compatibility with Unity 2018.2

## [0.4.0] - 2019-11-10
### Changed
- Renamed project to KtxUnity
- Using less memory by freeing up texture after GPU upload
### Added
- Support for loading KTX 2.0 files with Basis Universal super-compression

## [0.3.0] - 2019-06-30
### Added
- Thread support via Unity Job system
- Support for Android armeabi-v7a and x86

## [0.2.0] - 2019-06-25
### Added
- Support for Linux 32/64 bit

## [0.1.0] - 2019-06-23
### Added
- Added support for Windows 32/64 bit
- Docs on how to use it

### Changed
- Restructured project to be a valid Unity package

## [0.0.1] - 2019-06-21
### Added
- Changelog. All previous work was not versioned.
