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

namespace KtxUnity {
    public class KtxTexture : TextureBase {

        KtxNativeInstance m_Ktx;

        public override ErrorCode Load(NativeSlice<byte> data) {
            m_Ktx = new KtxNativeInstance();
            return m_Ktx.Load(data);
        }
        
        public override async Task<ErrorCode> Transcode(bool linear = false) {

            ErrorCode result;

            if(m_Ktx.valid) {
                if(m_Ktx.ktxClass==KtxClassId.ktxTexture2_c) {
                    if(m_Ktx.needsTranscoding) {
                        result = await TranscodeInternal(m_Ktx,linear);
                        // result.orientation = ktx.orientation;
                    } else {
                        result = ErrorCode.NotSuperCompressed;
                    }
                } else {
                    result = ErrorCode.UnsupportedVersion;
                }
            } else {
                result = ErrorCode.LoadingFailed;
            }
            return result;
        }

        public override TextureResult CreateTexture() {
            TextureResult result;
            Profiler.BeginSample("LoadBytesRoutineGpuUpload");
            try {
                var texture = m_Ktx.LoadTextureData(m_Format);
                result = new TextureResult {
                    texture = texture
                };
            }
            catch (UnityException) {
                result = new TextureResult(ErrorCode.TranscodeFailed);
            }
            
            Profiler.EndSample();
            return result;
        }
        
        public override void Dispose() {
            m_Ktx.Unload();
        }

        async Task<ErrorCode> TranscodeInternal(KtxNativeInstance ktx, bool linear) {
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