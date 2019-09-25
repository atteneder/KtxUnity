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

namespace BasisUniversalUnity {
    public class KtxTexture : TextureBase
    {
        protected override IEnumerator LoadBytesRoutine(NativeArray<byte> data) {

            Texture2D texture = null;

            var ktx = new KtxNativeInstance();

            if(ktx.Load(data)) {

                // TODO: Maybe do this somewhere more central
                TranscodeFormatHelper.Init();

                GraphicsFormat gf;
                TextureFormat? tf;
                TranscodeFormat transF;

                if(TranscodeFormatHelper.GetFormatsForImage(
                    ktx,
                    ktx,
                    out gf,
                    out tf,
                    out transF
                )) {
                    Profiler.BeginSample("KtxTranscode");

                    var job = new KtxTranscodeJob();
                    
                    var jobHandle = ktx.LoadBytesJob(
                        ref job,
                        transF
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
                        
                        if(tf.HasValue) {
                            texture = new Texture2D((int)width,(int)height,tf.Value,false);
                        } else {
                            texture = new Texture2D((int)width,(int)height,gf,TextureCreationFlags.None);
                        }
                        ktx.LoadRawTextureData(texture);
                        texture.Apply();
                        Profiler.EndSample();
                    } else {
                        Debug.LogError("Transcoding failed!");
                    }
                    job.result.Dispose();
                }
            }
            OnTextureLoaded(texture);
        }
    }
}