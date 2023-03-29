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
    internal class TotalMessagesContent : CommonResponse
    {
        [JsonInclude]
        public IList<Folder>? Counts { get; private set; }

        public record Folder
        {
            [JsonInclude]
            public string? LabelID { get; private set; }

            [JsonInclude]
            public long Total { get; private set; }

            [JsonInclude]
            public long Unread { get; private set; }

            public override string ToString()
            {
                return $"LabelID is '{LabelID}'; {Total} messages; {Unread} unread messages.";
            }
        }

        public override string ToString()
        {
            string result = string.Empty;

            foreach (var folder in Counts ?? Enumerable.Empty<Folder>())
            {
                result += folder.ToString() + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(result))
            {
                result = "Messages not found.";
            }

            return result;
        }
    }
}
