﻿// Copyright (c) 2020-2021 Andreas Atteneder, All Rights Reserved.

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
    
    /// <summary>
    /// TextureResult encapsulates result of texture loading. The texture itself and its orientation.
    /// </summary>
    public class TextureResult {
        public Texture2D texture;
        public TextureOrientation orientation;
        public ErrorCode errorCode = ErrorCode.Success;

        public TextureResult() {}
        
        public TextureResult(ErrorCode errorCode) {
            this.errorCode = errorCode;
        }

        public TextureResult(Texture2D texture, TextureOrientation orientation) {
            this.texture = texture;
            this.orientation = orientation;
        }
    }
}
