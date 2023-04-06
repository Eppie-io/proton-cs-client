# Proton API C# Client

**Licence** [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0)

## Description

This library is a C# (netstandard 2.0) implementation of Proton API Client. It is meant for use by C# applications that want to authenticate at [Proton](https://proton.me/) server.

The official Python client can be found [here](https://github.com/ProtonMail/proton-python-client).

### Dependencies

- [TuviAuthProtonLib](https://github.com/Eppie-io/TuviAuthProtonLib)
- [TuviRestClientLib](https://github.com/Eppie-io/TuviRestClientLib)
- [TuviSRPLib](https://github.com/Eppie-io/TuviSRPLib)

### Installation

For installation clone this repository recursively with submodules:

```shell
git clone --recurse-submodules https://github.com/Eppie-io/proton-cs-client.git
```

## Usage

### Setup

Create new Proton session. For test run use these parameters:

```C#
using Tuvi.Proton.Client;

var proton = new Session(
    httpClient: new HttpClient(),
    host: new Uri("https://mail-api.proton.me"))
{
    AppVersion = "Other", 
    RedirectUri = new Uri("https://protonmail.ch")
};
```

### Authentication

Authenticate by calling `LoginAsync`. Provide username and password.

Next, check if two-factor authentication is enabled. If so, provide TOTP code with `ProvideTwoFactorCodeAsync`.

```C#
await proton.LoginAsync(
    username: "user@proton.me",
    password: "password",
    cancellationToken: CancellationToken.None);

if (proton.IsTwoFactor && proton.IsTOTP)
{
    await proton.ProvideTwoFactorCodeAsync(
        code: "123456", 
        cancellationToken: CancellationToken.None);
}
```

### Logout

`LogoutAsync` closes the session.

```C#
await proton.LogoutAsync(cancellationToken: CancellationToken.None);
```

### Refresh Session

`RefreshAsync` asynchronously refreshes `AccessToken`.

```C#
await proton.RefreshAsync(cancellationToken: CancellationToken.None);
```

### Store session

 If you want to store the session for, call `Dump`. The return value will contain JSON data, that can be passed later to `Load` and `RefreshAsync`.

```C#
string protonDump = proton.Dump();
Save(protonDump);
```

Sample result:

```Json
{
    "Version":1, 
    "Uid":"7ry2z7aydqhqir4a3xe7pcyqyqblkzmp",
    "AccessToken":"xvth2getrrhfuvvw5lfnkd7k3esfdbz7",
    "TokenType":"Bearer",
    "RefreshToken":"n4am4teh7htzbkhjyr2rg4fsut35ec46",
    "PasswordMode":1,
    "Scope":"full self payments keys parent user loggedin nondelinquent mail vpn calendar drive pass verified"
}
```

### Load session

To `Load` previvously saved session, provide a JSON formatted string created by `Dump`:

```C#
string protonDump = ExampleLoadProtonDumpMethod();
proton.Load(dump: protonDump);
```

### API Calls

After successful authentication you are ready to make API calls to Proton server. For more information on available requests refer to [official Proton API repository](https://github.com/ProtonMail/go-proton-api).

`RequestAsync` makes a request asynchronously.

The following demo shows how to count the number of messages in the maillbox. This will return a value of type `TotalMessagesContent` that results from deserializing the content as JSON.

Example:

```C#
// TotalMessagesContent type
struct TotalMessagesContent
{
    public int Code { get; set; } 
    public string Error { get; set; } 
    public JsonObject Details { get; set; } 

    public IList<Folder> Counts { get; set; } 

    public record Folder
    {
        public string LabelID { get; set; } 
        public long Total { get; set; } 
        public long Unread { get; set; }
    }
}

// api request
TotalMessagesContent countResponse = await proton.RequestAsync<TotalMessagesContent>(
    endpoint: new Uri("/mail/v4/messages/count", UriKind.Relative),
    method: HttpMethod.Get,
    headers: null,
    cancellationToken: CancellationToken.None);
```
