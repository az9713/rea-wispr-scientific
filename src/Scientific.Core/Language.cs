// SPDX-License-Identifier: MIT
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Scientific;

public sealed class LanguageError(string code, string message) : Exception(message) { public string Code { get; } = code; }
public readonly record struct Dimension(int Mass, int Length, int Time)
{
 public static Dimension operator +(Dimension a, Dimension b)=>new(a.Mass+b.Mass,a.Length+b.Length,a.Time+b.Time);
 public static Dimension operator -(Dimension a, Dimension b)=>new(a.Mass-b.Mass,a.Length-b.Length,a.Time-b.Time);
 public Dimension Scale(int n)=>new(Mass*n,Length*n,Time*n);
 public override string ToString()=>this==default?"dimensionless":$"kg^{Mass} m^{Length} s^{Time}";
}
public readonly struct Rational : IEquatable<Rational>
{
 public BigInteger Numerator {get;} public BigInteger Denominator {get;}
 public Rational(BigInteger n,BigInteger d) {if(d==0)throw new LanguageError("zero-division","Division by zero is undefined.");if(n.GetBitLength()>8192||d.GetBitLength()>8192)throw new LanguageError("exact-budget","Exact rational coefficient budget exceeded8192 bits.");if(d<0){n=-n;d=-d;}var g=BigInteger.GreatestCommonDivisor(n,d);Numerator=n/g;Denominator=d/g;}
 public static Rational Parse(string value) {var parts=value.Split('.');BigInteger d=BigInteger.Pow(10,parts.Length==2?parts[1].Length:0);return new(BigInteger.Parse(value.Replace(".",""),CultureInfo.InvariantCulture),d);}
 public static Rational operator +(Rational a,Rational b)=>new(a.Numerator*b.Denominator+b.Numerator*a.Denominator,a.Denominator*b.Denominator);
 public static Rational operator -(Rational a,Rational b)=>new(a.Numerator*b.Denominator-b.Numerator*a.Denominator,a.Denominator*b.Denominator);
 public static Rational operator *(Rational a,Rational b)=>new(a.Numerator*b.Numerator,a.Denominator*b.Denominator);
 public static Rational operator /(Rational a,Rational b)=>new(a.Numerator*b.Denominator,a.Denominator*b.Numerator);
 public Rational Pow(int n)=>n>=0?new(BigInteger.Pow(Numerator,n),BigInteger.Pow(Denominator,n)):new(BigInteger.Pow(Denominator,-n),BigInteger.Pow(Numerator,-n));
 public double Double=>(double)Numerator/(double)Denominator;
 public bool Equals(Rational other)=>Numerator==other.Numerator&&Denominator==other.Denominator;
 public override bool Equals(object? o)=>o is Rational r&&Equals(r);public override int GetHashCode()=>HashCode.Combine(Numerator,Denominator);
 public override string ToString()=>Denominator==1?Numerator.ToString(CultureInfo.InvariantCulture):$"{Numerator}/{Denominator}";
}
public sealed record Expression(string Kind,string Value,Dimension Dimension,double? Numeric,Expression? Left=null,Expression? Right=null);
public sealed record ParseResult(bool Accepted,string? ErrorCode,string? Error,string Normalized,string? Latex,string? Code,string? Prose,string? Dimension,double? Numeric,string[] Notes,Expression? Ast);
public sealed record EquivalenceResult(string Verdict,string Reason);
internal sealed record Unit(string Symbol,Rational Scale,Dimension Dimension);

