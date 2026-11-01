// SPDX-License-Identifier: MIT
using Scientific;
using Scientific.Tests;
using System.Text.Json;
try{
 object result;int exit=0;
 if(args.Length==2&&args[0]=="--self-test"){var report=TestSuite.Run();result=report;Directory.CreateDirectory(args[1]);File.WriteAllText(Path.Combine(args[1],"test-results.json"),JsonSerializer.Serialize(report,Language.Json));exit=report.Failed==0?0:1;}
 else if(args.Length>=2&&args[0]=="--parse") {string? vars=args.Length==4&&args[2]=="--variables"?File.ReadAllText(args[3]):null;if(args.Length!=2&&vars is null)throw new ArgumentException("Invalid argument combination.");var parsed=Language.Parse(args[1],vars);result=parsed;exit=parsed.Accepted?0:2;}
 else if(args.Length==3&&args[0]=="--compare"){result=Language.Equivalent(Language.Parse(args[1]),Language.Parse(args[2]));}
 else if(args.Length==2&&args[0]=="--fixtures"){
  var fixtures=JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(args[1]),Language.Json)??throw new ArgumentException("No fixtures.");
  if(fixtures.Length>200)throw new ArgumentException("Fixture budget200.");
  var runs=fixtures.Select(f=>{var parsed=Language.Parse(f.Transcript,f.Variables);return new{f.Transcript,passed=parsed.Accepted==f.Accepted&&(f.ErrorCode is null||f.ErrorCode==parsed.ErrorCode),result=parsed};}).ToArray();result=runs;exit=runs.All(r=>r.passed)?0:1;
 }else{Console.WriteLine("Scientific Dictation\n --parse TRANSCRIPT [--variables FILE]\n --compare TRANSCRIPT TRANSCRIPT\n --fixtures FILE\n --self-test NEW_OUTPUT_DIRECTORY\nDeterministic transcript parser. No microphone capture or cross-app insertion.");return args.Length==0?0:64;}
 Console.WriteLine(JsonSerializer.Serialize(result,Language.Json));return exit;
}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}
record Fixture(string Transcript,bool Accepted,string? ErrorCode=null,string? Variables=null);
