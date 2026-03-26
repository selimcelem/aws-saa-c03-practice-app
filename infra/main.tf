terraform {
  required_version = ">= 1.3"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "aws" {
  region = var.region
}

# ─── Random suffix for globally unique names ─────────────────────────────────

resource "random_id" "suffix" {
  byte_length = 4
}

locals {
  suffix = random_id.suffix.hex
  tags = {
    Project     = "saa-c03-practice-app"
    Environment = var.environment
  }
}

# ─── S3 Bucket ───────────────────────────────────────────────────────────────
# Stores questions.json and per-user score backups as JSON files
# keyed by Cognito sub ID: scores/<sub-id>/sessions.json

resource "aws_s3_bucket" "main" {
  bucket = "${var.project_name}-${local.suffix}"
  tags   = local.tags
}

resource "aws_s3_bucket_public_access_block" "main" {
  bucket = aws_s3_bucket.main.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "main" {
  bucket = aws_s3_bucket.main.id
  versioning_configuration {
    status = "Disabled"
  }
}

# Upload the question bank so the app can fetch it from S3
resource "aws_s3_object" "questions" {
  bucket       = aws_s3_bucket.main.id
  key          = "questions.json"
  source       = "${path.module}/../src/Data/questions.json"
  content_type = "application/json"
  etag         = filemd5("${path.module}/../src/Data/questions.json")
  tags         = local.tags
}

# ─── Cognito User Pool ───────────────────────────────────────────────────────

resource "aws_cognito_user_pool" "main" {
  name = "${var.project_name}-pool-${local.suffix}"

  # Users sign in with their email address
  username_attributes      = ["email"]
  auto_verified_attributes = ["email"]

  # Optional MFA — users can enable TOTP in their account settings
  mfa_configuration = "OPTIONAL"
  software_token_mfa_configuration {
    enabled = true
  }

  password_policy {
    minimum_length    = 8
    require_lowercase = true
    require_numbers   = true
    require_symbols   = false
    require_uppercase = true
  }

  # Note: advanced_security_mode (Threat Protection) requires the PLUS tier
  # which costs extra. Omitted to stay on the ESSENTIALS tier / free tier.
  # If upgraded to PLUS in the future, add:
  #   user_pool_add_ons { advanced_security_mode = "AUDIT" }

  # Expose name and picture from social provider tokens
  schema {
    name                     = "email"
    attribute_data_type      = "String"
    required                 = true
    mutable                  = true
    string_attribute_constraints {
      min_length = 1
      max_length = 256
    }
  }

  tags = local.tags
}

# ─── Cognito Hosted UI Domain ─────────────────────────────────────────────────
# Required for Google OAuth redirect flow.
# The full domain will be:
#   https://<domain_prefix>.auth.<region>.amazoncognito.com

resource "aws_cognito_user_pool_domain" "main" {
  domain       = "${var.project_name}-${local.suffix}"
  user_pool_id = aws_cognito_user_pool.main.id
}

# ─── Google Identity Provider ────────────────────────────────────────────────
# Only created once Google credentials are supplied (second terraform apply).
# On first apply: google_client_id = "" → count = 0 → resource skipped.

resource "aws_cognito_identity_provider" "google" {
  count = var.google_client_id != "" ? 1 : 0

  user_pool_id  = aws_cognito_user_pool.main.id
  provider_name = "Google"
  provider_type = "Google"

  provider_details = {
    client_id                     = var.google_client_id
    client_secret                 = var.google_client_secret
    authorize_scopes              = "email openid profile"
    attributes_url                = "https://people.googleapis.com/v1/people/me?personFields="
    attributes_url_add_attributes = "true"
    authorize_url                 = "https://accounts.google.com/o/oauth2/v2/auth"
    oidc_issuer                   = "https://accounts.google.com"
    token_request_method          = "POST"
    token_url                     = "https://www.googleapis.com/oauth2/v4/token"
  }

  attribute_mapping = {
    email    = "email"
    username = "sub"
    name     = "name"
    picture  = "picture"
  }
}

# ─── Cognito App Client ───────────────────────────────────────────────────────
# Public client — no client secret (required for native mobile/desktop apps).
# Supports Google when credentials are available.

resource "aws_cognito_user_pool_client" "main" {
  name         = "${var.project_name}-client"
  user_pool_id = aws_cognito_user_pool.main.id

  # No client secret — native apps cannot keep secrets secure
  generate_secret = false

  # Prevent user enumeration — always returns generic error messages
  prevent_user_existence_errors = "ENABLED"

  explicit_auth_flows = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
  ]

  # Add Google IDP only after credentials are wired in
  supported_identity_providers = var.google_client_id != "" ? [
    "COGNITO",
    "Google",
  ] : ["COGNITO"]

  # Windows: localhost:7890 (port 80 requires admin on Windows; 7890 does not)
  # Android: app-specific custom URI scheme (not generic "myapp")
  callback_urls = [
    "http://localhost:7890",
    "selimcelemsaaapp://callback",
  ]

  logout_urls = [
    "http://localhost:7890",
    "selimcelemsaaapp://logout",
  ]

  allowed_oauth_flows                  = ["code"]
  allowed_oauth_scopes                 = ["email", "openid", "profile"]
  allowed_oauth_flows_user_pool_client = true

  # Token validity
  access_token_validity  = 1   # hours
  id_token_validity      = 1   # hours
  refresh_token_validity = 30  # days

  token_validity_units {
    access_token  = "hours"
    id_token      = "hours"
    refresh_token = "days"
  }

  depends_on = [aws_cognito_identity_provider.google]
}

# ─── Cognito Identity Pool ─────────────────────────────────────────────────
# Exchanges Cognito ID tokens for temporary, per-user AWS credentials.
# This is the only way authenticated mobile users get S3 access.

resource "aws_cognito_identity_pool" "main" {
  identity_pool_name               = "${var.project_name}-identity-${local.suffix}"
  allow_unauthenticated_identities = false

  cognito_identity_providers {
    client_id               = aws_cognito_user_pool_client.main.id
    provider_name           = aws_cognito_user_pool.main.endpoint
    server_side_token_check = true
  }

  tags = local.tags
}

# ─── IAM Role — Authenticated Cognito users ──────────────────────────────────

resource "aws_iam_role" "cognito_authenticated" {
  name = "${var.project_name}-auth-role-${local.suffix}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          Federated = "cognito-identity.amazonaws.com"
        }
        Action = "sts:AssumeRoleWithWebIdentity"
        Condition = {
          StringEquals = {
            "cognito-identity.amazonaws.com:aud" = aws_cognito_identity_pool.main.id
          }
          "ForAnyValue:StringLike" = {
            "cognito-identity.amazonaws.com:amr" = "authenticated"
          }
        }
      }
    ]
  })

  tags = local.tags
}

