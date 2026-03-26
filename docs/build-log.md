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
