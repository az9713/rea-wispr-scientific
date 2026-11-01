// SPDX-License-Identifier: MIT
using Scientific;
using System.Speech.Recognition;
using System.Text.Json;
namespace Scientific.App;

public sealed class Workbench:Form
{
 readonly TextBox transcript=new(){Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical};
 readonly TextBox variables=new(){Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Text=Language.DefaultVariables.Replace("\n",Environment.NewLine)};
 readonly TextBox latex=Output(),code=Output(),prose=Output(),ast=Output(),notes=Output();
 readonly Label status=new(){Dock=DockStyle.Fill,AutoSize=false,Text="Paste a recognition transcript, then inspect the parse."};
 readonly Label type=new(){Dock=DockStyle.Fill,AutoSize=false};
 readonly TextBox comparison=new(){Dock=DockStyle.Fill,PlaceholderText="Second transcript for exact polynomial comparison"};
 readonly Label equivalence=new(){Dock=DockStyle.Fill,AutoSize=false};
 readonly Button mic=new(){Text="Start microphone (Windows SAPI)",AutoSize=true};
 readonly Button stop=new(){Text="Stop microphone",AutoSize=true,Enabled=false};
 readonly Button[] copyButtons=new Button[3];
 SpeechRecognitionEngine? recognition;ParseResult? current;bool closing;
 public bool CurrentAccepted=>current?.Accepted==true;
 public bool CopyEnabled=>copyButtons.Any(b=>b.Enabled);
 public int VariableLineCount=>variables.Lines.Length;
 public void EditWithoutParsing(string value)=>transcript.Text=value;
 static readonly Color Bg=Color.FromArgb(10,20,32),Panel=Color.FromArgb(18,35,51),Ink=Color.FromArgb(229,240,249),Blue=Color.FromArgb(111,207,251),Gold=Color.FromArgb(246,191,109);
 static TextBox Output()=>new(){Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,WordWrap=true};
 public Workbench()
 {
  Text="Scientific Dictation — explicit structure before insertion";Width=1400;Height=920;MinimumSize=new(1050,740);StartPosition=FormStartPosition.CenterScreen;BackColor=Bg;ForeColor=Ink;Font=new Font("Segoe UI",10);
  var outer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(20)};
  outer.RowStyles.Add(new(SizeType.Absolute,68));outer.RowStyles.Add(new(SizeType.Percent,100));outer.RowStyles.Add(new(SizeType.Absolute,52));
  var title=new Label{Text="SCIENTIFIC DICTATION  /  deterministic transcript → typed expression",Font=new Font(Font.FontFamily,18,FontStyle.Bold),Dock=DockStyle.Fill,ForeColor=Blue};outer.Controls.Add(title);
  var split=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};split.ColumnStyles.Add(new(SizeType.Percent,40));split.ColumnStyles.Add(new(SizeType.Percent,60));
  var input=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=9,Padding=new Padding(0,0,18,0)};
  input.RowStyles.Add(new(SizeType.Absolute,26));input.RowStyles.Add(new(SizeType.Percent,45));input.RowStyles.Add(new(SizeType.Absolute,44));input.RowStyles.Add(new(SizeType.Absolute,70));input.RowStyles.Add(new(SizeType.Absolute,28));input.RowStyles.Add(new(SizeType.Percent,55));input.RowStyles.Add(new(SizeType.Absolute,28));input.RowStyles.Add(new(SizeType.Absolute,34));input.RowStyles.Add(new(SizeType.Absolute,65));
  input.Controls.Add(Label("RECOGNITION TRANSCRIPT / editable"));input.Controls.Add(transcript);
  var runbar=new FlowLayoutPanel{Dock=DockStyle.Fill};var parse=Button("Parse and check",()=>Parse());var example=Button("Load example",()=>LoadExample("two times open paren x plus three close paren squared"));runbar.Controls.AddRange([parse,example]);input.Controls.Add(runbar);
  var audio=new FlowLayoutPanel{Dock=DockStyle.Fill};audio.Controls.AddRange([mic,stop,new Label{Text="Optional adapter; ASR untested. Starts only on your click.",AutoSize=true,ForeColor=Gold}]);input.Controls.Add(audio);
  input.Controls.Add(Label("VARIABLE DIMENSIONS / lowercase name: SI unit expression"));input.Controls.Add(variables);
  input.Controls.Add(Label("COMPARE AGAINST / exact rational polynomial scope"));input.Controls.Add(comparison);
  var comparebar=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};comparebar.ColumnStyles.Add(new(SizeType.Absolute,120));comparebar.ColumnStyles.Add(new(SizeType.Percent,100));comparebar.Controls.Add(Button("Compare",Compare));comparebar.Controls.Add(equivalence);input.Controls.Add(comparebar);
  var result=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(12,0,0,0)};result.RowStyles.Add(new(SizeType.Absolute,70));result.RowStyles.Add(new(SizeType.Absolute,36));result.RowStyles.Add(new(SizeType.Percent,73));result.RowStyles.Add(new(SizeType.Percent,27));
  result.Controls.Add(status);result.Controls.Add(type);
  var tabs=new TabControl{Dock=DockStyle.Fill};
  AddTab(tabs,"LaTeX source",latex,0);AddTab(tabs,"C# SI magnitude",code,1);AddTab(tabs,"Structured prose",prose,2);var tree=new TabPage("Typed AST"){BackColor=Panel,ForeColor=Ink};tree.Controls.Add(ast);tabs.TabPages.Add(tree);result.Controls.Add(tabs);result.Controls.Add(notes);
  split.Controls.Add(input);split.Controls.Add(result);outer.Controls.Add(split);
  outer.Controls.Add(new Label{Text="Independent research app. No Wispr engine extraction. Transcript parser tested; ASR not tested. Copy is explicit; no automatic cross-app insertion. Real scalars and M/L/T dimensions only.",Dock=DockStyle.Fill,ForeColor=Gold});Controls.Add(outer);
  foreach(var box in new[]{transcript,variables,latex,code,prose,ast,notes,comparison}){box.BackColor=Panel;box.ForeColor=Ink;box.BorderStyle=BorderStyle.FixedSingle;box.Font=new Font("Consolas",10);}
  mic.Click+=(_,_)=>StartMicrophone();stop.Click+=(_,_)=>StopMicrophone();FormClosing+=(_,_)=>{closing=true;StopMicrophone();};
  transcript.TextChanged+=(_,_)=>InvalidatePreview();variables.TextChanged+=(_,_)=>InvalidatePreview();comparison.TextChanged+=(_,_)=>equivalence.Text="";
  LoadExample("two times open paren x plus three close paren squared");
 }
 static Label Label(string text)=>new(){Text=text,AutoSize=true,Dock=DockStyle.Fill,ForeColor=Blue,Font=new Font("Segoe UI",9,FontStyle.Bold)};
 static Button Button(string text,Action action){var button=new Button{Text=text,AutoSize=true,FlatStyle=FlatStyle.Flat,BackColor=Panel,ForeColor=Ink};button.Click+=(_,_)=>action();return button;}
 void AddTab(TabControl tabs,string title,TextBox output,int index){var page=new TabPage(title){BackColor=Panel,ForeColor=Ink};var grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};grid.RowStyles.Add(new(SizeType.Absolute,42));grid.RowStyles.Add(new(SizeType.Percent,100));copyButtons[index]=Button("Copy this preview",()=>{if(CurrentAccepted){try{Clipboard.SetText(output.Text);status.Text="Copied the reviewed preview to clipboard. Paste manually into your destination.";}catch(Exception e){status.Text=e.Message;}}});grid.Controls.Add(copyButtons[index]);grid.Controls.Add(output);page.Controls.Add(grid);tabs.TabPages.Add(page);}
 void InvalidatePreview(){current=null;foreach(var b in copyButtons)if(b is not null)b.Enabled=false;foreach(var box in new[]{latex,code,prose,ast,notes})box.Clear();status.Text="Transcript or declarations changed. Parse again before copying.";type.Text="";equivalence.Text="";}
 public void LoadExample(string text){transcript.Text=text;Parse();}
 void Parse(){current=Language.Parse(transcript.Text,variables.Text);status.ForeColor=current.Accepted?Color.LightGreen:Gold;status.Text=current.Accepted?"ACCEPTED / review the explicit AST and obligations before copying.":$"REJECTED [{current.ErrorCode}] / {current.Error}";type.Text=current.Accepted?$"Type: real scalar · {current.Dimension} · SI value: {current.Numeric?.ToString("G12",System.Globalization.CultureInfo.InvariantCulture)??"symbolic"}":"No preview or copy is enabled for rejected input.";latex.Text=current.Latex??"";code.Text=current.Code??"";prose.Text=current.Prose??"";ast.Text=current.Ast is null?"":JsonSerializer.Serialize(current.Ast,Language.Json);notes.Text=string.Join(Environment.NewLine+Environment.NewLine,current.Notes.Distinct());foreach(var button in copyButtons)button.Enabled=current.Accepted;}
 void Compare(){Parse();if(current is null)return;var other=Language.Parse(comparison.Text,variables.Text);var result=Language.Equivalent(current,other);equivalence.Text=result.Verdict.ToUpperInvariant()+Environment.NewLine+"Details in checks panel.";notes.AppendText(Environment.NewLine+Environment.NewLine+"Comparison: "+result.Reason+(other.Accepted?"":Environment.NewLine+other.ErrorCode+": "+other.Error));}
 void UpdateUi(Action callback){if(closing||IsDisposed||Disposing||!IsHandleCreated)return;try{BeginInvoke(callback);}catch(InvalidOperationException){/* Form closed after the state check. */}}
 void StartMicrophone()
 {
  if(recognition is not null)return;
  try{
   var engine=new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-US"));recognition=engine;
   engine.LoadGrammar(new DictationGrammar());engine.InitialSilenceTimeout=TimeSpan.FromSeconds(5);engine.BabbleTimeout=TimeSpan.FromSeconds(12);
   engine.SpeechRecognized+=(_,e)=>UpdateUi(()=>{if(!ReferenceEquals(recognition,engine))return;transcript.Text=e.Result.Text;status.Text=$"Windows SAPI transcript received (confidence{e.Result.Confidence:F2}). Review/correct it and click Parse. Recognition is not parser evidence.";});
   engine.RecognizeCompleted+=(_,e)=>UpdateUi(()=>{engine.Dispose();if(!ReferenceEquals(recognition,engine))return;recognition=null;mic.Enabled=true;stop.Enabled=false;if(e.Error is not null)status.Text="Microphone recognition error: "+e.Error.Message;});
   engine.SetInputToDefaultAudioDevice();mic.Enabled=false;stop.Enabled=true;status.Text="MICROPHONE ACTIVE / one Windows SAPI utterance. Stop cancels. No audio file is saved by this app.";engine.RecognizeAsync(RecognizeMode.Single);
  }catch(Exception e){recognition?.Dispose();recognition=null;mic.Enabled=true;stop.Enabled=false;status.Text="Microphone unavailable: "+e.Message+" Paste a transcript instead.";}
 }
 void StopMicrophone()
 {
  var engine=recognition;if(engine is null)return;
  try{engine.RecognizeAsyncCancel();}catch(InvalidOperationException){engine.Dispose();recognition=null;mic.Enabled=true;}
  stop.Enabled=false;
  if(closing){recognition=null;engine.Dispose();}
 }
 public void SavePreview(string path){using var image=new Bitmap(Width,Height);DrawToBitmap(image,new Rectangle(Point.Empty,Size));image.Save(path);}
}
