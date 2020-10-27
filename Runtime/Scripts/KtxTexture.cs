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

using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace KtxUnity {
    public class KtxTexture : TextureBase
    {
        Texture2D texture;

        public override IEnumerator LoadBytesRoutine(NativeSlice<byte> data, bool linear = false) {

            var orientation = TextureOrientation.UNITY_DEFAULT;

            var ktx = new KtxNativeInstance(data);

            if(ktx.valid) {
                orientation = ktx.orientation;
                yield return Transcode(ktx,linear);
            }
            ktx.Unload();
            OnTextureLoaded(texture,orientation);
        }

        IEnumerator Transcode(KtxNativeInstance ktx, bool linear) {
            // TODO: Maybe do this somewhere more central
            TranscodeFormatHelper.Init();

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
                    yield return null;
                }
                jobHandle.Complete();

                if(job.result[0] == KtxErrorCode.KTX_SUCCESS) {
                    Profiler.BeginSample("LoadBytesRoutineGPUupload");
                    uint width = ktx.baseWidth;
                    uint height = ktx.baseHeight;

                    if(formats.Value.format== GraphicsFormat.RGBA_DXT5_SRGB && !ktx.hasAlpha) {
                        // ktx library automatically decides to use the smaller DXT1 instead of DXT5 if no alpha
#if UNITY_2018_3_OR_NEWER
                        gf = GraphicsFormat.RGBA_DXT1_SRGB;
#else
                        gf = GraphicsFormat.RGB_DXT1_SRGB;
#endif
                    }
                    try {
                        texture = ktx.LoadTextureData(gf);
                    }
                    catch (UnityException) {
                        Debug.LogError(ERR_MSG_TRANSCODE_FAILED);
                        texture = null;
                    }
                    Profiler.EndSample();
                } else {
                    Debug.LogError(ERR_MSG_TRANSCODE_FAILED);
                }
                job.result.Dispose();
            }
        }
    }
}