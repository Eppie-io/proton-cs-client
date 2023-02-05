////////////////////////////////////////////////////////////////////////////////
//
//   Copyright 2023 Eppie(https://eppie.io)
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Tuvi.Proton.Primitive.Exceptions;
using Tuvi.Proton.Primitive.Messages.Payloads;
using Tuvi.RestClient;

namespace Tuvi.Proton.Client.Exceptions
{
    public class ProtonSessionRequestException : ProtonRequestException
    {
        internal ProtonSessionRequestException(string message) : base(message)
        { }

        internal ProtonSessionRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        internal ProtonSessionRequestException()
        { }

        internal ProtonSessionRequestException(string message, HttpRequestException innerException, HttpStatusCode code, Response response)
            : base(message, innerException)
        {
            HttpStatusCode = code;

            if (response is StringResponse stringResponse && TryCreateCommonResponse(stringResponse.Content, out var commonResponse))
            {
                ErrorInfo = new RequestErrorInfo(commonResponse);
            }
        }

        internal ProtonSessionRequestException(string message, CommonResponse response)
            : base(message)
        {
            if (response != null)
            {
                ErrorInfo = new RequestErrorInfo(response);
            }
        }

        private static bool TryCreateCommonResponse(string response, out CommonResponse commonResponse)
        {
            commonResponse = null;
            try
            {
                commonResponse = JsonSerializer.Deserialize<CommonResponse>(response);
                return true;
            }
            catch (ArgumentNullException)
            { }
            catch (JsonException)
            { }
            catch (NotSupportedException)
            { }

            return false;
        }
    }
}
