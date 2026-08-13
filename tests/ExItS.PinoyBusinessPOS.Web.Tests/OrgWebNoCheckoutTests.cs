namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebNoCheckoutTests
{
    [Fact]
    public void Organization_web_pages_do_not_implement_checkout_or_cart()
    {
        var root = FindWebRoot();
        var files = Directory.GetFiles(root, "*.razor", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(files);

        var forbidden = new[]
        {
            "CheckoutAsync",
            "VoidSaleAsync",
            "CreateSale",
            "/checkout",
            "New Sale",
            "Take Payment",
            "barcode selling",
            "AddToCart",
            "ShoppingCart"
        };

        var hits = new List<string>();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var token in forbidden)
            {
                if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    hits.Add($"{Path.GetRelativePath(root, file)}: {token}");
                }
            }
        }

        Assert.True(hits.Count == 0, "Organization Web must not implement POS checkout. Hits: " + string.Join("; ", hits));
        Assert.Contains(files, f => f.EndsWith("SalesHistory.razor", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(files, f => f.EndsWith("Login.razor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sales_history_is_explicitly_read_only()
    {
        var page = File.ReadAllText(Path.Combine(FindWebRoot(), "Components", "Pages", "Reports", "SalesHistory.razor"));
        Assert.Contains("Read-only", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no web checkout", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CheckoutAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/checkout\"", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_states_web_is_not_a_pos_client()
    {
        var login = File.ReadAllText(Path.Combine(FindWebRoot(), "Components", "Pages", "Login.razor"));
        Assert.Contains("Boundary_NotPos", login, StringComparison.Ordinal);
    }

    private static string FindWebRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Web");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Organization Web project was not found.");
    }
}
