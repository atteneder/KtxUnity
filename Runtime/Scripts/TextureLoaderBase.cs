// Copyright (c) 2019-2021 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

namespace KtxUnity {
    public abstract class TextureLoaderBase : MonoBehaviour
    {
        /// <summary>
        /// Example method for applying a loaded texture.
        /// </summary>
        /// <param name="result">Result of loading a texture</param>
        protected void OnTextureLoaded(TextureResult result) {
            if(result!=null) {
                ApplyTexture(result);
            } else {
                Debug.LogError("Loading Texture failed!");
            }
        }

        protected abstract void ApplyTexture(TextureResult result);
    }
}
