# Question Bank Expansion — 100 → 1000 Questions

**Date:** 2026-03-26
**Status:** Approved

---

## Goal

Expand `src/Data/questions.json` from 100 to 1000 scenario-based SAA-C03 practice questions while maintaining exam-accurate domain proportions and broadening AWS service coverage with ~15 new categories.

---

## Domain Distribution

| Domain | % | Current | New | Total |
|---|---|---|---|---|
| Design Secure Architectures | 30% | 30 | 270 | 300 |
| Design Resilient Architectures | 26% | 26 | 234 | 260 |
| Design High-Performing Architectures | 24% | 24 | 216 | 240 |
| Design Cost-Optimized Architectures | 20% | 20 | 180 | 200 |
| **Total** | | **100** | **900** | **1000** |

---

## ID Sequence

- Existing: `q001`–`q100` (unchanged)
- New: `q101`–`q1000` (zero-padded to four digits)

---

## Schema

Identical to existing — no schema changes:

```json
{
  "id": "q101",
  "domain": "Design Secure Architectures",
  "category": "Network Firewall",
  "question": "A company needs to inspect and filter...",
  "options": [
    "A. ...",
    "B. ...",
    "C. ...",
    "D. ..."
  ],
  "correct": 2,
  "explanation": "..."
}
```

Fields:
- `id` — string, `q` + zero-padded 4-digit number
- `domain` — one of the four SAA-C03 domain strings exactly
- `category` — AWS service or concept name (see category lists below)
- `question` — scenario-based, exam-style, ≥2 sentences
- `options` — exactly 4 strings, each prefixed `A. ` / `B. ` / `C. ` / `D. `
- `correct` — 0-based index of the correct option (0–3)
- `explanation` — explains why the correct answer is right and why the distractors are wrong

---

## Categories by Domain

### Design Secure Architectures (300 total, 270 new)

**Scale up existing:**
IAM, KMS, VPC, Security Groups, Network ACL, WAF, GuardDuty, CloudTrail, Secrets Manager, ACM, Cognito, Inspector, Macie, Security Hub

**New categories:**
- Network Firewall
- IAM Identity Center (SSO)
- Resource Access Manager (RAM)
- AWS Shield
- Security Lake
- AWS Config
- Amazon Detective

### Design Resilient Architectures (260 total, 234 new)

**Scale up existing:**
EC2, RDS, Auto Scaling, Route53, ELB, S3, SQS, DynamoDB, ElastiCache, EFS

**New categories:**
- ECS/Fargate
- EKS
- Step Functions
- EventBridge
- SNS
- Global Accelerator
- Elastic Disaster Recovery (DRS)

### Design High-Performing Architectures (240 total, 216 new)

**Scale up existing:**
S3, Lambda, DynamoDB, ElastiCache, CloudFront, Kinesis, API Gateway, EFS

**New categories:**
- Redshift
- Athena
- Glue
- OpenSearch
- EMR
- MSK (Managed Streaming for Kafka)

### Design Cost-Optimized Architectures (200 total, 180 new)

**Scale up existing:**
EC2, S3, RDS, Lambda, CloudFront, Auto Scaling

**New categories:**
- Savings Plans / Reserved Instances
- Spot Instances strategies
- Compute Optimizer
- Trusted Advisor
- S3 Intelligent-Tiering / Lifecycle policies
- FSx

---

## Data Quality Constraints

- Every question must be scenario-based: a business or technical situation, not a definition question
- Distractors must be plausible AWS services or configurations — not obviously wrong
- Correct answer index must be distributed across 0–3 (no systematic bias toward one index)
- Explanations must state why the correct answer is right AND why each distractor is wrong (or at least why the correct answer is preferred)
- No question text or scenario may be a near-duplicate of an existing question
- Category names must use exact casing from the lists above (fixes existing `Elasticache` → `ElastiCache` typo)

---

## Generation Strategy

Four sequential domain batches, one per domain. Each batch is a separate generation pass focused on one domain's services. After each batch, the JSON is validated before the next batch starts.

**Batch order and ID ranges:**

| Batch | Domain | IDs | Count |
|---|---|---|---|
| 1 | Design Secure Architectures | q101–q370 | 270 |
| 2 | Design Resilient Architectures | q371–q604 | 234 |
| 3 | Design High-Performing Architectures | q605–q820 | 216 |
| 4 | Design Cost-Optimized Architectures | q821–q1000 | 180 |

---

## Pre-existing Data Fix

Before appending new questions, fix the casing inconsistency in `q001`–`q100`:
- `"category": "Elasticache"` (q-unknown) → `"category": "ElastiCache"`

---

## Validation (after all batches)

Run a validation script that checks:
1. Total count == 1000
2. All IDs unique and in range `q001`–`q1000`
3. `correct` field is 0, 1, 2, or 3 for every question
4. Each question has exactly 4 options
5. Domain strings match exactly one of the four valid domain names
6. No duplicate question text

---

## Files Changed

- `src/Data/questions.json` — expanded from 100 to 1000 entries
- `docs/build-log.md` — Phase 5 entry added after completion
