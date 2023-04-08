# Proton API C# Client [![Build and Test](https://github.com/Eppie-io/proton-cs-client/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/Eppie-io/proton-cs-client/actions/workflows/build-and-test.yml)

**Licence** [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0)

## Description

This library is a C# (netstandard 2.0) implementation of Proton API Client. It is meant for use by C# applications that want to authenticate at [Proton](https://proton.me/) server.

The official Python client can be found [here](https://github.com/ProtonMail/proton-python-client).

### Dependencies

- [TuviAuthProtonLib](https://github.com/Eppie-io/TuviAuthProtonLib)
- [TuviRestClientLib](https://github.com/Eppie-io/TuviRestClientLib)
- [TuviSRPLib](https://github.com/Eppie-io/TuviSRPLib)

### Installation

- Add this repository as a submodule to your git repository;
- Update submodules recursively;
- Add the following projects to the solution:
  - `proton-cs-client.csproj`
  - `TuviAuthProtonLib.csproj`
  - `TuviProtonPrimitiveLib.csproj`
  - `TuviRestClientLib.csproj`
  - `ProtonBase64Lib.scproj`
  - `TuviSRPLib.scproj`
- Add the `proton-cs-client.csproj` project reference to your projects that will use `Proton`.

```shell

cd <repository folder path>

git submodule add https://github.com/Eppie-io/proton-cs-client.git submodules/proton-cs-client

git submodule update --init --recursive

# Unix/Linux shell

# globstar feature from Bash version 4 or higher.
# If set, the pattern ** used in a filename expansion context
shopt -s globstar 

dotnet sln <solution-file> add --solution-folder submodules **/proton-cs-client.csproj **/*Lib.csproj

dotnet add <project-file> reference **/proton-cs-client.csproj

# Windows PowerShell
dotnet sln <solution-file> add --solution-folder submodules (ls -r **/proton-cs-client.csproj) (ls -r **/*Lib.csproj)

dotnet add <project-file> reference (ls -r **/proton-cs-client.csproj)
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
    username: "<user@proton.me>",
    password: "<password>");

if (proton.IsTwoFactor && proton.IsTOTP)
{
    await proton.ProvideTwoFactorCodeAsync(code: "<123456>");
}
```

### Logout

`LogoutAsync` closes the session.

```C#
await proton.LogoutAsync();
```

### Refresh Session

`RefreshAsync` asynchronously refreshes `AccessToken`.

```C#
await proton.RefreshAsync();
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

To `Load` previously saved session, provide a JSON formatted string created by `Dump`:

```C#
string protonDump = "<previously saved session state>";
proton.Load(dump: protonDump);
```

### API Calls

After successful authentication you are ready to make API calls to Proton server. For more information on available requests refer to [official Proton API repository](https://github.com/ProtonMail/go-proton-api).

`RequestAsync` makes a request asynchronously.

The following demo shows how to count the number of messages in the mailbox. This will return a value of type `TotalMessagesContent` that results from deserializing the content as JSON.

Example:

```C#
// The type represents the JSON response value
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

// Api request
TotalMessagesContent countResponse = await proton.RequestAsync<TotalMessagesContent>(
    endpoint: new Uri("/mail/v4/messages/count", UriKind.Relative),
    method: HttpMethod.Get,
    headers: null);
```
