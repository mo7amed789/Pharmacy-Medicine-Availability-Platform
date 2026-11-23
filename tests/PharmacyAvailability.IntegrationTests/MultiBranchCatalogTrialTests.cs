using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Integration;
using PharmacyAvailability.Domain.Pharmacies;
using PharmacyAvailability.Infrastructure.Persistence;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

[Collection("CrossProjectDatabase")]
public class MultiBranchCatalogTrialTests
{
 [Fact] public async Task ExistingCatalogWithThreeFakeBranchesAndFiveHundredItemBoundary()
 {
  var source=Environment.GetEnvironmentVariable("PHARMACY_APP_SOURCE")!;
  var directory=Path.GetDirectoryName(Environment.GetEnvironmentVariable("PUBLISHER_TRIAL_SCRIPT"))!;
  var fixture=Environment.GetEnvironmentVariable("CATALOG_TRIAL_FIXTURE");
  bool generated=string.IsNullOrEmpty(fixture);
  if(generated){fixture=Path.Combine(Path.GetTempPath(),"invented-products-"+Guid.NewGuid()+".json");var gen=new ProcessStartInfo("python"){UseShellExecute=false};foreach(var a in new[]{Path.Combine(directory,"multibranch_trial.py"),"--generate",fixture})gen.ArgumentList.Add(a);using var g=Process.Start(gen)!;await g.WaitForExitAsync();Assert.Equal(0,g.ExitCode);}
  var rows=JsonDocument.Parse(File.ReadAllText(fixture!)).RootElement;
  var connection=Environment.GetEnvironmentVariable("INVENTORY_TEST_SQLSERVER")!;
  var options=new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer(connection).Options;
  var pharmacies=new List<Guid>();var branches=new List<Guid>();var medicines=new List<Guid>();
  await using(var db=new PharmacyDbContext(options)) {
   await db.Database.MigrateAsync();var cat=new Category("ExistingCatalogTrial_"+Guid.NewGuid());var man=new Manufacturer("ExistingCatalogTrial_"+Guid.NewGuid());db.AddRange(cat,man);await db.SaveChangesAsync();
   foreach(var r in rows.EnumerateArray()) {
    string Text(string name)=>r.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?(string.IsNullOrWhiteSpace(v.GetString())?"unspecified":v.GetString()!):"unspecified";
    var med=new Medicine(Text("trade_en"),"Existing catalog copy","Trial",Text("strength"),Text("form"),cat.Id,man.Id);db.Add(med);medicines.Add(med.Id);
   }
   for(int i=0;i<3;i++) {var p=new Pharmacy("FAKE branch pharmacy "+i,"FAKE_"+Guid.NewGuid(),"Invented test address","Trial",new(30+i*0.01,31),"0000000000");var b=p.AddBranch("FAKE branch "+i,"Invented test address","Trial",new(30+i*0.01,31),"0000000000");db.Add(p);pharmacies.Add(p.Id);branches.Add(b.Id);}
   await db.SaveChangesAsync();
  }
  var temp=Path.Combine(Path.GetTempPath(),"existing-catalog-trial-"+Guid.NewGuid());Directory.CreateDirectory(temp);
  File.WriteAllText(Path.Combine(temp,"setup.json"),JsonSerializer.Serialize(new{branches,medicines}));
  var start=new ProcessStartInfo("python"){RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
  foreach(var a in new[]{Path.Combine(directory,"multibranch_trial.py"),source,temp,fixture!,Path.Combine(temp,"setup.json")})start.ArgumentList.Add(a);
  using var process=Process.Start(start)!;var errors=process.StandardError.ReadToEndAsync();using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(5));int accepted=0;bool done=false;
  try {
   while(await process.StandardOutput.ReadLineAsync(timeout.Token)is{}line) {
    using var message=JsonDocument.Parse(line);var root=message.RootElement;if(root.GetProperty("kind").GetString()=="done"){done=true;Console.WriteLine(line);break;}
    if(root.GetProperty("kind").GetString()=="limit_observation"){Console.WriteLine(line);continue;}
    int n=root.GetProperty("branchIndex").GetInt32();var manifest=root.GetProperty("manifest");var install=manifest.GetProperty("installationId").GetString()!;
    await using(var db=new PharmacyDbContext(options)) {
     foreach(var b in manifest.GetProperty("bindings").EnumerateArray())db.Add(new InventoryPublisherBinding{BranchId=branches[n],InstallationId=Guid.Parse(install),MedicineId=Guid.Parse(b.GetProperty("medicineId").GetString()!),ExternalProductId=b.GetProperty("externalProductId").GetString()!,IdentityFingerprint=b.GetProperty("identityFingerprint").GetString()!,UnitsPerPack=1,Verified=true});await db.SaveChangesAsync();
    }
    var key="synthetic-multibranch-key-not-a-real-grant";
    await using var factory=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{
     builder.UseEnvironment("Testing");builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{
      ["FeatureFlags:EnableInventoryIntegration"]="true",["FeatureFlags:EnableReservations"]="false",
      ["InventoryIntegration:Publishers:0:PharmacyId"]=pharmacies[n].ToString(),["InventoryIntegration:Publishers:0:BranchId"]=branches[n].ToString(),["InventoryIntegration:Publishers:0:InstallationId"]=install,["InventoryIntegration:Publishers:0:KeySha256"]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))}));
    });
    using var client=factory.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
    using var request=new HttpRequestMessage(HttpMethod.Post,"/api/v1/integration/inventory-snapshots");request.Headers.Add("X-Inventory-Publisher-Key",key);request.Content=JsonContent.Create(root.GetProperty("payload"));
    var response=await client.SendAsync(request,timeout.Token);var body=await response.Content.ReadAsStringAsync(timeout.Token);Assert.True(response.IsSuccessStatusCode,body);accepted++;
    await process.StandardInput.WriteLineAsync("{\"status\":200,\"ack\":"+body+"}");await process.StandardInput.FlushAsync();
    if(n==2) {
     var result=await client.GetFromJsonAsync<JsonElement>($"/api/v1/availability?medicineId={medicines[0]}&latitude=30.02&longitude=31&radiusKm=5",timeout.Token);
     var items=result.GetProperty("items");Assert.Equal(3,items.GetArrayLength());Assert.Equal(branches[2].ToString(),items[0].GetProperty("branchId").GetString());Assert.Equal(0,items[0].GetProperty("distanceKm").GetDouble(),6);
     Assert.Contains(items.EnumerateArray(),x=>x.GetProperty("branchId").GetString()==branches[0].ToString()&&x.GetProperty("stockStatus").GetString()=="OutOfStock");
     var nearby=await client.GetFromJsonAsync<JsonElement>($"/api/v1/availability?medicineId={medicines[0]}&latitude=30.02&longitude=31&radiusKm=0.5",timeout.Token);Assert.Single(nearby.GetProperty("items").EnumerateArray());
     await using(var db=new PharmacyDbContext(options)){await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE PharmacyInventories SET UpdatedAt=DATEADD(minute,-20,SYSUTCDATETIME()) WHERE BranchId={branches[2]} AND MedicineId={medicines[0]}");}
     var stale=await client.GetFromJsonAsync<JsonElement>($"/api/v1/availability?medicineId={medicines[0]}&latitude=30.02&longitude=31&radiusKm=0.5",timeout.Token);Assert.Equal("Unknown",stale.GetProperty("items")[0].GetProperty("stockStatus").GetString());
     Console.WriteLine("3 fake branches x500 existing-catalog products=1500 accepted rows; nearest branch2 at0km; fake branch0 explicitly out of stock.");
    }
   }
   await process.WaitForExitAsync(timeout.Token);Assert.True(done,await errors);Assert.Equal(0,process.ExitCode);Assert.Equal(3,accepted);
  } finally {if(!process.HasExited)process.Kill(true);Directory.Delete(temp,true);if(generated)File.Delete(fixture!);}
 }
}
