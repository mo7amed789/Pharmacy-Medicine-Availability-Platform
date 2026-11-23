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

public class CrossProjectTrialTests
{
    [Fact]
    public async Task PythonDesktopSidecarToRealHttpPipelineAndSqlServerToPublicFinder()
    {
        var appSource=Environment.GetEnvironmentVariable("PHARMACY_APP_SOURCE")
            ?? throw new InvalidOperationException("PHARMACY_APP_SOURCE must point to the checked-out app source");
        var script=Environment.GetEnvironmentVariable("PUBLISHER_TRIAL_SCRIPT")
            ?? throw new InvalidOperationException("PUBLISHER_TRIAL_SCRIPT must point to the synthetic trial driver");
        var connection=Environment.GetEnvironmentVariable("INVENTORY_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("Isolated SQL test connection required");
        var options=new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer(connection).Options;
        var key="synthetic-cross-project-key-not-a-real-grant";
        Guid pharmacy,branch,medicine;
        await using(var db=new PharmacyDbContext(options))
        {
            await db.Database.MigrateAsync();
            var c=new Category("CrossTrial_"+Guid.NewGuid());var m=new Manufacturer("CrossTrial_"+Guid.NewGuid());
            db.AddRange(c,m);await db.SaveChangesAsync();
            var med=new Medicine("SYNTHETIC trial medicine","Synthetic","Synthetic","10mg","Tablet",c.Id,m.Id);
            var p=new Pharmacy("SYNTHETIC trial pharmacy","FAKE_"+Guid.NewGuid(),"Synthetic address","Test",new(30,31),"0000000000");
            var b=p.AddBranch("SYNTHETIC branch","Synthetic address","Test",new(30,31),"0000000000");
            db.AddRange(med,p);await db.SaveChangesAsync();pharmacy=p.Id;branch=b.Id;medicine=med.Id;
        }
        var temp=Path.Combine(Path.GetTempPath(),"pharmacy-synthetic-"+Guid.NewGuid());Directory.CreateDirectory(temp);
        var start=new ProcessStartInfo("python") { RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false };
        foreach(var arg in new[]{script,appSource,temp,branch.ToString(),medicine.ToString()})start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;
        var stderr=process.StandardError.ReadToEndAsync();
        WebApplicationFactory<Program>? factory=null;HttpClient? client=null;int requests=0;bool done=false;
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            while(await process.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
            {
                using var message=JsonDocument.Parse(line);var root=message.RootElement;
                if(root.GetProperty("kind").GetString()=="done"){done=true;break;}
                var manifest=root.GetProperty("manifest");
                if(factory==null)
                {
                    var install=manifest.GetProperty("installationId").GetString()!;
                    await using(var db=new PharmacyDbContext(options))
                    {
                        var binding=manifest.GetProperty("bindings")[0];
                        db.Add(new InventoryPublisherBinding { BranchId=branch,InstallationId=Guid.Parse(install),MedicineId=medicine,
                            ExternalProductId=binding.GetProperty("externalProductId").GetString()!,IdentityFingerprint=binding.GetProperty("identityFingerprint").GetString()!,
                            UnitsPerPack=binding.GetProperty("unitsPerPack").GetInt32(),Verified=true });await db.SaveChangesAsync();
                    }
                    factory=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{
                        builder.UseEnvironment("Testing");
                        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> {
                            ["ConnectionStrings:DefaultConnection"]=connection,["Jwt:Secret"]=new string('s',64),
                            ["FeatureFlags:EnableInventoryIntegration"]="true",["FeatureFlags:EnableReservations"]="false",
                            ["InventoryIntegration:Publishers:0:PharmacyId"]=pharmacy.ToString(),["InventoryIntegration:Publishers:0:BranchId"]=branch.ToString(),
                            ["InventoryIntegration:Publishers:0:InstallationId"]=install,
                            ["InventoryIntegration:Publishers:0:KeySha256"]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))) }));
                    });
                    client=factory.CreateClient(new WebApplicationFactoryClientOptions {BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
                    var denied=await client.PostAsJsonAsync("/api/v1/integration/inventory-snapshots",root.GetProperty("payload"));
                    Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode);
                }
                using var request=new HttpRequestMessage(HttpMethod.Post,"/api/v1/integration/inventory-snapshots");
                request.Headers.Add("X-Inventory-Publisher-Key",key);request.Content=JsonContent.Create(root.GetProperty("payload"));
                var response=await client!.SendAsync(request,timeout.Token);var body=await response.Content.ReadAsStringAsync(timeout.Token);
                Assert.True(response.IsSuccessStatusCode,$"Receiver returned {response.StatusCode}: {body}");
                requests++;
                var publicResult=await client.GetFromJsonAsync<JsonElement>($"/api/v1/availability?medicineId={medicine}&latitude=30&longitude=31&radiusKm=2",timeout.Token);
                var items=publicResult.GetProperty("items");Assert.Equal(1,items.GetArrayLength());
                var item=items[0];Assert.Equal(requests==3?"OutOfStock":"LowStock",item.GetProperty("stockStatus").GetString());
                Assert.Equal(requests==3?0:2,item.GetProperty("availableQuantity").GetInt32());
                Assert.Equal("box",item.GetProperty("saleUnit").GetString());Assert.Equal(0,item.GetProperty("distanceKm").GetDouble(),6);
                await process.StandardInput.WriteLineAsync("{\"status\":200,\"ack\":"+body+"}");await process.StandardInput.FlushAsync();
            }
            await process.WaitForExitAsync(timeout.Token);Assert.True(done,await stderr);Assert.Equal(0,process.ExitCode);Assert.Equal(3,requests);
            Console.WriteLine("Synthetic desktop -> real HTTP pipeline -> SQL Server -> public finder: PASS. Fake boxes=2, replay stable, sold-out=0, nearest distance=0, no anonymous write.");
        }
        finally {if(!process.HasExited)process.Kill(true);client?.Dispose();if(factory!=null)await factory.DisposeAsync();Directory.Delete(temp,true);}
    }
}
