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
using System.Collections.Generic;

namespace KtxUnity {

    public enum ErrorCode {
        Success,
        UnsupportedVersion,
        UnsupportedFormat,
        NotSuperCompressed,
        OpenUriFailed,
        LoadingFailed,
        TranscodeFailed,
    }

    public static class ErrorMessage {
        const string k_UnknownErrorMessage = "Unknown Error"; 
        static readonly Dictionary<ErrorCode, string> k_ErrorMessages = new Dictionary<ErrorCode, string>() {
            { ErrorCode.Success, "OK" },
            { ErrorCode.UnsupportedVersion, "Only KTX 2.0 is supported" },
            { ErrorCode.UnsupportedFormat, "Unsupported format" },
            { ErrorCode.NotSuperCompressed, "Only super-compressed KTX is supported" },
            { ErrorCode.OpenUriFailed, "Loading URI failed!" },
            { ErrorCode.LoadingFailed, "Loading failed!" },
            { ErrorCode.TranscodeFailed, "Transcoding failed!" },
        };

        public static string GetErrorMessage(ErrorCode code) {
            if (k_ErrorMessages.TryGetValue(code, out var message)) {
                return message;
            }
#if DEBUG
            return $"No Error message for error {code.ToString()}";
#else
            return k_UnknownErrorMessage;
#endif
        }
    }
}
