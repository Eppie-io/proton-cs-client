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

using System.Text.Json.Serialization;
using Tuvi.Proton.Primitive.Messages.Payloads;

namespace Tuvi.Proton.Client.Sample.Messages
{
    internal class FilterContent : CommonResponse
    {
        [JsonInclude]
        public long Total { get; private set; }

        [JsonInclude]
        public IList<Message>? Messages { get; private set; }

        public record Message
        {
            [JsonInclude]
            public string? Subject { get; private set; }

            [JsonInclude]
            public int? Unread { get; private set; }

            [JsonInclude]
            public string? SenderAddress { get; private set; }

            [JsonInclude]
            public string? SenderName { get; private set; }

            public override string ToString()
            {
                return $"Subject: '{Subject}'; Sender: {SenderName} <{SenderAddress}>; This message is {(Unread is 0 ? "read" : "unread")}";
            }
        }

        public override string ToString()
        {
            string result = $"Found {Total} messages" + Environment.NewLine;

            foreach (var message in Messages ?? Enumerable.Empty<Message>())
            {
                result += message.ToString() + Environment.NewLine;
            }

            return result;
        }
    }
}
