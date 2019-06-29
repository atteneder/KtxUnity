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

namespace BasisUniversalUnity {
    public class MetaData
    {
        public bool hasAlpha;

        public ImageInfo[] images;

        public void GetSize(out uint width, out uint height, uint imageIndex=0, uint levelIndex=0) {
            var level = images[imageIndex].levels[levelIndex];
            width = level.width;
            height = level.height;
        }

        public override string ToString() {
            return string.Format("BU images:{0} A:{1}",images.Length,hasAlpha);
        }
    }

    public class ImageInfo {
        public LevelInfo[] levels;
        public override string ToString() {
            return string.Format("Image levels:{0}",levels.Length);
        }
    }

    public class LevelInfo {
        public uint width;
        public uint height;

        static bool IsPowerOfTwo(uint i) {
            return (i&(i-1))==0;
        }

        public bool isPowerOfTwo {
            get {
                return IsPowerOfTwo(width) && IsPowerOfTwo(height);
            }
        }

        public bool isSquare {
            get {
                return width==height;
            }
        }

        public override string ToString() {
            return string.Format("Level size {0} x {1}", width, height);
        }
    }
}
