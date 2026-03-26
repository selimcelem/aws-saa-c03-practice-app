# AWS SAA-C03 Practice App

A cross-platform quiz application for Windows desktop and Android that helps you study for the **AWS Certified Solutions Architect – Associate (SAA-C03)** exam.

> Built with .NET MAUI (.NET 8), backed by AWS Cognito + S3, infrastructure managed via Terraform.

---

## Features (Roadmap)

- 100 → 1000 scenario-based practice questions across all 4 SAA-C03 exam domains
- Four study modes: Random, Exam Simulation (65Q / 130min), Quick 30, Quick 10
- Detailed answer explanations with AWS concept references
- Performance dashboard: score by domain, radar chart, "needs work" categories
- Google Sign-In via Cognito, score history synced to S3
- Local SQLite history — works offline

---

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 8.0+ | https://dot.net |
| MAUI workloads | - | `dotnet workload install maui-android maui-windows` |
| AWS CLI | 2.x | https://aws.amazon.com/cli |
| Terraform | 1.7+ | https://terraform.io/downloads |
| Git | any | https://git-scm.com |
| Android device or emulator | Android 8.0+ | - |

---

## Setup from Scratch

### 1. Clone the repository
```bash
git clone https://github.com/<your-username>/aws-saa-c03-practice-app.git
cd aws-saa-c03-practice-app
```

### 2. Configure AWS credentials
```bash
aws configure
# Enter: Access Key ID, Secret Access Key, region: eu-west-1, output: json
```

### 3. Deploy infrastructure
```bash
cd infra
terraform init
terraform apply
# Note the outputs: cognito_user_pool_id, cognito_client_id, s3_bucket_name, cognito_domain
```

### 4. Set up Google OAuth
1. Go to [Google Cloud Console](https://console.cloud.google.com) → APIs & Services → Credentials
2. Create an OAuth 2.0 Client ID (Web application type)
3. Add the Cognito domain as an authorised redirect URI: `https://<cognito_domain>/oauth2/idpresponse`
4. Copy the Client ID and Client Secret

### 5. Configure environment variables
```bash
cp .env.example .env
# Edit .env with your Terraform outputs and Google OAuth credentials
```

### 6. Wire Google into Cognito
```bash
cd infra
terraform apply  # Second apply with Google credentials wired in
```

### 7. Build and run

**Windows:**
```bash
dotnet build -f net8.0-windows10.0.19041.0
dotnet run -f net8.0-windows10.0.19041.0
```

**Android (USB device):**
```
Settings → About Phone → tap Build Number 7 times → enable Developer Options → enable USB Debugging
Plug phone into PC via USB and accept the debugging prompt
```
```bash
dotnet build -t:Run -f net8.0-android
```

---

## Destroying Infrastructure

To bring cost to exactly **$0**:
```bash
cd infra
terraform destroy
```

All resources are tagged `Project=saa-c03-practice-app` for easy identification.

## Rebuilding from Scratch

Follow the "Setup from Scratch" steps above. `terraform apply` rebuilds everything in under 5 minutes. The question bank is committed to the repository.

---

## Project Structure

```
aws-saa-c03-practice-app/
├── docs/
│   └── build-log.md          # Detailed step-by-step build history
├── src/
│   ├── Data/
│   │   └── questions.json    # 100–1000 practice questions
│   ├── Models/               # C# data models
│   ├── ViewModels/           # MVVM view models
│   ├── Views/                # MAUI XAML pages
│   └── Services/             # AWS, auth, storage services
├── infra/
│   ├── main.tf               # S3, Cognito, IAM resources
│   ├── variables.tf
│   └── outputs.tf
├── .gitignore
├── .env.example              # Template — copy to .env
└── README.md
```

---

## Phase 6 — 1000 Questions

The question bank will be expanded to 1000 questions maintaining domain proportions. See `docs/build-log.md` for progress.

---

## License

MIT
