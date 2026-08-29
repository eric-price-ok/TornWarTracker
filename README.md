# Torn War Tracker

A WPF desktop app that watches your faction's active ranked war, pulls the
enemy roster + status from Torn, merges in battle-stat estimates from
FFScouter, and lets you copy a formatted line for faction chat with one click.

## Requirements to build/run

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)
- VS Code with the **C# Dev Kit** extension (or plain C# extension + terminal)
- A Torn API key (torn.com → Settings → API) — a basic/limited key should be
  enough since faction roster/status is public data.
- An FFScouter API key — sign up at https://ffscouter.com, generate a key,
  and **register it on their site** (this walks you through their data
  policy, which Torn requires before a key is used with a third-party tool).
  The app itself doesn't implement the registration flow — do that once on
  ffscouter.com, then paste the key into the app.

## Running it during development

```
cd TornWarTracker
dotnet restore
dotnet run
```

## Building the standalone .exe (no install required to run it)

These flags used to be baked into the .csproj, but having
`RuntimeIdentifier`/`SelfContained` set unconditionally there also affects
plain `dotnet build`/`dotnet run`, and on at least one machine that caused
the WPF framework references to not resolve correctly (showing up as
"`ICollectionView` could not be found" even though the code is fine). Passing
them only at publish time avoids that:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output lands at:

```
bin\Release\net8.0-windows\win-x64\publish\TornWarTracker.exe
```

That single file is everything — hand it to whoever's calling the war and
they just double-click it, no .NET install needed.

## First run

1. Open the app, fill in your Torn API key, FFScouter API key, and your own
   faction ID, then **Save Settings**.
2. Hit **Start**. It'll poll your faction's ranked wars until it finds an
   active one, then auto-load the enemy roster.
3. Use the filter bar to hide Traveling/Abroad/Online members, restrict by
   level or stat estimate, or cap Fair Fight (default max 3.0, uncheck to
   disable).
4. **Copy** builds the caller-chat line (stats + profile link). **Claim**
   builds the "I'm taking this one" line (no stats, no link).

## Known rough edges / things to check before relying on this in a war

- **Torn's exact JSON field names weren't verified live.** I built the
  parsing against Torn's documented v1/v2 conventions
  (`status.state`, `status.until`, `last_action.status`,
  `rankedwars[].factions[].id`, etc.) but couldn't make an authenticated
  test call while writing this. If members or war detection come back
  empty/wrong, the fix is isolated to `TornApiClient.ParseMembers()` and
  `TornApiClient.ParseActiveWarOpponent()` — hit the endpoints once with
  your key (e.g. via https://www.torn.com/swagger.php) and adjust the
  property names there to match what actually comes back.
- **No retry/backoff on rate limits yet.** Fine at the discussed poll
  cadence (Torn allows 100/min, FFScouter 20/min, and this app makes one
  call to each per cycle), but worth adding if you drop the interval a lot
  or add more factions.
- **No input validation** on the settings/filter text boxes yet — an empty
  or non-numeric value in a numeric field will show a red binding-error
  border rather than crash, but it's not pretty.
- **History DB has no viewer.** Every poll's snapshot is written to
  `%AppData%\TornWarTracker\history.db` (SQLite) but there's no in-app way
  to browse it yet — open it with any SQLite tool if you want to look back
  at a war afterward.
- **No icon/installer polish** — this is a functional scaffold, not a
  shipped product.

## Project layout

```
Models/       Plain data classes (FactionMember, MemberStatus, AppConfig, ...)
Services/     TornApiClient, FfScouterClient, ConfigService, DatabaseService,
              ClipboardTextBuilder
ViewModels/   MainViewModel (polling, filtering, commands),
              MemberRowViewModel (one grid row)
Utilities/    RelayCommand, ObservableObject (small MVVM helpers, no
              external framework dependency)
MainWindow.xaml(.cs)   The UI
```
