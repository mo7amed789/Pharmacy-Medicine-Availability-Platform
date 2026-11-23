using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
namespace PharmacyAvailability.IntegrationTests;
public class SecurityRateLimitTests
{
 [Fact] public async Task EndpointPoliciesAreRegisteredAndAppliedAfterRouting()
 {
  await using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>{
   b.UseEnvironment("Testing");b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"RateLimiting:AuthEndpointLimit","2"}}));
  });
  using var client=factory.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
  for(int i=0;i<100;i++){var r=await client.PostAsJsonAsync("/api/v1/auth/login",new{});Assert.NotEqual(HttpStatusCode.InternalServerError,r.StatusCode);}
  Assert.Equal(HttpStatusCode.TooManyRequests,(await client.PostAsJsonAsync("/api/v1/auth/login",new{})).StatusCode);
  for(int i=0;i<10;i++){var r=await client.PostAsJsonAsync("/api/v1/pharmacies/register",new{});Assert.NotEqual(HttpStatusCode.InternalServerError,r.StatusCode);}
  Assert.Equal(HttpStatusCode.TooManyRequests,(await client.PostAsJsonAsync("/api/v1/pharmacies/register",new{})).StatusCode);
 }
}
