# AWS SAA-C03 Practice App — Build Log

---

## [Phase 1] — Question Bank (100 Questions)
**Date:** 2026-03-26
**Status:** Complete

### What was done
- Fetched the official AWS SAA-C03 exam guide via web search to extract domain names and percentage weightings.
- Generated 100 scenario-based, exam-style practice questions distributed proportionally across all four exam domains.
- Saved questions to `src/Data/questions.json`.

**Domains and distribution:**
| Domain | Weight | Questions |
|---|---|---|
| Design Secure Architectures | 30% | 30 (q001–q030) |
| Design Resilient Architectures | 26% | 26 (q031–q056) |
| Design High-Performing Architectures | 24% | 24 (q057–q080) |
| Design Cost-Optimized Architectures | 20% | 20 (q081–q100) |

**Categories covered:**
IAM, KMS, VPC, Security Groups, Network ACL, S3, RDS, Auto Scaling, Route53, EC2, CloudFront, ElastiCache, ELB, SQS, DynamoDB, Lambda, Kinesis, API Gateway, EFS, GuardDuty, WAF, CloudTrail, Secrets Manager, Inspector, Macie, Security Hub, ACM, Cognito, PrivateLink

**Question schema:**
```json
{
  "id": "q001",
  "domain": "Design Secure Architectures",
  "category": "IAM",
  "question": "...",
  "options": ["A", "B", "C", "D"],
  "correct": 1,
  "explanation": "..."
}
```

### Why
The question bank is the core content of the app. Starting here allows all subsequent phases (app, infrastructure) to be built around the actual data model. Proportional domain distribution matches the real exam scoring to give accurate simulated practice.

### How to reproduce on a new machine
No commands needed. `src/Data/questions.json` is committed to the repository. To validate the JSON:
```bash
python -m json.tool src/Data/questions.json > /dev/null && echo "Valid JSON"
```
Or with Node.js:
```bash
node -e "require('./src/Data/questions.json'); console.log('Valid')"
```

### What's next
Phase 2 — Initialise local Git repo, create `.gitignore` and `.env.example`, make initial commit, and push to GitHub.

---

## [Phase 2] — Repo and Git Setup
**Date:** 2026-03-26
**Status:** Complete (pending GitHub remote)

### What was done
- Initialised a local git repository in `aws-saa-c03-practice-app/`
- Created `.gitignore` covering: `.env`, `*.env`, `terraform.tfvars`, `*.tfstate`, `*.tfstate.backup`, `.terraform/`, `bin/`, `obj/`, `*.user`, `.vs/`, `appsettings.local.json`, Android keystores, SQLite databases
- Created `.env.example` with placeholder keys for: `AWS_REGION`, `COGNITO_USER_POOL_ID`, `COGNITO_CLIENT_ID`, `COGNITO_DOMAIN`, `S3_BUCKET_NAME`, `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `OAUTH_CALLBACK_WINDOWS`, `OAUTH_CALLBACK_ANDROID`
- Created `README.md` with full prerequisites, setup, destroy, and rebuild instructions
- Made initial commit with all Phase 1 + Phase 2 files:
  ```
  d4b402a Phase 1 & 2: Add 100-question SAA-C03 bank, repo structure, and gitignore
  ```

### Why
Git history gives a complete audit trail. The `.gitignore` is the primary security control preventing accidental secret commits. `.env.example` documents the contract between the repo and the runtime environment without exposing real values.

### How to reproduce on a new machine
```bash
# On a fresh clone, there is nothing to do — these files are in the repo.
# To verify .gitignore is protecting secrets:
git check-ignore -v .env                    # Should return: .gitignore:3:.env
git check-ignore -v infra/terraform.tfvars  # Should return: .gitignore:7:terraform.tfvars
```

### What's next
Phase 3 — Terraform infrastructure.

---

## [Phase 3] — AWS Infrastructure via Terraform
**Date:** 2026-03-26
**Status:** Complete (pending first terraform apply + Google OAuth setup)

### What was done
Created three Terraform files in `infra/`:

**`infra/main.tf`** provisions:
| Resource | Details |
|---|---|
| `aws_s3_bucket.main` | Private bucket, public access blocked, versioning off. Name: `saa-c03-practice-<random>` |
| `aws_s3_object.questions` | Uploads `src/Data/questions.json` to the bucket on apply |
| `aws_cognito_user_pool.main` | Email-based auth, password policy, `eu-west-1` |
| `aws_cognito_user_pool_domain.main` | Hosted UI domain for OAuth redirects |
| `aws_cognito_identity_provider.google` | Google IDP — created only when `google_client_id` variable is non-empty (second apply) |
| `aws_cognito_user_pool_client.main` | Public client (no secret), SRP + refresh auth, callbacks: `http://localhost` + `myapp://callback` |
| `aws_iam_policy.s3_app_access` | Scoped to `s3:ListBucket`, `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject` on this bucket only |

