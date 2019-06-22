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

#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace BasisUniversalUnity {

    public class BasisUniversalTexture
    {
        public event UnityAction<Texture2D> onTextureLoaded;

        public BasisUniversalTexture() {
            BasisUniversal.Init();
        }

        /// <summary>
        /// Loads a Basis Universal texture from the StreamingAssets folder
        /// see https://docs.unity3d.com/Manual/StreamingAssets.html
        /// </summary>
        /// <param name="filePath">Path to the file, relative to StreamingAssets</param>
        /// <param name="monoBehaviour">Can be any component. Used as loading Coroutine container. Make sure it is not destroyed before loading has finished.</param>
        public void LoadFromStreamingAssets( string filePath, MonoBehaviour monoBehaviour ) {
            var url = GetStreamingAssetsUrl(filePath);
            monoBehaviour.StartCoroutine(LoadBasisFile(url));
        }

        /// <summary>
        /// Loads a Basis Universal texture from an URL
        /// </summary>
        /// <param name="url">URL to the basis file to load</param>
        /// <param name="monoBehaviour">Can be any component. Used as loading Coroutine container. Make sure it is not destroyed before loading has finished.</param>
        public void LoadFromUrl( string url, MonoBehaviour monoBehaviour ) {
            monoBehaviour.StartCoroutine(LoadBasisFile(url));
        }

        IEnumerator LoadBasisFile(string url) {
    
            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if(!string.IsNullOrEmpty(webRequest.error)) {
                Debug.LogErrorFormat("Error loading {0}: {1}",url,webRequest.error);
                if(onTextureLoaded!=null) {
                    onTextureLoaded(null);
                }
                yield break;
            }
            var bytes = webRequest.downloadHandler.data;

            var texture = BasisUniversal.LoadBytes(bytes);

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
        string GetStreamingAssetsUrl( string subPath ) {

            var path = Path.Combine(Application.streamingAssetsPath,subPath);

            #if LOCAL_LOADING
                        path = string.Format( "file://{0}", path );
            #endif

            return path;
        }
    }
}
