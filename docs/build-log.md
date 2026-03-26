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
Phase 5 — Expand the question bank from 100 to 1000 questions, maintaining SAA-C03 domain proportions and adding new AWS service categories.

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
