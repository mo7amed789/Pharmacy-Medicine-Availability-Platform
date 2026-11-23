using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Contracts.Pharmacies;
using PharmacyAvailability.Contracts.Reservations;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Domain.Reservations;
using PharmacyAvailability.Infrastructure.Persistence;
using PharmacyAvailability.Infrastructure.Security;

namespace PharmacyAvailability.IntegrationTests;

public class StaffBranchScopeTests
{
 [Fact]
 public async Task LiveAssignmentGuardsEveryPrivateBranchRouteAndReservations()
 {
  var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("INVENTORY_TEST_SQLSERVER") ?? throw new InvalidOperationException("Isolated SQL required")) { InitialCatalog = "StaffScope_" + Guid.NewGuid().ToString("N") }.ConnectionString;
  const string secret = "SyntheticOnly_StaffScope_NotARealSecret_12345678901234567890";
  await using var db = new PharmacyDbContext(new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer(connection).Options);
  await db.Database.MigrateAsync();
  try {
   var pharmacy = new Pharmacy("Synthetic", "SYN_"+Guid.NewGuid(), "Test", "Test", new(30,31), "000"); pharmacy.Approve();
   var a = pharmacy.AddBranch("A", "Test", "Test", new(30,31), "000");
   var b = pharmacy.AddBranch("B", "Test", "Test", new(30,31), "000");
   var other = new Pharmacy("Other", "SYN_"+Guid.NewGuid(), "Test", "Test", new(30,31), "000");
   var foreign = other.AddBranch("Foreign", "Test", "Test", new(30,31), "000");
   var staff = new User("staff@example.invalid", "fake", "Synthetic", UserRole.PharmacyStaff, "000", pharmacy.Id);
   var admin = new User("admin@example.invalid", "fake", "Synthetic", UserRole.PharmacyAdmin, "000", pharmacy.Id);
   var customer = new User("customer@example.invalid", "fake", "Synthetic", UserRole.Customer, "000");
   var category = new Category("Synthetic"); var manufacturer = new Manufacturer("Synthetic");
   var medicine = new Medicine("Synthetic", "Synthetic", "Synthetic", "1", "Tablet", category.Id, manufacturer.Id);
   db.AddRange(pharmacy,other,staff,admin,customer,category,manufacturer,medicine); await db.SaveChangesAsync();
   var ra = new Reservation(customer.Id, pharmacy.Id, a.Id, medicine.Id, 1, DateTime.UtcNow.AddMinutes(30));
   var rb = new Reservation(customer.Id, pharmacy.Id, b.Id, medicine.Id, 1, DateTime.UtcNow.AddMinutes(30));
   db.AddRange(ra,rb); await db.SaveChangesAsync();
   await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(x => {
    x.UseEnvironment("Testing");x.UseSetting("ConnectionStrings:DefaultConnection",connection);x.UseSetting("Jwt:Secret",secret);
    x.UseSetting("Jwt:Issuer","StaffTest");x.UseSetting("Jwt:Audience","StaffTest");
    x.UseSetting("FeatureFlags:EnableManagementApis","true");x.UseSetting("FeatureFlags:EnableReservations","true");
   });
   using var client = factory.CreateClient(new() {BaseAddress=new Uri("https://localhost")});
   var tokens = new JwtTokenService(Options.Create(new JwtOptions {Secret=secret,Issuer="StaffTest",Audience="StaffTest",AccessTokenLifetimeMinutes=15}));
   var staffToken = tokens.GenerateAccessToken(staff).Token;
   void Login(User u) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GenerateAccessToken(u).Token);
   string Route(Guid branch,string suffix) => $"/api/v1/pharmacies/{pharmacy.Id}/branches/{branch}/{suffix}";
   Login(staff);
   Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(Route(a.Id,"inventory"))).StatusCode);
   Login(admin);
   Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync($"/api/v1/pharmacies/{pharmacy.Id}/staff/{staff.Id}/branch",new AssignStaffBranchRequest(foreign.Id))).StatusCode);
   Assert.Equal(HttpStatusCode.OK,(await client.PutAsJsonAsync($"/api/v1/pharmacies/{pharmacy.Id}/staff/{staff.Id}/branch",new AssignStaffBranchRequest(a.Id))).StatusCode);
   client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",staffToken);
   Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(Route(a.Id,"inventory"))).StatusCode);
   foreach(var suffix in new[]{"inventory","operations/dashboard","operations/reservations/queue","operations/inventory/low-stock","operations/activity"})
    Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(Route(b.Id,suffix))).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(Route(b.Id,"inventory"),new {medicineId=medicine.Id,quantity=1})).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await client.PatchAsJsonAsync($"/api/v1/pharmacies/{pharmacy.Id}/branches/{b.Id}/status",new {isActive=false})).StatusCode);
   Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/v1/reservations/{rb.Id}")).StatusCode);
   Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsync($"/api/v1/reservations/{rb.Id}/confirm",null)).StatusCode);
   await db.Entry(rb).ReloadAsync(); Assert.Equal(ReservationStatus.Active, rb.Status);
   var list=await client.GetFromJsonAsync<PharmacyAvailability.Contracts.Catalog.PagedResult<ReservationDto>>("/api/v1/reservations");
   Assert.Single(list!.Items);Assert.Equal(a.Id,list.Items[0].BranchId);
   Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/v1/pharmacies/{pharmacy.Id}/staff/{staff.Id}/branch",new AssignStaffBranchRequest(b.Id))).StatusCode);
   Login(admin);
   Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(Route(b.Id,"inventory"))).StatusCode);
   await client.PutAsJsonAsync($"/api/v1/pharmacies/{pharmacy.Id}/staff/{staff.Id}/branch",new AssignStaffBranchRequest(b.Id));
   client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",staffToken);
   Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(Route(a.Id,"inventory"))).StatusCode);
   Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(Route(b.Id,"inventory"))).StatusCode);
  } finally { await db.Database.EnsureDeletedAsync(); }
 }
}
