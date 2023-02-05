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
using System.Text.Json.Serialization;
using Tuvi.Auth.Proton;

namespace Tuvi.Proton.Client
{
    interface ISessionDump
    {
        int Version { get; }
    }

    public struct SessionDump : IEquatable<SessionDump>, ISessionDump
    {
        [JsonInclude]
        public int Version { get; private set; }

        [JsonInclude]
        public string Uid { get; private set; }

        [JsonInclude]
        public string AccessToken { get; private set; }

        [JsonInclude]
        public string TokenType { get; private set; }

        [JsonInclude]
        public string RefreshToken { get; private set; }

        [JsonInclude]
        public int PasswordMode { get; private set; }

        [JsonInclude]
        public string Scope { get; private set; }

        public static SessionDump Create(SessionData sessionData, string refreshToken, int passwordMode, string scope)
        {
            return new SessionDump()
            {
                Version = 1,
                Uid = sessionData.Uid,
                AccessToken = sessionData.AccessToken,
                TokenType = sessionData.TokenType,
                RefreshToken = refreshToken,
                PasswordMode = passwordMode,
                Scope = scope
            };
        }

        public bool Equals(SessionDump other)
        {
            return Version == other.Version &&
                   string.Equals(Uid, other.Uid, StringComparison.Ordinal) &&
                   string.Equals(AccessToken, other.AccessToken, StringComparison.Ordinal) &&
                   string.Equals(TokenType, other.TokenType, StringComparison.Ordinal) &&
                   string.Equals(RefreshToken, other.RefreshToken, StringComparison.Ordinal) &&
                   PasswordMode == other.PasswordMode &&
                   string.Equals(Scope, other.Scope, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is SessionData sessionData)
                return Equals(sessionData);

            return false;
        }

        public override int GetHashCode()
        {
            return $"{Version}#{Uid}#{AccessToken}#{TokenType}#{RefreshToken}#{PasswordMode}#{Scope}".GetHashCode();
        }

        public static bool operator ==(SessionDump left, SessionDump right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SessionDump left, SessionDump right)
        {
            return !left.Equals(right);
        }
    }
}
