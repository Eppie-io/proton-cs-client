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

using System.Diagnostics.CodeAnalysis;
using Tuvi.Auth.Proton.Exceptions;
using Tuvi.Proton.Client.Exceptions;
using Tuvi.Proton.Client.Test.Data;

namespace Tuvi.Proton.Client.Test
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated via NUnit Framework")]
    internal class SessionTests
    {
        private Session ProtonSession { get; set; }


        [SetUp]
        public void Setup()
        {
            ProtonSession = new Session(new HttpClient(), SessionTestData.FakeUri)
            {
                AppVersion = SessionTestData.FakeAppVersion,
                UserAgent = SessionTestData.FakeUserAgent,
            };
        }

        [Test]
        public void LoginAsync_WrongArgument_Throws()
        {
            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: null,
                        password: null,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: "any",
                        password: null,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: null,
                        password: "any",
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: string.Empty,
                        password: string.Empty,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: "any",
                        password: string.Empty,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: string.Empty,
                        password: "any",
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<AuthProtonArgumentException>(
                async () =>
                {
                    await ProtonSession.LoginAsync(
                        username: " ",
                        password: "any",
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });
        }

        [Test]
        public void ProtonSession_UninitializedSession_Throws()
        {
            Assert.ThrowsAsync<ProtonSessionException>(
                async () =>
                {
                    await ProtonSession.RefreshAsync(
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<ProtonSessionException>(
                async () =>
                {
                    await ProtonSession.ProvideTwoFactorCodeAsync(
                        code: null,
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });

            Assert.ThrowsAsync<ProtonSessionException>(
                async () =>
                {
                    await ProtonSession.LogoutAsync(
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);
                });
        }

        [Test]
        public void LoadDump_IncorrectData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ProtonSession.Load(null));
            Assert.Throws<ProtonSessionException>(() => ProtonSession.Load(string.Empty));
            Assert.Throws<ProtonSessionException>(() => ProtonSession.Load("not json"));
        }

        [Test]
        public void SaveDump_UninitializedSession_Throws()
        {
            Assert.Throws<ProtonSessionException>(() => ProtonSession.Dump());
        }
    }
}