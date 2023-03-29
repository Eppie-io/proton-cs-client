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

using Tuvi.Proton.Client;
using Tuvi.Proton.Client.Sample.Messages;
using Tuvi.RestClient;

try
{
    using var httpClient = new HttpClient();

    var proton = new Session(httpClient, new Uri("https://mail-api.proton.me"))
    {
        AppVersion = "Other",
        UserAgent = "Sample",
        ClientSecret = string.Empty,
        RedirectUri = new Uri("https://protonmail.ch")
    };

    Console.Write("User: ");
    var user = Console.ReadLine();

    Console.Write("Password: ");
    var password = Console.ReadLine();

    await proton.LoginAsync(user, password, CancellationToken.None).ConfigureAwait(false);

    if (proton.IsTwoFactor)
    {
        if (!proton.IsTOTP)
        {
            throw new InvalidOperationException("Two-factor authentication type is not supported");
        }

        Console.Write("TOTP code: ");
        var code = Console.ReadLine();
        await proton.ProvideTwoFactorCodeAsync(code, CancellationToken.None).ConfigureAwait(false);
    }

    PrintMessage($"""
        Proton scope: {proton.Scope}

        """);

    var countResponse = await proton.RequestAsync<TotalMessagesContent>(
        endpoint: new Uri("/mail/v4/messages/count", UriKind.Relative),
        method: HttpMethod.Get,
        headers: null,
        cancellationToken: CancellationToken.None).ConfigureAwait(false);

    PrintMessage($"""
        Response to '/mail/v4/messages/count' request:
        {countResponse}
        """);

    var filterResponse = await proton.RequestAsync<FilterContent, FilterPayload>(
        endpoint: new Uri("/mail/v4/messages", UriKind.Relative),
        method: HttpMethod.Post,
        payload: new FilterPayload { Subject = "Welcome" },
        headers: new HeaderCollection(new[] { ("X-HTTP-Method-Override", "GET") }),
        cancellationToken: CancellationToken.None).ConfigureAwait(false);

    PrintMessage($"""
        Response to '/mail/v4/messages?Subject=Welcome' request:
        {filterResponse}
        """);

    await proton.LogoutAsync(CancellationToken.None).ConfigureAwait(false);
}
catch (Tuvi.Proton.Primitive.Exceptions.ProtonRequestException ex)
{
    PrintError($"""
        ProtonRequestException:

        Proton Code: {ex.ErrorInfo.Code};
        Proton Error: {ex.ErrorInfo.Error};

        HttpStatusCode: {ex.HttpStatusCode};
        InnerException: {ex.InnerException};
        """);
}
catch (Exception ex)
{
    PrintError(ex.ToString());
}

static void PrintError(string error)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(error);
    Console.ForegroundColor = color;
}

static void PrintMessage(string message)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(message);
    Console.ForegroundColor = color;
}