public static class Language
{
 public static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true,IncludeFields=false};
 internal static readonly Dictionary<string,Unit> Units=new(StringComparer.Ordinal) {
  ["meter"]=new("m",new(1,1),new(0,1,0)),["meters"]=new("m",new(1,1),new(0,1,0)),
  ["centimeter"]=new("cm",new(1,100),new(0,1,0)),["centimeters"]=new("cm",new(1,100),new(0,1,0)),
  ["second"]=new("s",new(1,1),new(0,0,1)),["seconds"]=new("s",new(1,1),new(0,0,1)),
  ["kilogram"]=new("kg",new(1,1),new(1,0,0)),["kilograms"]=new("kg",new(1,1),new(1,0,0)),
  ["newton"]=new("N",new(1,1),new(1,1,-2)),["newtons"]=new("N",new(1,1),new(1,1,-2))
 };
 public static readonly string DefaultVariables="x: scalar\ny: scalar\nz: scalar\nv: meter / second\nt: second\na: meter / second squared";
 public static ParseResult Parse(string transcript,string? declarations=null)
 {
  string normalized="";
  try {
   var vars=Variables(declarations??DefaultVariables);var tokens=Tokenize(transcript);normalized=string.Join(" ",tokens);
   var p=new Parser(tokens,vars);var ast=p.Run();var notes=p.Notes.ToList();
   notes.Add("Code computes SI magnitudes with double arithmetic; dimensions are reported separately.");
   notes.Add("Transcript parsing tested; microphone recognition accuracy is not tested. Review the AST before copying.");
   return new(true,null,null,normalized,Render(ast,"latex"),Render(ast,"code"),Render(ast,"prose"),ast.Dimension.ToString(),ast.Numeric,notes.ToArray(),ast);
  }catch(LanguageError e){return new(false,e.Code,e.Message,normalized,null,null,null,null,null,[],null);}
 }
 internal static string[] Tokenize(string input)
 {
  if(string.IsNullOrWhiteSpace(input)||input.Length>1200)throw new LanguageError("input-budget","Enter 1–1200 characters.");
  string text=input.ToLowerInvariant().Trim();
  foreach(var pair in new[]{("to the power of","^"),("to the power","^"),("open parenthesis","("),("close parenthesis",")"),("open paren","("),("close paren",")"),("left parenthesis","("),("right parenthesis",")"),("divided by","/"),("multiplied by","*"),("square root of","sqrt"),("square root","sqrt")})
   text=Regex.Replace(text,@"\b"+pair.Item1+@"\b",pair.Item2,RegexOptions.CultureInvariant);
  if(Regex.IsMatch(text,@"[^a-z0-9.\s()+*/^\-]",RegexOptions.CultureInvariant))throw new LanguageError("invalid-character","Only the declared math vocabulary, ASCII numbers and math operators are accepted. No punctuation cleanup is guessed.");
  var matches=Regex.Matches(text,@"\d+(?:\.\d+)?|[a-z]+|[()+*/^\-]|\.");
  string[] words=["zero","one","two","three","four","five","six","seven","eight","nine","ten","eleven","twelve","thirteen","fourteen","fifteen","sixteen","seventeen","eighteen","nineteen","twenty"];
  var tokens=matches.Select(m=>m.Value).Select(w=>Array.IndexOf(words,w) is int n&&n>=0?n.ToString(CultureInfo.InvariantCulture):w switch{"plus"=>"+","minus"=>"-","negative"=>"-","times"=>"*","over"=>"/","per"=>"/","power"=>"^","sine"=>"sin","cosine"=>"cos","logarithm"=>"log",_=>w}).ToArray();
  if(tokens.Length>100||tokens.Any(t=>t.Length>24))throw new LanguageError("token-budget","At most100 tokens, each at most24 characters, are accepted.");
  if(tokens.Contains("."))throw new LanguageError("invalid-number","Use numeric decimals such as0.25; spoken decimal phrases are not guessed.");
  return tokens;
 }
 public static Dictionary<string,Dimension> Variables(string declarations)
 {
  if(declarations.Length>2000)throw new LanguageError("declarations-budget","Variable declarations exceed2000 characters.");
  var result=new Dictionary<string,Dimension>(StringComparer.Ordinal);
  foreach(string line in declarations.Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries)) {
   var pair=line.Split(':',StringSplitOptions.TrimEntries);
   if(pair.Length!=2||!Regex.IsMatch(pair[0],"^[a-z]{1,12}$")||Tokenize(pair[0]).Single()!=pair[0]||Units.ContainsKey(pair[0])||new[]{"sin","cos","sqrt","exp","log","pi","e","plus","minus","times","over","per","squared","cubed","scalar"}.Contains(pair[0]))throw new LanguageError("variable-declaration","Use unique lowercase names: unit expression, e.g. v: meter / second. Reserved words cannot be variables.");
   Dimension dim=default;
   if(pair[1]!="scalar") {
    var parser=new Parser(Tokenize(pair[1]),new());var expr=parser.Run();
    if(HasSymbol(expr)||expr.Numeric is null||Math.Abs(expr.Numeric.Value-1)>1e-12)throw new LanguageError("variable-unit","Declare an SI unit expression of magnitude1, such as meter / second. Variable values are not specified here.");
    dim=expr.Dimension;
   }
   if(!result.TryAdd(pair[0],dim))throw new LanguageError("duplicate-variable","A variable was declared twice.");
  }
  if(result.Count>30)throw new LanguageError("declarations-budget","At most30 variables are supported.");return result;
 }
 private static bool HasSymbol(Expression e)=>e.Kind=="symbol"||(e.Left is not null&&HasSymbol(e.Left))||(e.Right is not null&&HasSymbol(e.Right));
 internal sealed class Parser(string[] tokens,Dictionary<string,Dimension> variables)
 {
  int pos,depth;public List<string> Notes {get;}=[];
  string Peek=>pos<tokens.Length?tokens[pos]:"<end>";
  string Take()=>pos<tokens.Length?tokens[pos++]:throw Error("missing-expression","An operand is missing.");
  static LanguageError Error(string c,string m)=>new(c,m);
  bool Eat(string token){if(Peek!=token)return false;pos++;return true;}
  public Expression Run(){var e=Add();if(pos!=tokens.Length)throw Error("unexpected-token",$"Unexpected token '{Peek}'. Use explicit times between variables/numbers, and declare variables first.");return e;}
  Expression Add(){var e=Multiply();while(Peek is "+" or "-"){string op=Take();e=Binary(op,e,Multiply());}return e;}
  Expression Multiply(){var e=Unary();bool ungroupedDivision=false;while(Peek is "*" or "/"||Units.ContainsKey(Peek)){
   string op=Units.ContainsKey(Peek)?"*":Take();bool grouped=Peek=="(";var right=Unary();
   if(op=="/"&&!grouped)ungroupedDivision=true;if(op=="/"&&right.Numeric is null)Notes.Add("Symbolic division requires a nonzero denominator; this obligation is not solved.");e=Binary(op,e,right);
  }if(ungroupedDivision&&Peek is "+" or "-")throw Error("ambiguous-denominator","A division followed by plus/minus needs explicit grouping. Say 'one over open paren x plus one close paren', or '(one over x) plus one'.");return e;}
  Expression Unary(){if(Eat("-")){Notes.Add("Unary minus binds less tightly than powers: minus two squared means -(2^2). Use parentheses for a negative base.");var x=Unary();return new("neg","-",x.Dimension,x.Numeric is double n?-n:null,x);}return Power();}
  Expression Power(){var e=Atom();while(Peek is "squared" or "cubed"){string word=Take();e=Binary("^",e,Number(word=="squared"?"2":"3"));}if(Eat("^")){Notes.Add("Powers associate to the right: 2^3^2 means 2^(3^2).");var exponent=Unary();if(Units.ContainsKey(Peek))throw Error("ambiguous-power-unit","Group a power before attaching units, or group the complete exponent. Units after an ungrouped exponent are ambiguous.");if(e.Numeric is null||exponent.Numeric is null)Notes.Add("Symbolic powers retain real/finite-domain obligations; zero bases with non-positive exponents are not solved.");e=Binary("^",e,exponent);}return e;}
  Expression Atom(){if(++depth>24)throw Error("nesting-budget","At most24 nested expression levels are supported.");try{
   string token=Take();if(token=="("){var e=Add();if(!Eat(")"))throw Error("unclosed-group","A close parenthesis is required.");return e;}
   if(double.TryParse(token,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out _))return Number(token);
   if(Units.TryGetValue(token,out var unit))return new("unit",token,unit.Dimension,unit.Scale.Double);
   if(token is "pi" or "e")return new("constant",token,default,token=="pi"?Math.PI:Math.E);
   if(token is "sin" or "cos" or "sqrt" or "exp" or "log"){
    if(!Eat("("))throw Error("ambiguous-function","Functions require explicit parentheses, e.g. sine open paren x close paren.");var arg=Add();if(!Eat(")"))throw Error("unclosed-group","Close the function argument.");
    var d=arg.Dimension;
    if(token=="sqrt") {if(d.Mass%2!=0||d.Length%2!=0||d.Time%2!=0)throw Error("fractional-dimension","Square roots require even integer dimension exponents in this version.");d=new(d.Mass/2,d.Length/2,d.Time/2);}
    else if(d!=default)throw Error("dimension-function","sin/cos/exp/log require dimensionless arguments (angles in radians).");
    if(arg.Numeric is null&&token is "sqrt" or "log")Notes.Add("Symbolic sqrt/log domain restrictions remain obligations; variables have no numeric assignments.");
    double? value=null;if(arg.Numeric is double a){if((token=="sqrt"&&a<0)||(token=="log"&&a<=0))throw Error("real-domain","Function argument is outside the declared real domain.");value=token switch{"sin"=>Math.Sin(a),"cos"=>Math.Cos(a),"sqrt"=>Math.Sqrt(a),"exp"=>Math.Exp(a),_=>Math.Log(a)};Finite(value);}
    return new("function",token,d,value,arg);
   }
   if(variables.TryGetValue(token,out var dimension))return new("symbol",token,dimension,null);
   throw Error("unknown-token",$"Unknown token '{token}'. No correction, homophone or implicit variable is guessed.");
  }finally{depth--;}}
  static Expression Number(string text){var n=Rational.Parse(text).Double;Finite(n);return new("number",text,default,n);}
  static void Finite(double? n){if(n is double v&&!double.IsFinite(v))throw Error("numeric-overflow","Numeric result is not finite.");}
  static Expression Binary(string op,Expression a,Expression b){Dimension d;double? n=null;
   if(op is "+" or "-"){if(a.Dimension!=b.Dimension)throw Error("dimension-addition","Addition/subtraction requires matching dimensions; zero is not automatically assigned a unit.");d=a.Dimension;}
   else if(op=="*")d=a.Dimension+b.Dimension;
   else if(op=="/"){if(b.Numeric==0)throw Error("zero-division","Division by zero is undefined.");d=a.Dimension-b.Dimension;}
   else {
    if(b.Dimension!=default)throw Error("dimension-exponent","An exponent must be dimensionless.");d=default;
    if(a.Dimension!=default){if(b.Numeric is not double p||p!=Math.Truncate(p)||Math.Abs(p)>12)throw Error("dimension-power","Dimensioned bases need a constant integer exponent between-12 and12.");d=a.Dimension.Scale((int)p);}
    if(a.Numeric==0&&b.Numeric<=0)throw Error("power-domain","Zero to a non-positive power is undefined in this grammar.");
   }
   if(new[]{d.Mass,d.Length,d.Time}.Any(v=>Math.Abs(v)>24))throw Error("dimension-budget","Dimension exponents exceed the supported range.");
   if(a.Numeric is double av&&b.Numeric is double bv){n=op switch{"+"=>av+bv,"-"=>av-bv,"*"=>av*bv,"/"=>av/bv,_=>Math.Pow(av,bv)};if(double.IsNaN(n.Value))throw Error("real-domain","Power leaves the real-number domain.");Finite(n);}
   return new("binary",op,d,n,a,b);
  }
 }
 public static string Render(Expression e,string mode)
 {
  string A()=>Render(e.Left!,mode);string B()=>Render(e.Right!,mode);
  if(e.Kind=="number")return mode=="code"?(e.Value.Contains('.')?e.Value:e.Value+".0"):e.Value;
  if(e.Kind=="symbol")return mode=="code"?"@"+e.Value:mode=="latex"?e.Value switch{"alpha"=>@"\alpha","beta"=>@"\beta","theta"=>@"\theta","gamma"=>@"\gamma",_=>@"\mathrm{"+e.Value+"}"}:e.Value;
  if(e.Kind=="unit"){var u=Units[e.Value];return mode=="latex"?@"\mathrm{"+u.Symbol+"}":mode=="code"?u.Scale.Double.ToString("R",CultureInfo.InvariantCulture)+(u.Scale.Denominator==1?".0":""):e.Value;}
  if(e.Kind=="constant")return mode=="code"?(e.Value=="pi"?"Math.PI":"Math.E"):mode=="latex"&&e.Value=="pi"?@"\pi":e.Value;
  if(e.Kind=="neg")return mode=="prose"?$"negative of ({A()})":$"-({A()})";
  if(e.Kind=="function")return mode=="code"?$"Math.{e.Value switch{"sin"=>"Sin","cos"=>"Cos","sqrt"=>"Sqrt","exp"=>"Exp",_=>"Log"}}({A()})":mode=="latex"?(e.Value=="sqrt"?@"\sqrt{"+A()+"}":@"\"+e.Value+@"\left("+A()+@"\right)"):$"{e.Value} of ({A()})";
  if(mode=="prose")return $"{e.Value switch{"+"=>"sum","-"=>"difference","*"=>"product","/"=>"quotient",_=>"power"}} of ({A()}) and ({B()})";
  if(mode=="code")return e.Value=="^"?$"Math.Pow({A()}, {B()})":$"({A()} {e.Value} {B()})";
  if(e.Value=="/")return @"\frac{"+A()+"}{"+B()+"}";
  if(e.Value=="^")return @"\left("+A()+@"\right)^{"+B()+"}";
  return @"\left("+A()+(e.Value=="*"?@"\cdot ":e.Value)+B()+@"\right)";
 }
 public static EquivalenceResult Equivalent(ParseResult a,ParseResult b)
 {
  if(!a.Accepted||!b.Accepted)return new("unknown","Both expressions must parse successfully.");
  if(a.Ast!.Dimension!=b.Ast!.Dimension)return new("different","Root dimensions differ.");
  try{var x=Clean(Polynomial(a.Ast));var y=Clean(Polynomial(b.Ast));return new(x.Count==y.Count&&x.All(k=>y.TryGetValue(k.Key,out var v)&&v.Equals(k.Value))?"equivalent":"different","Exact SI rational-polynomial normalization; independent declared symbols, supported operations only.");}
  catch(LanguageError e){return new("unknown",e.Message);}
 }
 static Dictionary<string,Rational> Polynomial(Expression e)
 {
  if(e.Kind=="number")return new(){[""]=Rational.Parse(e.Value)};
  if(e.Kind=="unit")return new(){[""]=Units[e.Value].Scale};
  if(e.Kind=="symbol")return new(){[$"{e.Value}[{e.Dimension.Mass},{e.Dimension.Length},{e.Dimension.Time}]"]=new(1,1)};
  if(e.Kind is "function" or "constant")throw new LanguageError("unsupported-equivalence","Functions and pi/e are outside exact polynomial equivalence; no numerical sampling is used as proof.");
  if(e.Kind=="neg")return Polynomial(e.Left!).ToDictionary(k=>k.Key,k=>new Rational(-k.Value.Numerator,k.Value.Denominator));
  var a=Polynomial(e.Left!);var b=Polynomial(e.Right!);
  if(e.Value=="^"){
   if(b.Count!=1||!b.ContainsKey("")||b[""].Denominator!=1||b[""].Numerator<1||b[""].Numerator>8)throw new LanguageError("unsupported-equivalence","Polynomial powers require a constant integer exponent1–8; exponentzero and domain cancellation are not proved.");
   var result=new Dictionary<string,Rational>{{"",new(1,1)}};for(int i=0;i<(int)b[""].Numerator;i++)result=Multiply(result,a);return result;
  }
  if(e.Value=="*")return Multiply(a,b);
  if(e.Value=="/"){if(b.Count!=1||!b.TryGetValue("",out var c)||c.Numerator==0)throw new LanguageError("unsupported-equivalence","Division by a symbolic expression is outside polynomial equivalence; domains are not cancelled.");return a.ToDictionary(k=>k.Key,k=>k.Value/c);}
  var sum=new Dictionary<string,Rational>(a);foreach(var k in b){var value=e.Value=="+"?k.Value:new Rational(-k.Value.Numerator,k.Value.Denominator);sum[k.Key]=sum.TryGetValue(k.Key,out var old)?old+value:value;}return Clean(sum);
 }
 static Dictionary<string,Rational> Multiply(Dictionary<string,Rational>a,Dictionary<string,Rational>b){if(a.Count*b.Count>400)throw new LanguageError("equivalence-budget","Polynomial expansion exceeds400 intermediate products.");var result=new Dictionary<string,Rational>();foreach(var x in a)foreach(var y in b){var parts=(x.Key+" "+y.Key).Split(' ',StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal);string key=string.Join(' ',parts);if(key.Split(' ',StringSplitOptions.RemoveEmptyEntries).Length>12)throw new LanguageError("equivalence-budget","Polynomial degree exceeds12.");var value=x.Value*y.Value;result[key]=result.TryGetValue(key,out var old)?old+value:value;}return Clean(result);}
 static Dictionary<string,Rational> Clean(Dictionary<string,Rational> p)=>p.Where(k=>k.Value.Numerator!=0).ToDictionary(k=>k.Key,k=>k.Value);
}
