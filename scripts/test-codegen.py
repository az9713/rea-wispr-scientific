"""Compile real parser-generated C# and check independent numeric expectations; no eval/LLM."""
import pathlib,subprocess,json,os
root=pathlib.Path(__file__).resolve().parent.parent
sdk=os.environ['SCIENTIFIC_DOTNET']
cli=root/'src/Scientific.Cli/bin/Release/net9.0/scientific-cli.dll'
cases=[('one over two',.5),('minus two squared',-4),('two power three power two',512),('100 centimeters',1),('v times t',12),('two times open paren x plus three close paren squared',50),('sine open paren pi over two close paren',1),('two meters per second',2),('0.1 plus 0.2',.3)]
folder=root/'.runtime/codegen-smoke';folder.mkdir(parents=True,exist_ok=True)
lines=['using System;','double @x=2.0,@v=3.0,@t=4.0;']
for index,(transcript,expected) in enumerate(cases):
 run=subprocess.run([sdk,str(cli),'--parse',transcript],cwd=root,capture_output=True,text=True,check=True)
 parsed=json.loads(run.stdout)
 assert parsed['accepted']
 lines.append(f'double value{index} = {parsed["code"]};')
 lines.append(f'if(Math.Abs(value{index} - {expected!r}) > 1e-10) throw new Exception("C# magnitude case{index} failed");')
lines.append('Console.WriteLine("Generated C# magnitude checks:9 passed");')
(folder/'Program.cs').write_text('\n'.join(lines),encoding='utf-8')
(folder/'CodegenSmoke.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>',encoding='utf-8')
subprocess.run([sdk,'run','--project',str(folder/'CodegenSmoke.csproj'),'-c','Release'],cwd=root,check=True)
(folder/'result.json').write_text(json.dumps({'passed':9,'failed':0,'kind':'Actual compiled generated C# SI magnitude expressions'}),encoding='utf-8')
