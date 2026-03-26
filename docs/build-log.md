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
