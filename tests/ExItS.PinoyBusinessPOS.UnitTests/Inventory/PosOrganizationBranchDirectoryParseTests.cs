using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class PosOrganizationBranchDirectoryParseTests
{
    [Fact]
    public void ParseBranchList_reads_id_name_and_ignores_unknown_payload_shape()
    {
        const string json = """
            [
              {
                "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                "organizationId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                "code": "MAIN",
                "name": "Main Branch",
                "isPrimary": true,
                "status": "Active",
                "branchType": "Retail",
                "deliveryPolicy": { "unexpected": true, "nested": [1,2,3] },
                "createdAtUtc": "2026-09-05T00:00:00Z"
              },
              {
                "Id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
                "Name": "Iloilo Branch",
                "Code": "ILO",
                "Status": 1,
                "BranchType": "Warehouse",
                "AreaId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
                "AreaName": "Visayas"
              }
            ]
            """;

        using var document = JsonDocument.Parse(json);
        var branches = PosOrganizationBranchDirectory.ParseBranchList(document.RootElement);

        Assert.Equal(2, branches.Count);
        Assert.Equal(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), branches[0].Id);
        Assert.Equal("Main Branch", branches[0].Name);
        Assert.Equal("Retail", branches[0].BranchType);
        Assert.Equal(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), branches[1].Id);
        Assert.Equal("Iloilo Branch", branches[1].Name);
        Assert.Equal("1", branches[1].Status);
        Assert.Equal("Warehouse", branches[1].BranchType);
        Assert.Equal(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), branches[1].AreaId);
    }
}
