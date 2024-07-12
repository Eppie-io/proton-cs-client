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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tuvi.Auth.Proton;
using Tuvi.Auth.Proton.Messages.Payloads;
using Tuvi.Auth.Services;
using Tuvi.Proton.Client.Exceptions;
using Tuvi.Proton.Client.Messages;
using Tuvi.Proton.Primitive.Messages;
using Tuvi.Proton.Primitive.Messages.Payloads;
using Tuvi.RestClient;

namespace Tuvi.Proton.Client
{
    /// <summary>
    /// The Session class provides connection to the Proton API, execution of requests, loading and storing sessions.
    /// </summary>
    public class Session : Tuvi.RestClient.Client
    {
        /// <summary>
        /// This is the version of the application that is implementing the client.
        /// </summary>
        public string AppVersion { get => _broker.AppVersion; set => _broker.AppVersion = value; }

        /// <summary>
        /// Secret token for the new Session object that is added to the payload with key `ClientSecret`. [OPTIONAL]
        /// </summary>
        public string ClientSecret { get => _broker.ClientSecret; set => _broker.ClientSecret = value; }

        /// <summary>
        /// This is the refresh redirect url.
        /// </summary>
        public Uri RedirectUri { get => _broker.RedirectUri; set => _broker.RedirectUri = value; }

        /// <summary>
        /// This helps us to understand on what type of platforms the client is being used. [OPTIONAL]
        /// </summary>
        public string UserAgent { get => _broker.UserAgent; set => _broker.UserAgent = value; }

        /// <summary>
        /// Gets a value that indicates the password mode. It is filled out after login.
        /// </summary>
        /// <seealso cref="Auth.Proton.Messages.Payloads.PasswordMode"/>
        public int PasswordMode
        {
            get { lock (_sharedStateLock) return _passwordMode; }
        }
        private int _passwordMode;

        /// <summary>
        /// Gets a value that indicates the refresh token. It is filled out after login.
        /// </summary>
        public string RefreshToken
        {
            get { lock (_sharedStateLock) return _refreshToken; }
        }

        /// <summary>
        /// Gets a value that indicates the user id. It is filled out after login.
        /// </summary>
        public string UserId
        {
            get { lock (_sharedStateLock) return _sessionData?.Uid; }
        }

        /// <summary>
        /// The API permission scopes. It is filled out after login.
        /// </summary>
        public string Scope
        {
            get { lock (_sharedStateLock) return _scope; }
        }
        private string _scope;

        /// <summary>
        /// This is a collection of permission scope. It is filled out after login.
        /// </summary>
        public IEnumerable<string> Scopes => Scope?.Split(' ');

        /// <summary>
        /// Gets a value that specifies whether two-factor authentication is enabled. It is filled out after login.
        /// </summary>
        public bool IsTwoFactor
        {
            get { lock (_sharedStateLock) return _isTwoFactor; }
        }
        private bool _isTwoFactor;

        /// <summary>
        /// Gets a value that specifies whether two-factor authentication is TOTP. It is filled out after login.
        /// </summary>
        public bool IsTOTP
        {
            get { lock (_sharedStateLock) return _isTOTP; }
        }
        private bool _isTOTP;

        private readonly object _sharedStateLock = new object();
        private SessionData? _sessionData;
        private string _refreshToken;
        private readonly Broker _broker;

        /// <summary>
        /// Constructs a new instance of the Session class.
        /// </summary>
        /// <param name="httpClient">The HttpClient for making http requests.</param>
        /// <param name="srpClientFactory">A factory abstraction that can create <see cref="ISRPClient"> instances.</param>
        /// <param name="host">The base API url. The httpClient.BaseAddress will be used
        /// as the base API url if the host parameter is null.</param>
        public Session(HttpClient httpClient, ISRPClientFactory srpClientFactory, Uri host)
            : base(httpClient, host)
        {
            _broker = new Broker(httpClient, srpClientFactory, host);
        }

        /// <summary>
        /// Constructs a new instance of the Session class with a default ISRPClientFactory
        /// </summary>
        /// <param name="httpClient">The HttpClient for making http requests.</param>
        /// <param name="host">The base API url. The httpClient.BaseAddress will be used
        /// as the base API url if the host parameter is null.</param>
        public Session(HttpClient httpClient, Uri host)
            : base(httpClient, host)
        {
            _broker = new Broker(httpClient, host);
        }

