using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Infrastructure.Security;
namespace PharmacyAvailability.UnitTests.Security;
public class VerificationLogSafetyTests
{
 private sealed class Capture : ILogger<SmtpVerificationSender>
 {
  public readonly List<string> Lines=new();
  public IDisposable? BeginScope<TState>(TState state)where TState:notnull=>null;
  public bool IsEnabled(LogLevel level)=>true;
  public void Log<TState>(LogLevel level,EventId id,TState state,Exception? ex,Func<TState,Exception?,string> format)=>Lines.Add(format(state,ex));
 }
 [Fact] public async Task MissingProviderNeverLogsCodeOrRecipient()
 {
  var log=new Capture();var sender=new SmtpVerificationSender(Options.Create(new EmailSmtpOptions()),Options.Create(new SmsOptions()),log);
  await sender.SendEmailCodeAsync("synthetic@example.invalid","735219");await sender.SendPhoneCodeAsync("+20000000000","735219");
  Assert.Equal(2,log.Lines.Count);
  foreach(var line in log.Lines){Assert.DoesNotContain("735219",line);Assert.DoesNotContain("synthetic@",line);Assert.DoesNotContain("+2000",line);}
 }
}