**`infra/variables.tf`** — region, project_name, environment, google_client_id (sensitive), google_client_secret (sensitive)

**`infra/outputs.tf`** — cognito_user_pool_id, cognito_client_id, cognito_domain, cognito_hosted_ui_url, s3_bucket_name, region

All resources tagged: `Project = "saa-c03-practice-app"`, `Environment = "dev"`

### Why
- Local Terraform state only — no S3 backend, zero extra cost, instant destroy/rebuild
- Google IDP uses `count = var.google_client_id != "" ? 1 : 0` to support a two-phase apply (create infrastructure first, then wire OAuth after getting the Cognito domain)
- No client secret on the Cognito App Client — native mobile and desktop apps cannot securely store secrets

### How to reproduce on a new machine
```bash
# Prerequisites: AWS CLI configured (aws configure), Terraform installed

cd infra
terraform init
terraform apply         # First apply — no Google credentials yet
# Note outputs; set up Google OAuth (see Phase 3 ACTION REQUIRED steps)
# Create infra/terraform.tfvars with google_client_id and google_client_secret
terraform apply         # Second apply — wires Google into Cognito

# Save outputs to file (non-sensitive, can be committed)
terraform output -json > outputs.json

# To destroy everything and reach $0:
terraform destroy
```

### What's next
ACTION REQUIRED (below): First terraform apply, then Google OAuth setup, then second apply. After confirmation — Phase 4: Build the .NET MAUI app.

---

## [Phase 4] — .NET MAUI App Scaffold
**Date:** 2026-03-26
**Status:** Complete (Windows build: 0 errors; Android build: NuGet conflict resolved, XA5300 requires Android SDK on build machine)

### What was done

Created a full .NET MAUI single-project targeting `net8.0-android` and `net8.0-windows10.0.19041.0`. All pages, ViewModels, services, and models are fully implemented — not stubs.

**Project file (`src/AwsSaaC03Practice.csproj`):**
- `WindowsPackageType=None` → runs unpackaged on Windows (no MSIX required)
- `questions.json` embedded as a `MauiAsset` (bundled into the app package)
- Android-only `PackageReference` for `Xamarin.AndroidX.Browser` (OAuth custom tab)
- AndroidX Collection packages pinned to `1.4.0.3` to prevent duplicate class errors (see Android fix below)

