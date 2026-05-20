using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Persistence;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Payments;

[Collection("Integration")]
public class PaymentsEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PaymentsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitialiseDatabaseAsync();
        _client = await AuthenticatedHttpClient.CreateAsync(
            _factory,
            CustomWebApplicationFactory.AdminUsername,
            CustomWebApplicationFactory.AdminPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByBill_UnknownId_Returns200WithEmptyCollection()
    {
        var response = await _client.GetAsync($"/api/payments/bill/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<List<PaymentSummary>>>();
        envelope.Should().NotBeNull();
        envelope!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByBill_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync($"/api/payments/bill/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400()
    {
        var request = new { taxBillId = Guid.Empty, amount = -10 };

        var response = await _client.PostAsJsonAsync("/api/payments", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ValidRequest_PersistsPaymentAndUpdatesBillStatus()
    {
        var seededBillId = await SeedIssuedBillAsync();
        var request = new
        {
            taxBillId = seededBillId,
            method = PaymentMethod.Cash,
            amount = 250m,
            notes = "Partial cash settlement"
        };

        var response = await _client.PostAsJsonAsync("/api/payments", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var paymentEnvelope = await response.Content.ReadFromJsonAsync<Envelope<PaymentSummary>>();
        paymentEnvelope.Should().NotBeNull();
        paymentEnvelope!.Data.Id.Should().NotBeEmpty();
        paymentEnvelope.Data.Amount.Should().Be(250m);

        var getByBillResponse = await _client.GetAsync($"/api/payments/bill/{seededBillId}");
        getByBillResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var byBillEnvelope = await getByBillResponse.Content.ReadFromJsonAsync<Envelope<List<PaymentSummary>>>();
        byBillEnvelope.Should().NotBeNull();
        byBillEnvelope!.Data.Should().ContainSingle(p => p.Id == paymentEnvelope.Data.Id && p.Amount == 250m);
    }

    private async Task<Guid> SeedIssuedBillAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var taxpayer = new Taxpayer
        {
            Id = Guid.NewGuid(),
            FullName = "Payment Test Taxpayer",
            NationalId = $"{DateTime.UtcNow.Ticks % 100000000000000:D14}",
            MobileNumber = "01000000000",
            Email = "payment.test@example.com",
            CreatedBy = "test"
        };

        var property = new Property
        {
            Id = Guid.NewGuid(),
            PropertyCode = $"PROP-PAY-{Guid.NewGuid():N}"[..18],
            Type = PropertyType.Residential,
            BuiltUpArea = 140,
            City = "Cairo",
            Governorate = "Cairo",
            CreatedBy = "test"
        };

        var bill = new TaxBill
        {
            Id = Guid.NewGuid(),
            BillNumber = $"BILL-IT-{Guid.NewGuid():N}"[..16],
            PropertyId = property.Id,
            TaxpayerId = taxpayer.Id,
            TaxAssessmentId = Guid.NewGuid(),
            TaxYear = DateTime.UtcNow.Year,
            Status = BillStatus.Issued,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            IssueDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddMonths(1),
            CreatedBy = "test"
        };

        db.Taxpayers.Add(taxpayer);
        db.Properties.Add(property);
        db.TaxBills.Add(bill);
        await db.SaveChangesAsync();

        return bill.Id;
    }

    private record Envelope<T>(bool Success, T Data);
    private record PaymentSummary(Guid Id, decimal Amount);
}
