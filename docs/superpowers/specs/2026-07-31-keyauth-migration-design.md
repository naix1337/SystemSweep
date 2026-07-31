# KeyAuth-Migration Design

**Datum:** 2026-07-31
**Status:** Genehmigt (Brainstorming)
**Projekt:** SystemSweep (`cleaner1/`, .NET 8 WPF, WPF-UI 3.x)

## Ziel

Das bisherige Lizenz-/Aktivierungssystem (RSA-2048-Offline-Signaturen + Keyzy-Online-Fallback) komplett durch **KeyAuth** als Auth-Backend ersetzen. Kein eigenes Key-Signing, keine Offline-Signatur-Validierung mehr.

## Recherche-Ergebnis (Stand 2026-07-31, offizielle KeyAuth C#-SDK)

Quelle: `KeyAuth/KeyAuth-CSHARP-Example` auf GitHub.

- API-Endpoint aktuell: **`https://keyauth.win/api/1.3/`** (POST, form-urlencoded).
- Jede Response ist **Ed25519-signiert**: Header `x-signature-ed25519` + `x-signature-timestamp` (max. 20 s alt). Verifikation mit dem Public Key aus der offiziellen SDK
  (`5586b4bc69c7a4b487e4563a4cd96afd39140f919bd31cea7d1c6a1e8439422b`).
- Relevante Endpoints:
  - `type=init` → Session-ID (`sessionid`). Felder: `ver`, `name`, `ownerid`.
  - `type=license` → Key-Login. Felder: `key`, `hwid`, `sessionid`, `name`, `ownerid`.
  - `type=check` → Session validieren. Felder: `sessionid`, `name`, `ownerid`.
- Response-Struktur: `success`, `message`, `sessionid`, `info { username, ip, hwid, createdate, lastlogin, subscriptions[{ subscription, expiry, timeleft }] }`.
- **`secret` wird im Client-SDK v1.3 nicht mehr mitgeschickt** (nur Name/Ownerid/Version). Secret wird für Server-/Seller-API genutzt.
- HWID der SDK: `WindowsIdentity.GetCurrent().User.Value` (Windows-User-SID). KeyAuth bindet den Key server-seitig an die gesendete HWID.

## Nutzer-Entscheidungen (Klärungsfragen)

1. **Kein lokaler Trial.** Aktivierung = Key-Eingabe, darunter ein **„Demo-Modus"-Button**. Im Demo-Modus dürfen keine Buttons/Tweaks ausgeführt werden.
2. **Nur online.** Kein Offline-Cache, keine Grace-Period. Ohne bestätigte KeyAuth-Session kein Vollzugriff. Netzwerkfehler → Aktivierungsdialog (Demo möglich).
3. **`tools/KeyGenerator` komplett entfernen** (inkl. committeter `private.key`, `public-key-cs.txt`). Keys werden nur noch im KeyAuth-Dashboard erstellt.
4. **Ansatz A:** Eigener sauberer `KeyAuthService` (HttpClient) + nur das pure-C#-`Ed25519.cs` aus der SDK vorkopieren. Kein SDK-Junk (TerminateProcess, SSL-Pinning, Atom-Threads).

## Architektur

| Datei | Typ | Zweck |
|---|---|---|
| `Services/AppEnv.cs` | neu | Mini-.env-Parser (kein NuGet). Liest `.env` aus App-Basisordner, CWD, dann Projekt-Root. Stellt `KEYAUTH_NAME`, `KEYAUTH_OWNERID`, `KEYAUTH_SECRET`, `KEYAUTH_VERSION` bereit. |
| `Services/KeyAuthService.cs` | neu | `InitAsync()`, `LoginWithKeyAsync(key)`, `CheckAsync()`. Ed25519-Verifikation jeder Response (fail-closed). Zustand: SessionId, Username, Subscription, Expiry, IsAuthenticated. |
| `Services/Ed25519.cs` | neu (vendoriert) | Pure-C#-Ed25519-Verifikation aus offizieller SDK; Public Key aus SDK. Keine weiteren SDK-Teile. |
| `AppLicense.cs` | neu | Statischer Lizenz-Zustand: `Mode` (Full/Demo), Username, SubscriptionName, Expiry, `IsFullAccess`. Wird von Pages zur Feature-Gate geprüft. |
| `.env` | neu (gitignored) | Echte Werte (Name/Ownerid/Secret/Version). |
| `.env.example` | neu (committet) | Platzhalter, dokumentiert Variablen. |

## Credentials

- Name: `System sweep`
- Ownerid: `yuuA7J35yC`
- Secret: `56dc5c56dc483087774db04b70ef1f19c9e422e13e59bde6cfa7ca2ddf1316b9`
- Version: `1.0` (aus `.env`; Dashboard-Version einsetzen, sobald bekannt)

Kein Hardcoding im Source. `.env` wird in `.gitignore` aufgenommen. `.env.example` wird committet. Git-History-Check ergab: **keine KeyAuth-Credentials in vergangenen Commits**.

## Start-Flow (App.xaml.cs Umbau)

1. Schutz-Checks, Settings, Theme, MainWindow — unverändert.
2. Gespeicherten Key aus `license.key` (DPAPI, bestehendes Format vereinfacht auf reines Key-String) laden.
3. Wenn Key vorhanden → `KeyAuthService.InitAsync()` dann `LoginWithKeyAsync(key)`.
   - Erfolg → `AppLicense.Mode = Full`, Session-Daten übernehmen, 4-Minuten-`CheckAsync()`-Timer starten.
   - Fehler (invalid/expired/offline) → `license.key` löschen, Aktivierungsdialog.
