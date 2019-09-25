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

    public class BasisUniversalTexture : TextureBase
    {
        protected override IEnumerator LoadBytesRoutine(NativeArray<byte> data) {

            uint imageIndex = 0;

            Texture2D texture = null;
            
            var transcoder = BasisUniversal.GetTranscoderInstance();

            while(transcoder==null) {
                yield return null;
                transcoder = BasisUniversal.GetTranscoderInstance();
            }

            if(transcoder.Open(data)) {
                var meta = transcoder.LoadMetaData();

                GraphicsFormat gf;
                TextureFormat? tf;
                TranscodeFormat transF;

                if(BasisUniversal.GetFormats(
                    meta,
                    imageIndex,
                    out gf,
                    out tf,
                    out transF
                )) {
                    Profiler.BeginSample("BasisUniversalJob");
                    var job = new BasisUniversalJob();

                    job.imageIndex = imageIndex;
                    job.levelIndex = 0;

                    job.result = new NativeArray<bool>(1,KtxNativeInstance.defaultAllocator);

                    var jobHandle = BasisUniversal.LoadBytesJob(
                        ref job,
                        transcoder,
                        data,
                        transF
                        );

                    Profiler.EndSample();
                    
                    while(!jobHandle.IsCompleted) {
                        yield return null;
                    }
                    jobHandle.Complete();

                    if(job.result[0]) {
                        Profiler.BeginSample("LoadBytesRoutineGPUupload");
                        uint width;
                        uint height;
                        meta.GetSize(out width,out height);
                        if(tf.HasValue) {
                            texture = new Texture2D((int)width,(int)height,tf.Value,false);
                        } else {
                            texture = new Texture2D((int)width,(int)height,gf,TextureCreationFlags.None);
                        }
                        texture.LoadRawTextureData(job.textureData);
                        texture.Apply();
                        Profiler.EndSample();
                    } else {
                        Debug.LogError("Transcoding failed!");
                    }
                    job.textureData.Dispose();
                    job.result.Dispose();
                }
            }
            
            BasisUniversal.ReturnTranscoderInstance(transcoder);

            OnTextureLoaded(texture);
        }
    }
}
