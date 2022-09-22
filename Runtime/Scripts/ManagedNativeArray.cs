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

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace KtxUnity {

    /// <summary>
    /// Wraps a managed byte[] in a NativeArray&lt;byte&gt;without copying memory.
    /// </summary>
    public class ManagedNativeArray : IDisposable {

        NativeArray<byte> m_NativeArray;
        GCHandle m_BufferHandle;
        AtomicSafetyHandle m_SafetyHandle;
        bool m_Pinned;

        public unsafe ManagedNativeArray(byte[] original) {
            if (original != null) {
                m_BufferHandle = GCHandle.Alloc(original,GCHandleType.Pinned);
                fixed (void* bufferAddress = &original[0]) {
                    m_NativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(bufferAddress, original.Length, Allocator.None);
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_SafetyHandle = AtomicSafetyHandle.Create();
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(array: ref m_NativeArray, m_SafetyHandle);
    #endif
                }

                m_Pinned = true;
            }
            else {
                m_NativeArray = new NativeArray<byte>();
            }
        }

        public NativeArray<byte> nativeArray => m_NativeArray;
        
        public void Dispose() {
            if (m_Pinned) {
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_SafetyHandle);
    #endif
                m_BufferHandle.Free();
            }
        }
    }
}
