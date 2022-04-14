// Copyright (c) 2019-2022 Andreas Atteneder, All Rights Reserved.

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

namespace KtxUnity.Editor {

    public static class OnScriptsReloadHandler {

        const string k_PkgName = "KtxUnity";
        const string k_PkgLegacyVersion = "1.x";
        const string k_PkgMinVersion = "2.0.0";
        
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded() {
#if UNITY_WEBGL && !UNITY_2021_2_OR_NEWER
            Debug.LogError($"Downgrade {k_PkgName} to version {k_PkgLegacyVersion}!\nWebGL builds will not succeed with {k_PkgName} {k_PkgMinVersion} or newer and Unity versions older than 2021.2.");
#endif
        }
    }
}
