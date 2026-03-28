import json
import os
import boto3
from datetime import datetime, timezone

s3 = boto3.client("s3")
sns = boto3.client("sns")

BUCKET = os.environ["S3_BUCKET_NAME"]
TOPIC_ARN = os.environ["SNS_TOPIC_ARN"]
LAST_RUN_KEY = "reports/_last_digest.txt"


def lambda_handler(event, context):
    # Get last run timestamp
    last_run = datetime.min.replace(tzinfo=timezone.utc)
    try:
        resp = s3.get_object(Bucket=BUCKET, Key=LAST_RUN_KEY)
        last_run_str = resp["Body"].read().decode("utf-8").strip()
        last_run = datetime.fromisoformat(last_run_str)
    except s3.exceptions.NoSuchKey:
        pass
    except Exception:
        pass  # First run

    now = datetime.now(timezone.utc)

    # List report files
    paginator = s3.get_paginator("list_objects_v2")
    pages = paginator.paginate(Bucket=BUCKET, Prefix="reports/")

    summaries = []
    for page in pages:
        for obj in page.get("Contents", []):
            key = obj["Key"]
            if key.endswith("_last_digest.txt") or not key.endswith(".json"):
                continue

            last_modified = obj["LastModified"]
            if last_modified <= last_run:
                continue

            # Read report file
            try:
                resp = s3.get_object(Bucket=BUCKET, Key=key)
                reports = json.loads(resp["Body"].read().decode("utf-8"))
            except Exception:
                continue

            # Filter for new reports since last run
            new_reports = []
            for r in reports:
                reported_at = datetime.fromisoformat(r.get("reportedAt", "").replace("Z", "+00:00"))
                if reported_at > last_run:
                    new_reports.append(r)

            if new_reports:
                question_id = key.replace("reports/", "").replace(".json", "")
                latest_comment = new_reports[-1].get("comment", "")
                summaries.append({
                    "questionId": question_id,
                    "count": len(new_reports),
                    "latestComment": latest_comment,
                })

    if not summaries:
        print("No new reports since last run.")
        # Still update timestamp
        s3.put_object(Bucket=BUCKET, Key=LAST_RUN_KEY, Body=now.isoformat())
        return {"statusCode": 200, "body": "No new reports"}

    # Compose email
    lines = [f"Question Report Digest — {now.strftime('%Y-%m-%d %H:%M UTC')}", ""]
    lines.append(f"New reports since {last_run.strftime('%Y-%m-%d %H:%M UTC')}:")
    lines.append("")
    for s_item in sorted(summaries, key=lambda x: x["count"], reverse=True):
        lines.append(f"  {s_item['questionId']}: {s_item['count']} report(s)")
        if s_item["latestComment"]:
            lines.append(f"    Latest comment: {s_item['latestComment']}")
    lines.append("")
    lines.append(f"Total: {sum(s_item['count'] for s_item in summaries)} new report(s) across {len(summaries)} question(s)")

    message = "\n".join(lines)
    sns.publish(
        TopicArn=TOPIC_ARN,
        Subject=f"SAA-C03 Question Reports — {len(summaries)} question(s)",
        Message=message,
    )

    # Update last run timestamp
    s3.put_object(Bucket=BUCKET, Key=LAST_RUN_KEY, Body=now.isoformat())

    return {"statusCode": 200, "body": f"Sent digest with {len(summaries)} question(s)"}
