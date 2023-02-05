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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tuvi.Auth.Exceptions;
using Tuvi.Auth.Proton;
using Tuvi.Auth.Proton.Message.Payloads;
using Tuvi.Auth.Services;

namespace Tuvi.Proton.Client
{
    public class Session : Broker
    {
        private static string TwoFactorScope => "twofactor";
        public int PasswordMode { get; private set; }
        public string Scope { get; private set; }
        public bool IsTwoFactor => Scopes?.FirstOrDefault((scope) => scope.Equals(TwoFactorScope, StringComparison.OrdinalIgnoreCase) ) != null;

        public IEnumerable<string> Scopes => Scope?.Split(' ');

        private SessionData? _sessionData;
        private string _refreshToken;

        public Session(HttpClient httpClient, ISRPClient srpClient, Uri host)
            : base(httpClient, srpClient, host)
        { }

        public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                var data = await AuthenticateAsync(username, password, cancellationToken).ConfigureAwait(false);
                if (data?.Success is true)
                {
                    _sessionData = new SessionData()
                    {
                        Uid = data.Uid,
                        TokenType = data.TokenType,
                        AccessToken = data.AccessToken,
                    };
                    Scope = data.Scope;
                    _refreshToken = data.RefreshToken;

                    return true;
                }
            }
            catch (ProtonRequestException exception)
            {
                if (TryHumanVerification(exception) is false)
                {
                    throw;
                }
            }

            return false;
        }

        public async Task ProvideTwoFactorCodeAsync(string code, CancellationToken cancellationToken)
        {
            var data = await ProvideTwoFactorCodeAsync(GetSessionData(), code, cancellationToken).ConfigureAwait(false);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            var sessionData = _sessionData;

            _refreshToken = null;
            _sessionData = null;
            Scope = null;
            PasswordMode = 0;

            var data = await LogoutAsync(GetSessionData(sessionData), cancellationToken).ConfigureAwait(false);

        }

        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            var data = await RefreshAsync(GetSessionData(), _refreshToken, cancellationToken).ConfigureAwait(false);
            if (data?.Success is true)
            {
                _refreshToken = data.RefreshToken;
                UpdateSessionData(data.AccessToken);
            }
        }

        public virtual void Load(string dump)
        {
            var sessionDump = JsonSerializer.Deserialize<SessionDump>(
                json: dump,
                options: new JsonSerializerOptions());

            if (sessionDump.Version == 1)
            {
                _refreshToken = sessionDump.RefreshToken;
                _sessionData = new SessionData()
                {
                    AccessToken = sessionDump.AccessToken,
                    TokenType = sessionDump.TokenType,
                    Uid = sessionDump.Uid,
                };
                PasswordMode = sessionDump.PasswordMode;
                Scope = sessionDump.Scope;
            }
        }

        public virtual string Dump()
        {
            return JsonSerializer.Serialize(
                value: SessionDump.Create(GetSessionData(), _refreshToken, PasswordMode, Scope),
                options: new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
        }

        public Task RequestAsync(string endpoint, string jsonData, string additionalHeaders,
            string method, string requestParams)
        {
            throw new NotImplementedException();
        }

        private static bool TryHumanVerification(ProtonRequestException exception)
        {
            if (CommonResponse.ResponseCode.HumanVerificationRequired.SameAs(exception.ErrorInfo.Code))
            {

                return true;
            }

            return false;
        }

        private SessionData GetSessionData()
        {
            return GetSessionData(_sessionData);
        }

        private static SessionData GetSessionData(SessionData? sessionData)
        {
            return sessionData ?? throw new InvalidOperationException();
        }

        private void UpdateSessionData(string accessToken)
        {
            var sessionData = GetSessionData();
            _sessionData = new SessionData
            {
                AccessToken = accessToken,
                TokenType = sessionData.TokenType,
                Uid = sessionData.Uid,
            };
        }
    }
}
