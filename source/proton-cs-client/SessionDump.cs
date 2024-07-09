////////////////////////////////////////////////////////////////////////////////
//
//   Copyright 2024 Eppie(https://eppie.io)
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
using System.Collections.Generic;
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
        public long ExpirationTime { get; private set; }

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
                Version = 2,
                Uid = sessionData.Uid,
                AccessToken = sessionData.AccessToken,
                TokenType = sessionData.TokenType,
                ExpirationTime = sessionData.ExpirationTime,
                RefreshToken = refreshToken,
                PasswordMode = passwordMode,
                Scope = scope
            };
        }

        public override bool Equals(object obj)
        {
            return obj is SessionData sessionData && Equals(sessionData);
        }

        public override int GetHashCode()
        {
            int hashCode = -1425866907;
            hashCode = hashCode * -1521134295 + Version.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Uid);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AccessToken);
            hashCode = hashCode * -1521134295 + ExpirationTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TokenType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RefreshToken);
            hashCode = hashCode * -1521134295 + PasswordMode.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Scope);
            return hashCode;
        }

        public bool Equals(SessionDump other)
        {
            return Version == other.Version &&
                   Uid == other.Uid &&
                   AccessToken == other.AccessToken &&
                   TokenType == other.TokenType &&
                   ExpirationTime == other.ExpirationTime &&
                   RefreshToken == other.RefreshToken &&
                   PasswordMode == other.PasswordMode &&
                   Scope == other.Scope;
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
