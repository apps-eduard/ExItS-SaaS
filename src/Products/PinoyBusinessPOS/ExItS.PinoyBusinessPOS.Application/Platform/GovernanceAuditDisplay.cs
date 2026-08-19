namespace ExItS.PinoyBusinessPOS.Application.Platform;

public static class GovernanceAuditDisplay
{
    public static string FormatMobileRow(PlatformGovernanceAuditRecordDto record)
    {
        var text = string.IsNullOrWhiteSpace(record.Summary)
            ? DescribeAction(record.ActionCode)
            : record.Summary.Trim();
        return $"{text} · {FormatLocalTime(record.OccurredAtUtc)}";
    }

    public static string FormatWebAction(string actionCode) => DescribeAction(actionCode);

    public static string FormatLocalTime(DateTimeOffset utc)
    {
        var local = utc.ToLocalTime();
        return local.ToString("h:mm tt");
    }

    public static string FormatWebTimestamp(DateTimeOffset utc) =>
        utc.ToLocalTime().ToString("g");

    private static string DescribeAction(string actionCode) =>
        actionCode switch
        {
            "platform.organization.updated" => "Organization profile updated",
            "platform.organization.branding_updated" => "Branding updated",
            "platform.organization.branch.created" => "Branch created",
            "platform.organization.branch.updated" => "Branch updated",
            "platform.organization.branch.archived" => "Branch archived",
            "platform.organization.branch.reactivated" => "Branch reactivated",
            "platform.organization.branch.hours_updated" => "Branch hours updated",
            "platform.organization.branch.fulfillment_updated" => "Fulfillment settings updated",
            "platform.organization.branch.delivery_policy_updated" => "Delivery policy updated",
            "platform.organization.branch.orders_paused" => "Online orders pause changed",
            "platform.membership.branch_assignments_updated" => "Staff branch assignments updated",
            "platform.membership.added" => "Staff member added",
            "platform.membership.role_changed" => "Staff role changed",
            "platform.membership.suspended" => "Staff suspended",
            "platform.membership.reactivated" => "Staff reactivated",
            "platform.membership.revoked" => "Staff removed",
            "platform.invitation.created" => "Staff invitation sent",
            "platform.invitation.revoked" => "Staff invitation revoked",
            "platform.pos_device.registered" => "Device registered",
            "platform.pos_device.revoked" => "Device revoked",
            "platform.pos_device.renamed" => "Device renamed",
            _ => actionCode
        };
}
