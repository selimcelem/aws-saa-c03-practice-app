# Privacy Policy

**AWS SAA-C03 Practice App**
Last updated: 2026-03-26

---

## What data we collect

- **Email address** — used for authentication via Google Sign-In (processed by AWS Cognito).
- **Display name** — shown in the app dashboard greeting. Comes from your Google account.
- **Quiz scores and session history** — questions answered, correct/incorrect, time spent per question.

We do **not** collect:
- Location data
- Contacts
- Device identifiers or advertising IDs
- Analytics or telemetry beyond what is described above

## Where data is stored

- **Quiz session history** is stored locally on your device in a SQLite database.
- **Score backups** are synced to **Amazon Web Services (AWS) S3** in the **eu-west-1 (Ireland)** region, encrypted in transit via HTTPS. Each user's data is stored in an isolated folder that only their authenticated credentials can access.
- **Authentication tokens** are stored in the platform's secure storage (Android Keystore / Windows Credential Manager).

## How data is used

Your data is used solely to:
1. Authenticate you and restore your sessions across devices.
2. Display your quiz performance and study progress.
3. Back up your session history so it is not lost if you reinstall the app.

## Data sharing

Your data is **never sold, shared with third parties, or used for advertising**.

The only third-party services that process your data are:
- **AWS Cognito** — authentication (email, name)
- **AWS S3** — score backup storage
- **Google Sign-In** — identity verification

## Data retention

- Your session data persists as long as your account exists.
- You can delete your local data by clearing the app's storage or uninstalling the app.
- To request deletion of your cloud-stored data, contact us at the email below.

## Children's privacy

This app is not directed at children under the age of 13. We do not knowingly collect personal information from children.

## Changes to this policy

We may update this policy from time to time. Changes will be reflected in the "Last updated" date above.

## Contact

For questions about this privacy policy or to request data deletion:

**Email:** [your-email@example.com]

---

*This privacy policy is required by the Google Play Store for apps that handle user data.*