# ─── Per-user S3 access (scoped to scores/{identity-id}/*) ──────────────────
# Each user can ONLY read/write their own scores folder.
# The ${cognito-identity.amazonaws.com:sub} variable resolves to the
# Cognito Identity Pool identity ID at request time.

resource "aws_iam_role_policy" "cognito_s3_per_user" {
  name = "${var.project_name}-s3-per-user"
  role = aws_iam_role.cognito_authenticated.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowPerUserReadWrite"
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
        ]
        Resource = "${aws_s3_bucket.main.arn}/scores/$${cognito-identity.amazonaws.com:sub}/*"
      },
      {
        Sid      = "AllowListBucketScoped"
        Effect   = "Allow"
        Action   = ["s3:ListBucket"]
        Resource = aws_s3_bucket.main.arn
        Condition = {
          StringLike = {
            "s3:prefix" = "scores/$${cognito-identity.amazonaws.com:sub}/*"
          }
        }
      },
      {
        Sid      = "AllowReadQuestions"
        Effect   = "Allow"
        Action   = ["s3:GetObject"]
        Resource = "${aws_s3_bucket.main.arn}/questions.json"
      },
    ]
  })
}

# ─── Attach role to Identity Pool ────────────────────────────────────────────

resource "aws_cognito_identity_pool_roles_attachment" "main" {
  identity_pool_id = aws_cognito_identity_pool.main.id
  roles = {
    authenticated = aws_iam_role.cognito_authenticated.arn
  }
}
