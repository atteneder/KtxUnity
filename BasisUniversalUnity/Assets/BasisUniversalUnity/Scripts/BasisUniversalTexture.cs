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

        public void Load( string filePath, MonoBehaviour monoBehaviour ) {
            monoBehaviour.StartCoroutine(LoadBasisFile(filePath));
        }

        IEnumerator LoadBasisFile(string filePath) {
    
            var path = Path.Combine(Application.streamingAssetsPath,filePath);

#if LOCAL_LOADING
            path = string.Format( "file://{0}", path );
#endif

            var webRequest = UnityWebRequest.Get(path);
            yield return webRequest.SendWebRequest();
            if(!string.IsNullOrEmpty(webRequest.error)) {
                yield break;
            }
            var bytes = webRequest.downloadHandler.data;

            var texture = BasisUniversal.LoadBytes(bytes);

            if(onTextureLoaded!=null) {
                onTextureLoaded(texture);
            }
        }
    }
}
