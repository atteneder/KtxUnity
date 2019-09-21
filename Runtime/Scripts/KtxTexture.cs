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
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Events;
using UnityEngine.Networking;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BasisUniversalUnity {
    public class KtxTexture
    {
        public event UnityAction<Texture2D> onTextureLoaded;

        /// <summary>
        /// Loads a KTX texture from the StreamingAssets folder
        /// see https://docs.unity3d.com/Manual/StreamingAssets.html
        /// </summary>
        /// <param name="filePath">Path to the file, relative to StreamingAssets</param>
        /// <param name="monoBehaviour">Can be any component. Used as loading Coroutine container. Make sure it is not destroyed before loading has finished.</param>
        public void LoadFromStreamingAssets( string filePath, MonoBehaviour monoBehaviour ) {
            var url = BasisUniversalTexture.GetStreamingAssetsUrl(filePath);
            monoBehaviour.StartCoroutine(LoadFile(url,monoBehaviour));
        }

        /// <summary>
        /// Loads a KTX texture from an URL
        /// </summary>
        /// <param name="url">URL to the basis file to load</param>
        /// <param name="monoBehaviour">Can be any component. Used as loading Coroutine container. Make sure it is not destroyed before loading has finished.</param>
        public void LoadFromUrl( string url, MonoBehaviour monoBehaviour ) {
            monoBehaviour.StartCoroutine(LoadFile(url,monoBehaviour));
        }

        /// <summary>
        /// Load a KTX texture from a buffer
        /// </summary>
        /// <param name="data">Native buffer that holds the basisu file</param>
        /// <param name="monoBehaviour">Can be any component. Used as loading Coroutine container. Make sure it is not destroyed before loading has finished.</param>
        public void LoadFromBytes( NativeArray<byte> data, MonoBehaviour monoBehaviour ) {
            monoBehaviour.StartCoroutine(LoadBytesRoutine(data));
        }

        IEnumerator LoadFile( string url, MonoBehaviour monoBehaviour ) {
    
            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if(!string.IsNullOrEmpty(webRequest.error)) {
                Debug.LogErrorFormat("Error loading {0}: {1}",url,webRequest.error);
                if(onTextureLoaded!=null) {
                    onTextureLoaded(null);
                }
                yield break;
            }

            var buffer = webRequest.downloadHandler.data;

            var na = new NativeArray<byte>(buffer,BasisUniversal.defaultAllocator);
            yield return monoBehaviour.StartCoroutine(LoadBytesRoutine(na));
            na.Dispose();
        }

        IEnumerator LoadBytesRoutine(NativeArray<byte> data) {

            uint imageIndex = 0;

            Texture2D texture = null;
            int status;

            var ktx = new KtxNativeInstance();

            if(ktx.Load(data)) {
                // var meta = transcoder.LoadMetaData();

                // TODO: Maybe do this somewhere more central
                TranscodeFormatHelper.Init();

                GraphicsFormat gf;
                TextureFormat? tf;
                TranscodeFormat transF;

                if(BasisUniversal.GetFormatsForImage(
                    ktx,
                    ktx,
                    out gf,
                    out tf,
                    out transF
                )) {
                    Profiler.BeginSample("KtxTranscode");
                    // var job = new BasisUniversalJob();

                    // job.imageIndex = imageIndex;
                    // job.levelIndex = 0;

                    // job.result = new NativeArray<bool>(1,BasisUniversal.defaultAllocator);

                    // var jobHandle = BasisUniversal.LoadBytesJob(
                    //     ref job,
                    //     transcoder,
                    //     data,
                    //     transF
                    //     );

                    var transcodeResult = ktx.Transcode(transF);
                    if(transcodeResult) {
                        Debug.Log("Transcode worked!");
                    }
                    Profiler.EndSample();
                    
                    // while(!jobHandle.IsCompleted) {
                        yield return null;
                    // }
                    // jobHandle.Complete();

                    if(transcodeResult /*job.result[0]*/) {
                        Profiler.BeginSample("LoadBytesRoutineGPUupload");
                        uint width = ktx.baseWidth;
                        uint height = ktx.baseHeight;
                        
                        if(tf.HasValue) {
                            texture = new Texture2D((int)width,(int)height,tf.Value,false);
                        } else {
                            texture = new Texture2D((int)width,(int)height,gf,TextureCreationFlags.None);
                        }
                        // texture.LoadRawTextureData(job.textureData);
                        ktx.LoadRawTextureData(texture);
                        texture.Apply();
                        Profiler.EndSample();
                    } else {
                        Debug.LogError("Transcoding failed!");
                    }
                    // job.textureData.Dispose();
                    // job.result.Dispose();
                }
            }
            
            // BasisUniversal.ReturnTranscoderInstance(transcoder);

            if(onTextureLoaded!=null) {
                onTextureLoaded(texture);
            }
        }

        /// <summary>
        /// Converts a relative sub path within StreamingAssets
        /// and creates an absolute URI from it. Useful for loading
        /// via UnityWebRequests.
        /// </summary>
        /// <param name="subPath">Path, relative to StreamingAssets. Example: path/to/file.basis</param>
        /// <returns>Platform independent URI that can be loaded via UnityWebRequest</returns>
        public static string GetStreamingAssetsUrl( string subPath ) {

            var path = Path.Combine(Application.streamingAssetsPath,subPath);

            #if LOCAL_LOADING
            path = string.Format( "file://{0}", path );
            #endif

            return path;
        }
    }
}