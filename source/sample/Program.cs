////////////////////////////////////////////////////////////////////////////////
//
//   Copyright 2025 Eppie(https://eppie.io)
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

using Tuvi.Auth.Proton.Exceptions;
using Tuvi.Proton.Client;
using Tuvi.Proton.Client.Sample.Messages;
using Tuvi.Proton.Primitive.Exceptions;
using Tuvi.Proton.Primitive.Messages.Errors;
using Tuvi.Proton.Primitive.Messages.Payloads;
using Tuvi.RestClient;

try
{
    using var httpClient = new HttpClient();
    var host = new Uri("https://mail-api.proton.me");

    var proton = new Session(httpClient, host)
    {
        AppVersion = "Other",
        RedirectUri = new Uri("https://protonmail.ch")
    };

    Console.Write("User: ");
    var user = Console.ReadLine();

    Console.Write("Password: ");
    var password = Console.ReadLine();

    try
    {
        await proton.LoginAsync(user, password).ConfigureAwait(false);
    }
    catch (AuthUnsuccessProtonException ex)
    {
        var (method, token) = TryHumanVerification(ex.Response, host);
        if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(token))
        {
            throw;
        }

        proton.SetHumanVerification(method, token);
        await proton.LoginAsync(user, password).ConfigureAwait(false);
        proton.ResetHumanVerification();
    }

    if (proton.IsTwoFactor)
    {
        if (!proton.IsTOTP)
        {
            throw new InvalidOperationException("Two-factor authentication type is not supported");
        }

        Console.Write("TOTP code: ");
        var code = Console.ReadLine();
        await proton.ProvideTwoFactorCodeAsync(code).ConfigureAwait(false);
    }

    PrintMessage($"""
        Proton scope: {proton.Scope}

        """);

    var countResponse = await proton.RequestAsync<TotalMessagesContent>(
        endpoint: new Uri("/mail/v4/messages/count", UriKind.Relative),
        method: HttpMethod.Get,
        headers: null).ConfigureAwait(false);

    PrintMessage($"""
        Response to '/mail/v4/messages/count' request:
        {countResponse}
        """);

    var filterResponse = await proton.RequestAsync<FilterContent, FilterPayload>(
        endpoint: new Uri("/mail/v4/messages", UriKind.Relative),
        method: HttpMethod.Post,
        payload: new FilterPayload { Subject = "Welcome" },
        headers: new HeaderCollection(new[] { ("X-HTTP-Method-Override", "GET") })).ConfigureAwait(false);

    PrintMessage($"""
        Response to '/mail/v4/messages?Subject=Welcome' request:
        {filterResponse}
        """);

    await proton.LogoutAsync().ConfigureAwait(false);
}
catch (ProtonRequestException ex)
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

static (string? method, string? token) ShowHumanVerificationMessage(HumanVerificationDetails details, Uri host)
{
    Console.WriteLine($"""
        Proton '{details.Title}'
        Description: {(string.IsNullOrEmpty(details.Description) ? "None" : details.Description)}
        Methods: {string.Join(", ", details.HumanVerificationMethods)}
        """);

    var method = details.HumanVerificationMethods.Count == 1 ? details.HumanVerificationMethods.First()
                                                             : string.Empty;

    while (!details.HumanVerificationMethods.Contains(method))
    {
        Console.Write($"Select verification method: ");
        method = Console.ReadLine();
    }

    if (string.Equals("captcha", method, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"Captcha url: {new Uri(host, details.HumanVerificationApiUri)}");
    }

    Console.Write($"Enter verification token: ");
    var token = Console.ReadLine();

    return (method, token);
}

static (string? method, string? token) TryHumanVerification(CommonResponse response, Uri host)
{
    if (response.IsHumanVerificationRequired() != true)
    {
        return (null, null);
    }

    var details = response.ReadDetails<HumanVerificationDetails>();
    if (details is null)
    {
        return (null, null);
    }

    return ShowHumanVerificationMessage(details, host);
}