# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
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