4. Kein Key → Aktivierungsdialog.
5. Aktivierungsdialog-Ergebnisse:
   - **Aktiviert** → Key in `license.key` speichern (DPAPI), Vollzugriff.
   - **Demo** → `AppLicense.Mode = Demo`, App läuft eingeschränkt.
   - **Fenster geschlossen** → App beendet.

## Demo-Modus (Feature-Gating)

- Aktivierungsdialog: Key-Textfeld + „Demo-Modus"-Button.
- `AppLicense.IsFullAccess == false`:
  - Alle Aktions-Buttons (Clean, Tweaks, Duplikate löschen, Browser-Cache leeren, Startup ändern, Restore) `IsEnabled=false`.
  - Guard am Anfang jedes Aktions-Handlers (doppelte Absicherung).
  - Optional Hinweis-Text „🔒 Demo — Lizenzkey erforderlich".
  - Dashboard/Stats/Lizenz/About bleiben lesbar.

Betroffene Pages: `CleanPage`, `TweaksPage`, `BrowserCachePage`, `DuplicatesPage`, `StartupPage` (Bestandsaufnahme der Aktions-Buttons in der Implementierung).

## UI

- **ActivationDialog.xaml/.cs**: Trial-Button → „Demo-Modus"; Untertitel-Hinweis aktualisieren; KeyAuth-Fehlermeldungen (`response.message`) direkt anzeigen (z. B. „Invalid key", „Key expired", „User banned"). Rate-Limit (5 Versuche / 2 s) beibehalten.
- **LicensePage.xaml/.cs**: KeyAuth-Felder anzeigen — Username, Subscription, Ablaufdatum aus `subscriptions[0].expiry` (Unix→DateTime), Key maskiert. Im Demo-Modus entsprechend „Demo" anzeigen. RSA/Keyzy-Logik entfernen.
- **AboutPage**: kleine Lizenzstatus-Zeile (optional, wird im Plan final entschieden).

## Entfernen

- `Services/LicenseService.cs` (RSA 2048, Trial, MachineFP) — löschen
- `Services/KeyzyLicenseService.cs` — löschen
- `tools/KeyGenerator/` inkl. `private.key`, `public-key-cs.txt`, `batch-licenses.txt` (falls vorhanden) — löschen
- `keyzy-config.example.json` — löschen (ersetzt durch `.env.example`)
- Trial-Logik (`trial.dat`, `TrialDaysRemaining`, `ResetTrial`) — entfällt mit LicenseService

## Nicht-Modifikationen

- `cleaner1.csproj`: keine RSA-/Keyzy-Pakete zu entfernen (RSA nutzte Framework-eigene `System.Security.Cryptography`). `System.Security.Cryptography.ProtectedData` (DPAPI für `license.key`) und `System.Management` (Machine-Fingerprint, andere Services) bleiben. Newtonsoft.Json bleibt (KeyAuth-JSON-Parsing).
- `ProtectionService.cs` bleibt unverändert (Anti-Tamper wie gehabt; keine SDK-Anti-Tamper-Routinen übernehmen).

## Sicherheit

- **Ed25519-Response-Verifikation** (fail-closed): schützt vor MITM/Response-Manipulation.
- **Session-basiert**: Features benötigen gültige Server-Session; periodischer `check()` erzwingt Re-Validierung.
- Kein lokaler Feature-State, der offline nutzbar wäre.
- `.env`-Secret: Client-`secret` wird von v1.3-Client ohnehin nicht gesendet (nur Seller-API); in `.env` für künftige Seller-Nutzung hinterlegt. `ownerid` ist per Design öffentlich im Client.
- Demo-Gate ist Client-seitig; ein Cracker kann es umgehen, gewinnt aber keinen Vollzugriff (braucht gültige Server-Session).

## Fehlerbehandlung

- `response.message` direkt im Dialog anzeigen (invalid key, expired, banned, rate-limit …).
- Netzwerkfehler/Timeout → keine Offline-Nutzung; Aktivierungsdialog mit Retry-/Demo-Option.
- Ed25519-Signaturfehler → als ungültig behandeln (fail-closed), App nicht crashen.
- Periodischer `check()` schlägt fehl → `license.key` löschen, Aktivierungsdialog, App schließt bei Nicht-Interaktion (wie bisheriges Verhalten).

## Validierung

- `dotnet build cleaner1/cleaner1.csproj` läuft fehlerfrei durch.
- Manueller Lauf mit echten KeyAuth-Credentials in `.env` (Aktivierung + Demo-Modus + Fehlerfall).
- Bestandsaufnahme: keine verbleibenden Verweise auf `LicenseService`/`KeyzyLicenseService`/`license.key`-RSA-Format.

## Hinweis: `private.key` in Git-History

Die `private.key` liegt zusätzlich in früheren Commits. Nach dieser Migration ist das RSA-System inkl. Public Key stillgelegt → der geleakte Key ist wertlos. Kein erneutes History-Rewrite geplant (außer Nutzer wünscht es explizit).

## Offene Punkte (im Implementierungsplan zu klären)

- HWID-Quelle: bestehende Machine-Fingerprint-Hash vs. Windows-User-SID (SDK-Standard). Entscheidung: bestehender Machine-Fingerprint (Kontinuität).
- Exakte Liste der Aktions-Buttons pro Page.
- AboutPage-Zeile final ja/nein.
