// SPDX-License-Identifier: MIT
using Scientific;
using Scientific.Tests;
using System.Text.Json;
namespace Scientific.App;
internal static class Program
{
 [STAThread] static void Main(string[] args)
 {
  ApplicationConfiguration.Initialize();
  if(args.Length==2&&args[0]=="--self-test"){
   string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
   try{
    var report=TestSuite.Run();File.WriteAllText(Path.Combine(output,"test-results.json"),JsonSerializer.Serialize(report,Language.Json));
    using var form=new Workbench();form.Show();Application.DoEvents();
    form.LoadExample("two times open paren x plus three close paren squared");Application.DoEvents();form.SavePreview(Path.Combine(output,"scientific-workbench.png"));
    if(!form.CurrentAccepted||!form.CopyEnabled||form.VariableLineCount<6)throw new InvalidOperationException("Accepted UI fixture/copy controls/declaration line rendering failed.");
    form.EditWithoutParsing("x plus one");if(form.CurrentAccepted||form.CopyEnabled)throw new InvalidOperationException("Stale preview remained copyable after edit.");
    form.LoadExample("one over x plus one");Application.DoEvents();form.SavePreview(Path.Combine(output,"ambiguity.png"));
    if(form.CurrentAccepted||form.CopyEnabled)throw new InvalidOperationException("Ambiguous UI fixture passed or copy was enabled.");
    File.WriteAllText(Path.Combine(output,"ui-result.json"),JsonSerializer.Serialize(new{passed=report.Failed==0,tests=report.Passed,uiAcceptedFixture=true,uiAmbiguityRejected=true,staleCopyDisabled=true,declarationLinesVisible=true,microphoneInitialized=false,clipboardUsed=false},Language.Json));form.Close();Environment.ExitCode=report.Failed==0?0:1;
   }catch(Exception e){File.WriteAllText(Path.Combine(output,"self-test-error.txt"),e.ToString());Environment.ExitCode=1;}return;
  }
  Application.Run(new Workbench());
 }
}