**NuGet packages:**
| Package | Version | Purpose |
|---|---|---|
| CommunityToolkit.Mvvm | 8.3.2 | MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`) |
| sqlite-net-pcl | 1.9.172 | Local SQLite session history |
| SQLitePCLRaw.bundle_green | 2.1.8 | SQLite native bindings |
| AWSSDK.S3 | 3.7.307.24 | S3 score sync |
| AWSSDK.CognitoIdentityProvider | 3.7.302.39 | Cognito auth |
| LiveChartsCore.SkiaSharpView.Maui | 2.0.0-rc6.1 | Domain score charts on Dashboard |
| DotNetEnv | 3.1.0 | `.env` loading for local dev |
| Xamarin.AndroidX.Browser | 1.8.0.1 | Android OAuth custom tab |

**Models (`src/Models/`):**
- `Question.cs` — id, domain, category, question, options[], correct index, explanation
- `QuizMode.cs` — enum: Random, Exam, Quick30, Quick10; extension methods `QuestionCount()`, `TimeLimit()`
- `QuizSession.cs` — SQLite table record; stores mode, userSub, startedAt, duration, correctAnswers, totalQuestions, answerDataJson (serialised `List<AnswerRecord>`)
- `SessionResult.cs` — in-memory result container; domain scores, category scores, wrong question IDs

**Services (`src/Services/`):**
- `SettingsService` — loads `appsettings.json` from the app bundle at startup; validates `CognitoClientId` is set
- `QuestionService` — deserialises `questions.json`; `GetForMode()` shuffles and trims by mode; exposes `GetAllCategories()` and `GetAllDomains()`
- `AuthService` — full PKCE OAuth flow; Windows path opens browser + spins `HttpListener` on `localhost:7890`; Android path uses `Launcher` + `TaskCompletionSource` waiting for `MainActivity.OnNewIntent`; token refresh; `SecureStorage` for token persistence
- `SessionDbService` — SQLite CRUD; `BuildResultAsync()` aggregates domain/category scores and wrong IDs; `GetStreakAsync()` computes consecutive calendar-day streak
- `S3SyncService` — upload/download `scores/{userSub}/sessions.json` using `EnvironmentVariablesAWSCredentials`

**ViewModels (`src/ViewModels/`):**
- `BaseViewModel` — `IsBusy`, `Title` observables
- `LoginViewModel` — calls `TryAutoLoginAsync()`; navigates to Dashboard or shows sign-in button
- `ModePickerViewModel` — exposes mode list, category filter, triggers quiz navigation with query params
- `QuizViewModel` — `[QueryProperty]` for mode/filter/ids; per-option colour state for answer reveal; `IDispatcherTimer` countdown for Exam mode; saves session to SQLite on completion; navigates to Results
- `ResultsViewModel` — receives completed `QuizSession`; calls `BuildResultAsync()`; exposes domain/category scores; "Retry wrong" re-launches quiz with specific IDs; triggers S3 sync
- `DashboardViewModel` — loads user info, all sessions, computes overall score, study streak, last session summary, domain scores list

**Views (`src/Views/`):**
- `LoginPage` — sign-in button, activity indicator
- `ModePickerPage` — mode cards + optional category picker
- `QuizPage` — question text, four option buttons with colour feedback, progress label, timer
- `ResultsPage` — score summary, domain breakdown list, "Retry wrong answers" and "Back to menu" buttons
- `DashboardPage` — metric cards (overall score, streak, sessions), last session card, domain score list, sign-out button

**App shell:**
- `AppShell.xaml` — Shell navigation with routes registered for all five pages
- `MauiProgram.cs` — DI registration; synchronous bootstrap loads `SettingsService` and `QuestionService` before the UI starts

### Android duplicate class fix
At initial Android build, `Type androidx.collection.ArrayMapKt is defined multiple times` due to a transitive conflict between `Xamarin.AndroidX.Collection.Jvm 1.4.0.2` and `Xamarin.AndroidX.Collection.Ktx 1.2.0.9`. In Collection 1.4.x, Kotlin extensions were merged into the JVM artifact, so having both versions in the graph duplicates classes.

Fix: pin all three Collection packages explicitly to `1.4.0.3` in the Android-only `ItemGroup`:
```xml
<PackageReference Include="Xamarin.AndroidX.Collection" Version="1.4.0.3" />
<PackageReference Include="Xamarin.AndroidX.Collection.Jvm" Version="1.4.0.3" />
<PackageReference Include="Xamarin.AndroidX.Collection.Ktx" Version="1.4.0.3" />
```

### Why
- MVVM with CommunityToolkit source generators eliminates boilerplate and keeps ViewModels lean
- `QuestionService.GetForMode()` handles all four quiz modes from a single method, keeping mode logic out of ViewModels
- `AuthService` uses PKCE (no client secret) — the only viable OAuth flow for native apps
- Windows auth uses a local `HttpListener` instead of a custom URI scheme, avoiding the need for MSIX registration
- SQLite local-first means the app works fully offline; S3 sync is additive

### How to reproduce on a new machine
```bash
# Prerequisites: .NET 8 SDK with MAUI workloads
dotnet workload install maui-android maui-windows

