# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.2.3] - 2022-11-18
### Fixed
- WebGL build errors (unresolved symbols during linking) due to outdated native library

## [2.2.2] - 2022-09-29
### Changed
- Marked `TextureBase.LoadBytesRoutine` obsolete in favor of `TextureBase.LoadFromBytes`
### Fixed
- Made `TextureBase.LoadBytesRoutine` public again to preserve API compatibility

## [2.2.1] - 2022-09-28
### Added
- Low-level API that gives finer control over the loading process (alternative to `TextureBase.LoadBytesRoutine`)
- `ManagedNativeArray` wrapper for efficient access of managed `byte[]` from C# Jobs
- Basis Universal texture (.basis) Editor import
- Support for loading any single image/layer/mipmap level of textures of any type (arrays, cubemaps, volumes)
- Support for discarding mipmap level chain (and import just one mipmap level)
- Support for importing Basis Universal texture types 2darray, 3d, video, cubemap (single images only at the moment)
- Many property getters on `KtxTexture` (e.g. `needsTranscoding`,`hasAlpha`,`isCubemap`)
- Experimental support for importing uncompressed KTX 2.0 textures (limited set of formats for now; #47)
### Changed
- De-prioritized texture formats `RGBA4444` and `BGR565` to avoid incorrect channel usage or low quality. Transcoding to `RGBA32` instead.
- Bumped minimum required Unity version to 2019.4 LTS
### Fixed
- Textures with alpha channel are shown blended (with checkerboard background) in the inspector now (`alphaIsTransparency` is enabled; #64)
- Avoid extra memcopy of input data by using `ManagedNativeArray` in `LoadFromStreamingAssets` and `LoadFromUrl`
- Improved texture format usage detection (linear vs. sRGB sampling)
- Loading of native library on Linux (thanks [@Blackclaws][Blackclaws] for #60)

## [2.1.2] - 2022-04-14
### Fixed
- Fix undefined variable error when building project
- Compiler error due to C# 7.3 incompatible code

## [2.1.1] - 2022-04-14
Ported changes from 1.2.0
### Added
- Editor Import via `ScriptableImporter` (thanks @hybridherbst][hybridherbst] for #45)
- Error Codes (in `TextureResult.errorCode`)
### Changed
- In release builds there's no console logging anymore (use the `errorCode` instead). In Debug builds and the Editor you still get detailed error messages.
### Fixed
- Will not transcode textures with sizes that are not a multiple of four to incompatible DXT5 or BC7 formats anymore
- Re-compiled macOS native library in release mode (`MinSizeRel`; was `Debug`). Expect improved performance.

## [2.0.1] - 2021-11-23
### Fixed
- Apple Silicon Unity Editor decoding

## [2.0.0] - 2021-10-28
### Changed
- WebGL library is built with Emscripten 2.0.19 now
- Minimum required version is Unity 2021.2

## [1.3.2] - 2023-04-11
### Fixed
- Apple Silicon Unity Editor decoding

## [1.3.1] - 2022-11-29
### Changed
- Marked `TextureBase.LoadBytesRoutine` obsolete in favor of `TextureBase.LoadFromBytes`
### Fixed
- Made `TextureBase.LoadBytesRoutine` public again to preserve API compatibility

## [1.3.0] - 2022-11-28
### Added
- Low-level API that gives finer control over the loading process (alternative to `TextureBase.LoadBytesRoutine`)
- `ManagedNativeArray` wrapper for efficient access of managed `byte[]` from C# Jobs
- Basis Universal texture (.basis) Editor import
- Support for loading any single image/layer/mipmap level of textures of any type (arrays, cubemaps, volumes)
- Support for discarding mipmap level chain (and import just one mipmap level)
- Support for importing Basis Universal texture types 2darray, 3d, video, cubemap (single images only at the moment)
- Many property getters on `KtxTexture` (e.g. `needsTranscoding`,`hasAlpha`,`isCubemap`)
- Experimental support for importing uncompressed KTX 2.0 textures (limited set of formats for now; #47)
### Changed
- De-prioritized texture formats `RGBA4444` and `BGR565` to avoid incorrect channel usage or low quality. Transcoding to `RGBA32` instead.
- Bumped minimum required Unity version to 2019.4 LTS
### Fixed
- Textures with alpha channel are shown blended (with checkerboard background) in the inspector now (`alphaIsTransparency` is enabled; #64)
- Avoid extra memcopy of input data by using `ManagedNativeArray` in `LoadFromStreamingAssets` and `LoadFromUrl`
- Improved texture format usage detection (linear vs. sRGB sampling)

## [1.2.3] - 2022-04-14
### Fixed
- Fix undefined variable error when building project

## [1.2.2] - 2022-04-14
### Fixed
- Compiler error due to C# 7.3 incompatible code

## [1.2.1] - 2022-04-14
### Added
- Editor Import via `ScriptableImporter` (thanks @hybridherbst][hybridherbst] for #45)
- Error Codes (in `TextureResult.errorCode`)
### Changed
- In release builds there's no console logging anymore (use the `errorCode` instead). In Debug builds and the Editor you still get detailed error messages.
### Fixed
- Will not transcode textures with sizes that are not a multiple of four to incompatible DXT5 or BC7 formats anymore
- Re-compiled macOS native library in release mode (`MinSizeRel`; was `Debug`). Expect improved performance.

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

[Blackclaws]: https://github.com/Blackclaws
[hybridherbst]: https://github.com/hybridherbst
