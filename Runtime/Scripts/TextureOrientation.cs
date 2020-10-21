// Copyright (c) 2020 Andreas Atteneder, All Rights Reserved.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


namespace KtxUnity {

    /// <summary>
    /// See Section 5.2 in http://github.khronos.org/KTX-Specification
    /// </summary>
    [System.Flags]
    public enum TextureOrientation {
        KTX_DEFAULT = 0b000,
        X_LEFT = 0b001,
        Y_UP = 0b010,
        Z_IN = 0b100, // Not used at the moment
        /// <summary>
        /// Unity expects GPU textures to be X=right Y=up
        /// </summary>
        UNITY_DEFAULT = Y_UP,
    }

    public static class TextureOrientationExtension {

        /// <summary>
        /// Evaluates if the texture's horizontal orientation conforms to Unity's default.
        /// If it's not aligned (=true; =flipped), the texture has to be applied mirrored horizontally.
        /// </summary>
        /// <param name="to"></param>
        /// <returns>True if the horizontal orientation is flipped, false otherwise</returns>
        public static bool IsXFlipped(this TextureOrientation to) {
            // Unity default == X_RIGHT
            return (to & TextureOrientation.X_LEFT) != 0;
        }

        /// <summary>
        /// Evaluates if the texture's vertical orientation conforms to Unity's default.
        /// If it's not aligned (=true; =flipped), the texture has to be applied mirrored vertically.
        /// </summary>
        /// <param name="to"></param>
        /// <returns>True if the vertical orientation is flipped, false otherwise</returns>
        public static bool IsYFlipped(this TextureOrientation to) {
            // Unity default == Y_UP
            return (to & TextureOrientation.Y_UP) == 0;
        }
    }
}