        /// <summary>
        /// Starts the asynchronous authentication operation.
        /// </summary>
        /// <param name="username">Proton account username.</param>
        /// <param name="password">Proton account password.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ProtonSessionException"></exception>
        /// <exception cref="ProtonSessionRequestException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonArgumentException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonRequestException"></exception>
        public async Task LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var authenticator = await _broker.BuildAuthenticatorAsync(username, password, cancellationToken).ConfigureAwait(false);
            var data = await authenticator(cancellationToken).ConfigureAwait(false);

            EnsureCorrectResponse(data);

            long expirationTime = GetExpirationTime(data.ExpiresIn);
            TwoFAStatus twoFAStatus = (TwoFAStatus)data.TwoFactorSettings.Enabled;

            lock (_sharedStateLock)
            {
                _sessionData = new SessionData()
                {
                    Uid = data.UID,
                    TokenType = data.TokenType,
                    AccessToken = data.AccessToken,
                    ExpirationTime = expirationTime,
                };
                _refreshToken = data.RefreshToken;
                _scope = data.Scope;
                _passwordMode = data.PasswordMode;
                _isTwoFactor = twoFAStatus != TwoFAStatus.None;
                _isTOTP = twoFAStatus.HasFlag(TwoFAStatus.TOTP);
            }
        }

        /// <summary>
        /// Provides Two Factor Authentication Code to the API asynchronously.
        /// </summary>
        /// <param name="code">This is the TOTP code represented by a string of numbers.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ProtonSessionException"></exception>
        /// <exception cref="ProtonSessionRequestException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonArgumentException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonRequestException"></exception>
        public async Task ProvideTwoFactorCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var data = await _broker.ProvideTwoFactorCodeAsync(GetSessionData(), code, cancellationToken).ConfigureAwait(false);

            EnsureCorrectResponse(data);

