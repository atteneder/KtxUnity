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

using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace KtxUnity {
    public class KtxTexture : TextureBase
    {
        public override async Task<TextureResult> LoadBytesRoutine(NativeSlice<byte> data, bool linear = false) {

            var ktx = new KtxNativeInstance(data);
            TextureResult result;

            if(ktx.valid) {
                if(ktx.ktxClass==KtxClassId.ktxTexture2_c) {
                    if(ktx.needsTranscoding) {
                        result = await Transcode(ktx,linear);
                        result.orientation = ktx.orientation;
                    } else {
                        result = new TextureResult(ErrorCode.NotSuperCompressed);
                    }
                } else {
                    result = new TextureResult(ErrorCode.UnsupportedVersion);
                }
            } else {
                result = new TextureResult(ErrorCode.LoadingFailed);
            }
            ktx.Unload();
            return result;
        }

        async Task<TextureResult> Transcode(KtxNativeInstance ktx, bool linear) {
            // TODO: Maybe do this somewhere more central
            TranscodeFormatHelper.Init();

            TextureResult result = null;
            
            var formats = GetFormat(ktx,ktx,linear);

            if(formats.HasValue) {
                var gf = formats.Value.format;
#if KTX_VERBOSE
                Debug.LogFormat("Transcode to GraphicsFormat {0} ({1})",gf,formats.Value.transcodeFormat);
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

                if(job.result[0] == KtxErrorCode.KTX_SUCCESS) {
                    Profiler.BeginSample("LoadBytesRoutineGPUupload");

                    if(formats.Value.format== GraphicsFormat.RGBA_DXT5_SRGB && !ktx.hasAlpha) {
                        // ktx library automatically decides to use the smaller DXT1 instead of DXT5 if no alpha
#if UNITY_2018_3_OR_NEWER
                        gf = GraphicsFormat.RGBA_DXT1_SRGB;
#else
                        gf = GraphicsFormat.RGB_DXT1_SRGB;
#endif
                    }
                    try {
                        var texture = ktx.LoadTextureData(gf);
                        result = new TextureResult {
                            texture = texture
                        };
                    }
                    catch (UnityException) {
                        result = new TextureResult(ErrorCode.TranscodeFailed);
                    }
                    Profiler.EndSample();
                } else {
                    result = new TextureResult(ErrorCode.TranscodeFailed);
                }
                job.result.Dispose();
            } else {
                result = new TextureResult(ErrorCode.UnsupportedFormat);
            }
            return result;
        }
    }
}