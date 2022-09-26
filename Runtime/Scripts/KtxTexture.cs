// Copyright (c) 2019-2022 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace KtxUnity {
    public class KtxTexture : TextureBase {

        KtxNativeInstance m_Ktx;

        /// <inheritdoc />
        public override ErrorCode Open(NativeSlice<byte> data) {
            m_Ktx = new KtxNativeInstance();
            return m_Ktx.Load(data);
        }

        public bool needsTranscoding => m_Ktx.needsTranscoding;
        public bool hasAlpha => m_Ktx.hasAlpha;
        public bool isPowerOfTwo => m_Ktx.isPowerOfTwo;
        public bool isMultipleOfFour => m_Ktx.isMultipleOfFour;
        public bool isSquare => m_Ktx.isSquare;
        public uint baseWidth => m_Ktx.baseWidth;
        public uint baseHeight => m_Ktx.baseHeight;
        public uint baseDepth => m_Ktx.baseDepth;
        public uint numLevels => m_Ktx.numLevels;
        public bool isArray => m_Ktx.isArray;
        public bool isCubemap => m_Ktx.isCubemap;
        public bool isCompressed => m_Ktx.isCompressed;
        public uint numDimensions => m_Ktx.numDimensions;
        public uint numLayers => m_Ktx.numLayers;
        public uint numFaces => m_Ktx.numFaces;
        public TextureOrientation orientation => m_Ktx.orientation;
        
        /// <inheritdoc />
        public override async Task<TextureResult> LoadTexture2D(
            bool linear = false,
            uint layer = 0,
            uint faceSlice = 0,
            uint mipLevel = 0,
            bool mipChain = true
            )
        {

            var result = new TextureResult();

            if(m_Ktx.valid) {
                if(m_Ktx.ktxClass==KtxClassId.ktxTexture2_c) {
                    if(m_Ktx.needsTranscoding) {
                        result.errorCode = await TranscodeInternal(m_Ktx,linear,layer,faceSlice,mipLevel);
                        // result.orientation = ktx.orientation;
                    }
                } else {
                    result.errorCode = ErrorCode.UnsupportedVersion;
                }
            } else {
                result.errorCode = ErrorCode.LoadingFailed;
            }

            if (result.errorCode != ErrorCode.Success) {
                return result;
            }
        
            Assert.IsTrue(m_Ktx.valid);
            Profiler.BeginSample("CreateTexture");
            
#if KTX_UNITY_GPU_UPLOAD
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3
               )
            {
                m_Ktx.EnqueueForGpuUpload();
                
                Texture2D texture;
                bool success;
                while (!m_Ktx.TryCreateTexture(out texture, out success, m_Format)) {
                    Profiler.EndSample();
                    await Task.Yield();
                }
                
                if (success) {
                    return new TextureResult {
                        texture = texture
                    };
                }
                return new TextureResult(ErrorCode.LoadingFailed);
            }
#endif

            try {
                var texture = m_Ktx.LoadTextureData(
                    m_Format,
                    layer,
                    mipLevel,
                    faceSlice,
                    mipChain
                    );
                result.texture = texture;
            }
            catch (UnityException) {
                result.errorCode = ErrorCode.LoadingFailed;
            }
            
            Profiler.EndSample();
            return result;
        }
        
        /// <inheritdoc />
        public override void Dispose() {
            m_Ktx.Unload();
        }

        async Task<ErrorCode> TranscodeInternal(
            KtxNativeInstance ktx,
            bool linear,
            uint layer,
            uint faceSlice,
            uint mipLevel
            )
        {

            if (layer >= (isArray ? numLayers : 1) ) {
                return ErrorCode.InvalidLayer;
            }

            if (isCubemap && faceSlice >= numFaces) {
                return ErrorCode.InvalidFace;
            }

            if ( numDimensions > 2 && faceSlice >= baseDepth) {
                return ErrorCode.InvalidSlice;
            }

            if (mipLevel >= numLevels) {
                return ErrorCode.InvalidLevel;
            }
            
            // TODO: Maybe do this somewhere more central
            TranscodeFormatHelper.Init();

            var result = ErrorCode.Success;
            
            var formats = GetFormat(ktx,ktx,linear);

            if(formats.HasValue) {
                m_Format = formats.Value.format;

                if(m_Format== GraphicsFormat.RGBA_DXT5_SRGB && !m_Ktx.hasAlpha) {
                    // ktx library automatically decides to use the smaller
                    // DXT1 instead of DXT5 if no alpha channel is present
#if UNITY_2018_3_OR_NEWER
                    m_Format = GraphicsFormat.RGBA_DXT1_SRGB;
#else
                    m_Format = GraphicsFormat.RGB_DXT1_SRGB;
#endif
                }

#if KTX_VERBOSE
                Debug.LogFormat("Transcode to GraphicsFormat {0} ({1})",m_Format,formats.Value.transcodeFormat);
#endif
                Profiler.BeginSample("KtxTranscode");

                var job = new KtxTranscodeJob();
                
                var jobHandle = ktx.LoadBytesJob(
                    ref job,
                    formats.Value.transcodeFormat
                    );

                Profiler.EndSample();
                
                while(!jobHandle.IsCompleted) {
                    await Task.Yield();
                }
                jobHandle.Complete();

                if(job.result[0] != KtxErrorCode.KTX_SUCCESS) {
                    result = ErrorCode.TranscodeFailed;
                }
                job.result.Dispose();
            } else {
                result = ErrorCode.UnsupportedFormat;
            }
            return result;
        }
    }
}