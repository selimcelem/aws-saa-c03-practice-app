# These outputs are non-sensitive and can be committed to git as infra/outputs.json
# Run after apply: terraform output -json > ../infra/outputs.json

output "cognito_user_pool_id" {
  description = "Cognito User Pool ID — goes into COGNITO_USER_POOL_ID in .env"
  value       = aws_cognito_user_pool.main.id
}

output "cognito_client_id" {
  description = "Cognito App Client ID — goes into COGNITO_CLIENT_ID in .env"
  value       = aws_cognito_user_pool_client.main.id
}

output "cognito_domain" {
  description = "Cognito Hosted UI domain — goes into COGNITO_DOMAIN in .env and is the Google OAuth redirect URI base"
  value       = "${aws_cognito_user_pool_domain.main.domain}.auth.${var.region}.amazoncognito.com"
}

output "cognito_hosted_ui_url" {
  description = "Full Google OAuth redirect URI to add to Google Cloud Console"
  value       = "https://${aws_cognito_user_pool_domain.main.domain}.auth.${var.region}.amazoncognito.com/oauth2/idpresponse"
}

output "s3_bucket_name" {
  description = "S3 bucket name — goes into S3_BUCKET_NAME in .env"
  value       = aws_s3_bucket.main.bucket
}

output "region" {
  description = "AWS region — goes into AWS_REGION in .env"
  value       = var.region
}

output "cognito_identity_pool_id" {
  description = "Cognito Identity Pool ID — goes into CognitoIdentityPoolId in appsettings.json"
  value       = aws_cognito_identity_pool.main.id
}

output "google_idp_configured" {
  description = "Whether the Google identity provider has been wired in"
  value       = length(aws_cognito_identity_provider.google) > 0
}

output "sns_topic_arn" {
  description = "SNS topic ARN for question report digest emails"
  value       = aws_sns_topic.report_digest.arn
}

output "lambda_function_name" {
  description = "Lambda function name for report digest"
  value       = aws_lambda_function.report_digest.function_name
}
