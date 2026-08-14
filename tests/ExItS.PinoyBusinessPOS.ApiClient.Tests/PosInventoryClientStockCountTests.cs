using System.Net;
using System.Text;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.ApiClient;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

public sealed class PosInventoryClientStockCountTests
{
    private const string CountPayload =
        """
        {
          "stockCountId": "11111111-1111-1111-1111-111111111111",
          "organizationId": "22222222-2222-2222-2222-222222222222",
          "countNumber": "CNT-20260814-01",
          "title": "Weekly count",
          "status": "Draft",
          "countDate": "2026-08-14",
          "notes": "Counted after Friday closing.",
          "startedAtUtc": null,
          "startedBy": null,
          "completedAtUtc": null,
          "completedBy": null,
          "cancelledAtUtc": null,
          "cancelledBy": null,
          "createdAtUtc": "2026-08-14T04:10:00Z",
          "updatedAtUtc": "2026-08-14T04:10:00Z",
          "lines": [
            {
              "lineId": "33333333-3333-3333-3333-333333333333",
              "productId": "44444444-4444-4444-4444-444444444444",
              "productName": "Bottled Water 500ml",
              "unitOfMeasure": "Piece",
              "lineNumber": 1,
              "systemOnHandSnapshot": null,
              "countedQuantity": null,
              "variance": null
            }
          ]
        }
        """;

    [Fact]
    public async Task Create_stock_count_posts_title_and_notes_separately()
    {
        string? body = null;
        var handler = new StubHandler(async request =>
        {
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CountPayload, Encoding.UTF8, "application/json")
            };
        });
        var client = new PosInventoryClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });
        var productId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var result = await client.CreateStockCountAsync(
            new CreateStockCountRequest(
                [new CreateStockCountLineRequest(productId)],
                "Weekly count",
                Notes: "Counted after Friday closing."));

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/pos/inventory/stock-counts", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("\"title\":\"Weekly count\"", body, StringComparison.Ordinal);
        Assert.Contains("\"notes\":\"Counted after Friday closing.\"", body, StringComparison.Ordinal);
        Assert.Equal("Weekly count", result.Data!.Title);
        Assert.Equal("Counted after Friday closing.", result.Data.Notes);
        Assert.Equal("CNT-20260814-01", result.Data.CountNumber);
        Assert.Equal(TimeSpan.Zero, result.Data.CreatedAtUtc.Offset);
    }

    [Fact]
    public async Task Get_stock_count_deserializes_title_for_list_and_detail()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CountPayload, Encoding.UTF8, "application/json")
        }));
        var client = new PosInventoryClient(new HttpClient(handler) { BaseAddress = new Uri("http://pos.test") });

        var result = await client.GetStockCountAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Weekly count", result.Data!.Title);
        Assert.Equal("Draft", result.Data.Status);
        Assert.Equal("Counted after Friday closing.", result.Data.Notes);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return await respond(request);
        }
    }
}
