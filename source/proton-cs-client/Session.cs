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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tuvi.Auth.Proton;
using Tuvi.Auth.Services;
using Tuvi.Proton.Client.Exceptions;
using Tuvi.Proton.Primitive.Messages;
using Tuvi.Proton.Primitive.Messages.Payloads;
using Tuvi.RestClient;

namespace Tuvi.Proton.Client
{
    public class Session : Tuvi.RestClient.Client
    {
        private static string TwoFactorScope => "twofactor";

        public Uri RedirectUri { get => _broker.RedirectUri; set => _broker.RedirectUri = value; }
        public string ClientSecret { get => _broker.ClientSecret; set => _broker.ClientSecret = value; }
        public string UserAgent { get => _broker.UserAgent; set => _broker.UserAgent = value; }
        public string AppVersion { get => _broker.AppVersion; set => _broker.AppVersion = value; }

        public int PasswordMode { get; private set; }
        public string Scope { get; private set; }
        public bool IsTwoFactor => Scopes?.FirstOrDefault((scope) => scope.Equals(TwoFactorScope, StringComparison.OrdinalIgnoreCase)) != null;
        public IEnumerable<string> Scopes => Scope?.Split(' ');

        private SessionData? _sessionData;
        private string _refreshToken;
        private readonly Broker _broker;

        public Session(HttpClient httpClient, ISRPClientFactory srpClientFactory, Uri host)
            : base(httpClient, host)
        {
            _broker = new Broker(httpClient, srpClientFactory, host);
        }

        public async Task LoginAsync(string username, string password, CancellationToken cancellationToken)
        {
            var data = await _broker.AuthenticateAsync(username, password, cancellationToken).ConfigureAwait(false);

            EnsureCorrectResponse(data);

            _sessionData = new SessionData()
            {
                Uid = data.Uid,
                TokenType = data.TokenType,
                AccessToken = data.AccessToken,
            };
            Scope = data.Scope;
            PasswordMode = data.PasswordMode;
            _refreshToken = data.RefreshToken;

        }

        public async Task ProvideTwoFactorCodeAsync(string code, CancellationToken cancellationToken)
        {
            var data = await _broker.ProvideTwoFactorCodeAsync(GetSessionData(), code, cancellationToken).ConfigureAwait(false);

            EnsureCorrectResponse(data);

            Scope = data.Scope;
        }

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            var sessionData = _sessionData;

            _refreshToken = null;
            _sessionData = null;
            Scope = null;
            PasswordMode = 0;

            await _broker.LogoutAsync(GetSessionData(sessionData), cancellationToken).ConfigureAwait(false);
        }

        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            var data = await _broker.RefreshAsync(GetSessionData(), _refreshToken, cancellationToken).ConfigureAwait(false);

            EnsureCorrectResponse(data);

            _refreshToken = data.RefreshToken;
            Scope = data.Scope;
            _sessionData = new SessionData
            {
                AccessToken = data.AccessToken,
                TokenType = data.TokenType,
                Uid = data.Uid,
            };
        }

        public virtual void Load(string dump)
        {
            try
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
            catch (Exception ex) when (ex is JsonException || ex is NotSupportedException)
            {
                throw new ProtonSessionException("Session dump could not be parsed.", innerException: ex);
            }
        }

        public virtual string Dump()
        {
            try
            {
                return JsonSerializer.Serialize(
                    value: SessionDump.Create(GetSessionData(), _refreshToken, PasswordMode, Scope),
                    options: new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });
            }
            catch (NotSupportedException ex)
            {
                throw new ProtonSessionException("Session can't be saved.", innerException: ex);
            }
        }

        public async Task<HttpStatusCode> RequestAsync<TResponse, TRequest>(ProtonMessage<TResponse, TRequest> message, CancellationToken cancellationToken)
            where TResponse : Response
            where TRequest : Request
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var sessionData = GetSessionData();
            message.UserAgent = UserAgent;
            message.AppVersion = AppVersion;
            message.AccessToken = sessionData.AccessToken;
            message.TokenType = sessionData.TokenType;
            message.Uid = sessionData.Uid;

            try
            {
                return await SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new ProtonSessionRequestException(
                    message: "Bad Proton request.",
                    innerException: exception,
                    code: message.HttpStatus,
                    response: message.Response);
            }
        }

        private SessionData GetSessionData()
        {
            return GetSessionData(_sessionData);
        }

        private static SessionData GetSessionData(SessionData? sessionData)
        {
            return sessionData ?? throw new ProtonSessionException("Connection not established, please try to login.");
        }

        private static void EnsureCorrectResponse(CommonResponse response)
        {
            if (response is null)
            {
                throw new ProtonSessionException("Response not found.");
            }

            if (!response.Success)
            {
                throw new ProtonSessionRequestException("Proton response failed.", response);
            }
        }
    }
}