            lock (_sharedStateLock)
            {
                _scope = data.Scope;
            }
        }

        /// <summary>
        /// Closes session asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonArgumentException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonRequestException"></exception>
        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            SessionData? sessionData = null;
            lock (_sharedStateLock)
            {
                sessionData = _sessionData;

                _sessionData = null;
                _refreshToken = null;
                _scope = null;
                _passwordMode = 0;
                _isTwoFactor = false;
                _isTOTP = false;
            }
            await _broker.LogoutAsync(GetSessionData(sessionData), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes AccessToken asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonArgumentException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonRequestException"></exception>
        public Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            var (sessionData, refreshToken) = GetRefreshData();
            return RefreshAsync(sessionData, refreshToken, cancellationToken);

            (SessionData, string) GetRefreshData()
            {
                lock (_sharedStateLock)
                {
                    return (GetSessionData(_sessionData), _refreshToken);
                }
            }
        }

        /// <summary>
        /// Restores previous session asynchronously.
        /// </summary>
        /// <param name="uid">User identifier</param>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonArgumentException"></exception>
        /// <exception cref="Auth.Proton.Exceptions.AuthProtonRequestException"></exception>
        public Task RestoreAsync(string uid, string refreshToken, CancellationToken cancellationToken = default)
        {
            var sessionData = new SessionData
            {
                Uid = uid
            };
            return RefreshAsync(sessionData, refreshToken, cancellationToken);
        }

        /// <summary>
        /// Checks if the expiration time has been exceeded.
        /// </summary>
        /// <param name="reserve">A number of seconds.</param>
        /// <returns>
        /// <c>true</c> if the token is expired or it's going to be expired in the next <c>reserve</c> seconds; otherwise, <c>false</c>.
        /// </returns>
        public bool IsExpired(int reserve = 0)
        {
            SessionData data = GetSessionData();

            DateTime expirationTime = new DateTime(data.ExpirationTime);
            TimeSpan reserveTime = new TimeSpan(reserve * TimeSpan.TicksPerSecond);

            return expirationTime < DateTime.UtcNow + reserveTime;
        }

        /// <summary>
        /// Loads Proton session.
        /// </summary>
        /// <param name="dump">The output generated by <see cref="Dump()"/></param>
        /// <exception cref="ProtonSessionException"></exception>
        public virtual void Load(string dump)
        {
            try
            {
                var sessionDump = JsonSerializer.Deserialize<SessionDump>(
                    json: dump,
                    options: new JsonSerializerOptions());

                if (sessionDump.Version >= 1)
                {
                    lock (_sharedStateLock)
                    {
                        _sessionData = new SessionData()
                        {
                            Uid = sessionDump.Uid,
                            TokenType = sessionDump.TokenType,
                            AccessToken = sessionDump.AccessToken,
                            ExpirationTime = (sessionDump.Version == 2) ? sessionDump.ExpirationTime : 0,
                        };
                        _refreshToken = sessionDump.RefreshToken;
                        _scope = sessionDump.Scope;
                        _passwordMode = sessionDump.PasswordMode;
                        _isTwoFactor = false;
                        _isTOTP = false;
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is NotSupportedException)
            {
                throw new ProtonSessionException("Session dump could not be parsed.", innerException: ex);
            }
        }

        /// <summary>
        /// Provides session dump. If you want to reuse the session, then dump it and store the values.
        /// </summary>
        /// <returns>The result will contain some data that will be needed for <see cref="RefreshAsync(CancellationToken)"/></returns>
        /// <exception cref="ProtonSessionException"></exception>
        public virtual string Dump()
        {
            try
            {
                lock (_sharedStateLock)
                {
                    return JsonSerializer.Serialize(
                        value: SessionDump.Create(GetSessionData(_sessionData), _refreshToken, _passwordMode, _scope),
                        options: new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        });
                }
            }
            catch (NotSupportedException ex)
            {
                throw new ProtonSessionException("Session can't be saved.", innerException: ex);
            }
        }

        /// <summary>
        /// Makes a Proton API request asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">The type of response message. <seealso cref="Response"> </typeparam>
        /// <typeparam name="TRequest">The type of request message. <seealso cref="Request"> </typeparam>
        /// <param name="message">The Proton API message.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ProtonSessionRequestException"></exception>
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

        /// <summary>
        /// Makes a Proton API request asynchronously.
        /// </summary>
        /// <typeparam name="TContent">The content type of the response.</typeparam>
        /// <typeparam name="TPayload">The payload type of the request.</typeparam>
        /// <param name="endpoint">API endpoint.</param>
        /// <param name="method">HTTP protocol method.</param>
        /// <param name="payload">A request payload that will be serialized to JSON.</param>
        /// <param name="headers">Additional http headers.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>Returns the value that results from deserializing the content as JSON.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ProtonSessionRequestException"></exception>
        public async Task<TContent> RequestAsync<TContent, TPayload>(Uri endpoint, HttpMethod method, TPayload payload, HeaderCollection headers, CancellationToken cancellationToken = default)
        {
            var message = new CustomMessage<TContent, TPayload>(endpoint, method, payload)
            {
                CustomHeaders = headers
            };

            await RequestAsync(message, cancellationToken).ConfigureAwait(false);

            if (message.Response is null)
            {
                return default;
            }

            return message.Response.Content;
        }

        /// <summary>
        /// Makes a Proton API request asynchronously.
        /// </summary>
        /// <typeparam name="TContent">The content type of the response.</typeparam>
        /// <param name="endpoint">API endpoint.</param>
        /// <param name="method">HTTP protocol method.</param>
        /// <param name="headers">Additional http headers.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>Returns the value that results from deserializing the content as JSON.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ProtonSessionRequestException"></exception>
        public async Task<TContent> RequestAsync<TContent>(Uri endpoint, HttpMethod method, HeaderCollection headers, CancellationToken cancellationToken = default)
        {
            var message = new CustomMessage<TContent>(endpoint, method)
            {
                CustomHeaders = headers
            };

            await RequestAsync(message, cancellationToken).ConfigureAwait(false);

            if (message.Response is null)
            {
                return default;
            }

            return message.Response.Content;
        }

        public void SetHumanVerification(string type, string token)
        {
            _broker.HumanVerificationTokenType = type;
            _broker.HumanVerificationToken = token;
        }

        public void ResetHumanVerification()
        {
            _broker.HumanVerificationTokenType = string.Empty;
            _broker.HumanVerificationToken = string.Empty;
        }

        public SessionData GetSessionData()
        {
            lock (_sharedStateLock)
            {
                return GetSessionData(_sessionData);
            }
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

        private async Task RefreshAsync(SessionData sessionData, string refreshToken, CancellationToken cancellationToken)
        {
            var data = await _broker.RefreshAsync(sessionData, refreshToken, cancellationToken).ConfigureAwait(false);

            EnsureCorrectResponse(data);

            long expirationTime = GetExpirationTime(data.ExpiresIn);

            lock (_sharedStateLock)
            {
                _sessionData = new SessionData
                {
                    Uid = data.Uid,
                    TokenType = data.TokenType,
                    AccessToken = data.AccessToken,
                    ExpirationTime = expirationTime
                };
                _refreshToken = data.RefreshToken;
                _scope = data.Scope;
                _isTwoFactor = false;
                _isTOTP = false;
            }
        }

        private static long GetExpirationTime(int expiresIn)
        {
            return (DateTime.UtcNow + TimeSpan.FromSeconds(expiresIn)).Ticks;
        }
    }
}
