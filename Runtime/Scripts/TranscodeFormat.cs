// Copyright (c) 2019 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace KtxUnity {

    // source: C++ enum ktx_transcode_fmt_e; ktx.h
    public enum TranscodeFormat : uint {
        // Compressed formats

        // ETC1-2
        ETC1_RGB = 0,							// Opaque only, returns RGB or alpha data if cDecodeFlagsTranscodeAlphaDataToOpaqueFormats flag is specified
        ETC2_RGBA = 1,							// Opaque+alpha, ETC2_EAC_A8 block followed by a ETC1 block, alpha channel will be opaque for opaque .basis files

        // BC1-5, BC7 (desktop, some mobile devices)
        BC1_RGB = 2,							// Opaque only, no punchthrough alpha support yet, transcodes alpha slice if cDecodeFlagsTranscodeAlphaDataToOpaqueFormats flag is specified
        BC3_RGBA = 3, 							// Opaque+alpha, BC4 followed by a BC1 block, alpha channel will be opaque for opaque .basis files
        BC4_R = 4,								// Red only, alpha slice is transcoded to output if cDecodeFlagsTranscodeAlphaDataToOpaqueFormats flag is specified
        BC5_RG = 5,								// XY: Two BC4 blocks, X=R and Y=Alpha, .basis file should have alpha data (if not Y will be all 255's)
        BC7_RGBA = 6,						// !< RGB or RGBA mode 5 for ETC1S,  modes 1, 2, 3, 4, 5, 6, 7 for UASTC.

        // PVRTC1 4bpp (mobile, PowerVR devices)
        PVRTC1_4_RGB = 8,						// Opaque only, RGB or alpha if cDecodeFlagsTranscodeAlphaDataToOpaqueFormats flag is specified, nearly lowest quality of any texture format.
        PVRTC1_4_RGBA = 9,					// Opaque+alpha, most useful for simple opacity maps. If .basis file doens't have alpha PVRTC1_4_RGB will be used instead. Lowest quality of any supported texture format.

        // ASTC (mobile, Intel devices, hopefully all desktop GPU's one day)
        ASTC_4x4_RGBA = 10,					// Opaque+alpha, ASTC 4x4, alpha channel will be opaque for opaque .basis files. Transcoder uses RGB/RGBA/L/LA modes, void extent, and up to two ([0,47] and [0,255]) endpoint precisions.

        // ATC (mobile, Adreno devices, this is a niche format)
        ATC_RGB = 11,							// Opaque, RGB or alpha if cDecodeFlagsTranscodeAlphaDataToOpaqueFormats flag is specified. ATI ATC (GL_ATC_RGB_AMD)
        ATC_RGBA = 12,							// Opaque+alpha, alpha channel will be opaque for opaque .basis files. ATI ATC (GL_ATC_RGBA_INTERPOLATED_ALPHA_AMD) 

        // FXT1 (desktop, Intel devices, this is a super obscure format)
        FXT1_RGB = 17,							// Opaque only, uses exclusively CC_MIXED blocks. Notable for having a 8x4 block size. GL_3DFX_texture_compression_FXT1 is supported on Intel integrated GPU's (such as HD 630).
                                                        // Punch-through alpha is relatively easy to support, but full alpha is harder. This format is only here for completeness so opaque-only is fine for now.
                                                        // See the BASISU_USE_ORIGINAL_3DFX_FXT1_ENCODING macro in basisu_transcoder_internal.h.

        PVRTC2_4_RGB = 18,					// Opaque-only, almost BC1 quality, much faster to transcode and supports arbitrary texture dimensions (unlike PVRTC1 RGB).
        PVRTC2_4_RGBA = 19,					// Opaque+alpha, slower to encode than PVRTC2_4_RGB. Premultiplied alpha is highly recommended, otherwise the color channel can leak into the alpha channel on transparent blocks.

        ETC2_EAC_R11 = 20,					// R only (ETC2 EAC R11 unsigned)
        ETC2_EAC_RG11 = 21,					// RG only (ETC2 EAC RG11 unsigned), R=opaque.r, G=alpha - for tangent space normal maps
        
        // Uncompressed (raw pixel) formats
        RGBA32 = 13,							// 32bpp RGBA image stored in raster (not block) order in memory, R is first byte, A is last byte.
        RGB565 = 14,							// 166pp RGB image stored in raster (not block) order in memory, R at bit position 11
        BGR565 = 15,							// 16bpp RGB image stored in raster (not block) order in memory, R at bit position 0
        RGBA4444 = 16,							// 16bpp RGBA image stored in raster (not block) order in memory, R at bit position 12, A at bit position 0
    }
}
