# DISABLED BY DEFAULT — sample systemd-style timer notes for operators.
# Do not enable in Production without approved storage, credentials, and alerting.

# Example daily schedule (cron, disabled):
# 15 2 * * *  /usr/bin/pwsh /opt/exits/ops/backup/Backup-ExItsDatabase.ps1 ...
# 30 2 * * *  /usr/bin/pwsh /opt/exits/ops/backup/Verify-ExItsBackup.ps1 ...
# 0 3 * * 0   /usr/bin/pwsh /opt/exits/ops/backup/Invoke-ExItsRetentionCleanup.ps1 -BackupDirectory /secure/backups

# Environment-owned: credential provisioning, remote storage destination, alert delivery.
