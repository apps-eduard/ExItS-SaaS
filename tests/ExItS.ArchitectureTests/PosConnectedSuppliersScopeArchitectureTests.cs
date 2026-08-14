namespace ExItS.ArchitectureTests;

public sealed class PosConnectedSuppliersScopeArchitectureTests
{
    [Fact]
    public void Connected_supplier_slice_has_no_redis_marketplace_or_image_contracts()
    {
        var roots=new[]{
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"),"ConnectedSuppliers"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"),"ConnectedSuppliers"),
            Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Api"),"ConnectedSuppliers")};
        var text=string.Join('\n',roots.SelectMany(x=>Directory.EnumerateFiles(x,"*.cs",SearchOption.AllDirectories)).Select(File.ReadAllText));
        Assert.DoesNotContain("Redis",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Marketplace",text,StringComparison.OrdinalIgnoreCase);

        var contracts=File.ReadAllText(Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Application"),"ConnectedSuppliers","ConnectedSupplierUseCases.cs"));
        Assert.DoesNotContain("ImageUrl",contracts,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ImageData",contracts,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Thumbnail",contracts,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Incoming_order_accept_does_not_reference_inventory()
    {
        var domain=File.ReadAllText(Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"),"ConnectedSuppliers","ConnectedSuppliers.cs"));
        var acceptStart=domain.IndexOf("public void Accept(",StringComparison.Ordinal);
        var declineStart=domain.IndexOf("public void Decline(",acceptStart,StringComparison.Ordinal);
        Assert.True(acceptStart>=0&&declineStart>acceptStart);
        Assert.DoesNotContain("Inventory",domain[acceptStart..declineStart],StringComparison.OrdinalIgnoreCase);
    }

    private static string PosProject(string name)
    {
        var dir=new DirectoryInfo(AppContext.BaseDirectory);
        while(dir is not null&&!File.Exists(Path.Combine(dir.FullName,"ExItS.slnx")))dir=dir.Parent;
        if(dir is null)throw new InvalidOperationException("Repository root not found.");
        return Path.Combine(dir.FullName,"src","Products","PinoyBusinessPOS",name);
    }
}