cd src

# Windows build (no Android SDK required)
dotnet build -f net8.0-windows10.0.19041.0

# Android build (requires Android SDK installed)
dotnet build -f net8.0-android
```
The app requires `src/Resources/Raw/appsettings.json` to run (not committed). Copy `appsettings.example.json` and fill in Cognito and S3 values from `terraform output`.

### What's next
Expand the question bank from 100 to 1000 questions, maintaining SAA-C03 domain proportions and adding new AWS service categories.

---

## Security Hardening for Play Store
**Date:** 2026-03-26
**Status:** Complete

### Changes made

#### 1. APK Secrets Exposure — Audited, no action needed
All values in `appsettings.json` are configuration identifiers, not secrets:
- `CognitoClientId` — public by AWS design (Cognito public clients have no client secret)
- `CognitoUserPoolId`, `CognitoDomain`, `S3BucketName` — infrastructure identifiers, not credentials
- `AwsRegion`, `OAuthCallback*` — public configuration values
- Google OAuth client ID/secret — exist only server-side in Terraform (`sensitive = true`)
- AWS SDK credentials — not in the APK; obtained at runtime via Cognito Identity Pool

#### 2. S3 Access Hardening — Per-user scoping via Identity Pool
**Before:** The IAM policy `s3_app_access` granted read/write to ALL objects in the S3 bucket. The app used `EnvironmentVariablesAWSCredentials` — unusable on mobile devices.

**After:**
- Added **Cognito Identity Pool** (`aws_cognito_identity_pool.main`) that exchanges ID tokens for temporary AWS credentials
- Created **per-user IAM role** (`aws_iam_role.cognito_authenticated`) with inline policy scoped to `scores/${cognito-identity.amazonaws.com:sub}/*`
- Each user can ONLY access their own folder — no cross-user data access possible
- Read-only access to `questions.json` for all authenticated users
- `S3SyncService` now uses `CognitoAWSCredentials` instead of `EnvironmentVariablesAWSCredentials`
- Added `AWSSDK.CognitoIdentity` NuGet package
- Added `CognitoIdentityPoolId` to `AppSettings` and `appsettings.example.json`
- Removed the old broad `aws_iam_policy.s3_app_access` resource

#### 3. Cognito Hardening
- **MFA:** Enabled as OPTIONAL (`mfa_configuration = "OPTIONAL"`, `software_token_mfa_configuration { enabled = true }`) — users can enable TOTP in their account
- **Token expiry:** Already correct: 1h access token, 1h ID token, 30d refresh token
- **Anti-enumeration:** Added `prevent_user_existence_errors = "ENABLED"` on the App Client — prevents attackers from discovering which emails have accounts
- **Advanced security:** `user_pool_add_ons { advanced_security_mode = "AUDIT" }` was NOT applied — requires Cognito PLUS tier (additional cost). Documented as a future upgrade option in `main.tf`.
- **Rate limiting:** Built into Cognito service by default; no additional configuration needed

#### 4. Android Manifest Permissions — Audited, minimal changes
- Kept `INTERNET` and `ACCESS_NETWORK_STATE` (required by MAUI and HTTP calls)
- Set `android:allowBackup="false"` — prevents `adb backup` extraction of app data
- Set `android:usesCleartextTraffic="false"` — blocks all non-HTTPS traffic
- Removed `BrowserTabActivity` (MSAL artifact, unused)
- Removed `<queries>` entry for `http` scheme (only `https` retained)
- Added `android:networkSecurityConfig` reference for cert pinning

#### 5. OAuth Callback URI — Renamed to app-specific scheme
Changed `myapp://callback` → `selimcelemsaaapp://callback` everywhere:
- `MainActivity.cs` — `IntentFilter DataScheme` and scheme check
- `SettingsService.cs` — default `OAuthCallbackAndroid` value
- `appsettings.example.json`, `.env.example`
- Terraform `aws_cognito_user_pool_client.main` — `callback_urls` and `logout_urls`

This prevents other apps from intercepting the OAuth callback via the generic `myapp://` scheme.

#### 6. Certificate Pinning — Android network security config
Created `Platforms/Android/Resources/xml/network_security_config.xml`:
- Pins `amazonaws.com` and `amazoncognito.com` (including subdomains) to Amazon Root CA 1-4 and Starfield Services Root CA G2
- Pin set expires `2027-01-01` — must be reviewed before this date
- Blocks all cleartext traffic (`cleartextTrafficPermitted="false"`)
- Exception for `localhost` (Windows OAuth callback during development)

**Note:** This is Android-only (OS-level enforcement). Windows currently relies on the default .NET TLS validation chain. For production Windows release, consider adding `HttpClientHandler.ServerCertificateCustomValidationCallback` pinning.

#### 7. R8 / ProGuard Obfuscation — Enabled for Release builds
Added to `.csproj` for Android Release configuration:
- `AndroidLinkMode=SdkOnly` — IL linker removes unused SDK code
- `RunAOTCompilation=true` — ahead-of-time compilation
- `AndroidEnableProfiledAot=true` — profiled AOT for faster startup
- `PublishTrimmed=true` — tree-shakes unused code
- `ProguardConfiguration=Platforms\Android\proguard-rules.pro` — keeps reflection targets: AWS SDK, SQLite models, JSON serialisation, LiveCharts, SkiaSharp

#### 8. Privacy Policy — PRIVACY_POLICY.md created
Covers: data collected (email, display name, quiz scores), storage location (AWS S3 eu-west-1), no data selling/sharing, data retention, children's privacy. Contact email is a placeholder — **must be updated before Play Store submission**.

#### 9. Bug Fixes (also in this pass)
- **Crash on exam completion:** `ResultsViewModel` was loading sessions filtered by `UserSub == ""`, missing logged-in users' sessions. Fixed: added `GetSessionByIdAsync(int id)` to `SessionDbService`, updated `ResultsViewModel` to use it.
- **Answer always index 1:** Options displayed in JSON order (81/100 had `correct: 1`). Fixed: `QuizViewModel.ShowQuestion()` now shuffles option display order and maps answers back to original indices.
- **Ugly OAuth URL on Android:** Switched from `Launcher.OpenAsync()` to Chrome Custom Tabs (`AndroidX.Browser.CustomTabs.CustomTabsIntent`) for a cleaner in-app toolbar.
- **Answer button text truncation:** Replaced `Button` elements with `Border` + `Label` + `TapGestureRecognizer` using `LineBreakMode="WordWrap"` for reliable text wrapping.
- **Global crash logging:** Added `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` handlers that write to `{AppDataDirectory}/crash.log`.

### Terraform apply result
```
Apply complete! Resources: 4 added, 1 changed, 0 destroyed.
  + aws_cognito_identity_pool.main
  + aws_iam_role.cognito_authenticated
  + aws_iam_role_policy.cognito_s3_per_user
  + aws_cognito_identity_pool_roles_attachment.main
  ~ aws_cognito_user_pool_client.main (callback URIs, prevent_user_existence_errors)
  - aws_iam_policy.s3_app_access (replaced by per-user role policy)
```

### Still needed before Play Store submission
- [ ] Replace `[your-email@example.com]` in `PRIVACY_POLICY.md` with a real contact email
- [ ] Host the privacy policy at a public URL and add it to the Play Console listing
- [ ] Generate a release signing keystore and configure it in the csproj
- [ ] Update `CognitoIdentityPoolId` in `appsettings.json` with the Terraform output value (`eu-west-1:eb8572ad-8d4e-4014-8e8d-09bde1e1ed92`)
- [ ] Run a full Android release build and test the APK on a physical device
- [ ] Review certificate pin expiry (2027-01-01) and set a calendar reminder to update
- [ ] Consider upgrading to Cognito PLUS tier for Threat Protection if budget allows
- [ ] Add Windows-side certificate pinning for the AWS SDK HttpClient

---
