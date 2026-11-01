// SPDX-License-Identifier: MIT
using Scientific;
namespace Scientific.Tests;
public sealed record TestOutcome(string Name,bool Passed,string? Error);
public sealed record TestReport(int Passed,int Failed,TestOutcome[] Results);
public static class TestSuite
{
 public static TestReport Run()
 {
  var outcomes=new List<TestOutcome>();
  void Check(string name,Action action){try{action();outcomes.Add(new(name,true,null));}catch(Exception e){outcomes.Add(new(name,false,e.Message));}}
  void Assert(bool condition,string message="Unexpected result"){if(!condition)throw new Exception(message);}
  void Numeric(string text,double expected){var r=Language.Parse(text);Assert(r.Accepted,r.Error??"rejected");Assert(r.Numeric is double v&&Math.Abs(v-expected)<1e-10,$"Expected{expected}, got{r.Numeric}");}
  void Reject(string text,string code,string? vars=null){var r=Language.Parse(text,vars);Assert(!r.Accepted&&r.ErrorCode==code,$"Expected{code}, got{r.ErrorCode} /{r.Error}");}
  void Eq(string a,string b,string verdict)=>Assert(Language.Equivalent(Language.Parse(a),Language.Parse(b)).Verdict==verdict,$"Equivalence expected{verdict}");
  Check("multiplication precedes addition",()=>Numeric("two plus three times four",14));
  Check("explicit groups override precedence",()=>Numeric("open paren two plus three close paren times four",20));
  Check("unary minus binds after powers",()=>Numeric("minus two squared",-4));
  Check("negative base requires parentheses",()=>Numeric("open paren minus two close paren squared",4));
  Check("right associative powers",()=>Numeric("two power three power two",512));
  Check("negative exponent",()=>Numeric("two power minus two",0.25));
  Check("grouped fraction",()=>Numeric("one over open paren two plus two close paren",0.25));
  Check("ungrouped division plus rejected",()=>Reject("one over x plus one","ambiguous-denominator"));
  Check("grouped quotient then addition",()=>Assert(Language.Parse("open paren one over x close paren plus one").Accepted));
  Check("unknown homophone rejected",()=>Reject("two plus won","unknown-token"));
  Check("unknown variable rejected",()=>Reject("omega plus one","unknown-token"));
  Check("implicit variable multiplication rejected",()=>Reject("two x","unexpected-token"));
  Check("empty transcript rejected",()=>Reject("","input-budget"));
  Check("injection punctuation rejected",()=>Reject("x; system()","invalid-character"));
  Check("unclosed group rejected",()=>Reject("open paren x plus one","unclosed-group"));
  Check("decimal rational input",()=>Numeric("0.1 plus 0.2",0.3));
  Check("spoken decimal ambiguity rejected",()=>Reject("one point five","unexpected-token"));
  Check("zero division rejected",()=>Reject("one divided by zero","zero-division"));
  Check("computed constant zero division rejected",()=>Reject("one over open paren two minus two close paren","zero-division"));
  Check("zero exponent domain",()=>Reject("zero power zero","power-domain"));
  Check("negative real sqrt rejected",()=>Reject("sqrt open paren minus one close paren","real-domain"));
  Check("log zero rejected",()=>Reject("log open paren zero close paren","real-domain"));
  Check("functions require explicit grouping",()=>Reject("sine x plus one","ambiguous-function"));
  Check("radian sine",()=>Numeric("sine open paren pi over two close paren",1));
  Check("dimensioned trig rejected",()=>Reject("sin open paren two meters close paren","dimension-function"));
  Check("unit implicit numeric product",()=>{var r=Language.Parse("two meters per second");Assert(r.Accepted&&r.Ast!.Dimension==new Dimension(0,1,-1)&&r.Numeric==2);});
  Check("dimension mismatch rejected",()=>Reject("two meters plus three seconds","dimension-addition"));
  Check("centimeter SI scale",()=>{var r=Language.Parse("100 centimeters");Assert(r.Accepted&&r.Numeric==1&&r.Ast!.Dimension==new Dimension(0,1,0));});
  Check("derived SI units",()=>Eq("one newton","one kilogram times one meter over open paren one second squared close paren","equivalent"));
  Check("dimensioned square root",()=>{var r=Language.Parse("sqrt open paren nine meters squared close paren");Assert(r.Accepted&&r.Numeric==3&&r.Ast!.Dimension==new Dimension(0,1,0));});
  Check("fractional dimension rejected",()=>Reject("sqrt open paren one meter close paren","fractional-dimension"));
  Check("dimensioned exponent rejected",()=>Reject("two power open paren one meter close paren","dimension-exponent"));
  Check("ungrouped exponent then unit rejected",()=>Reject("two power one meter","ambiguous-power-unit"));
  Check("symbolic dimensioned exponent rejected",()=>Reject("two meters power x","dimension-power"));
  Check("declared velocity times time",()=>{var r=Language.Parse("v times t");Assert(r.Accepted&&r.Ast!.Dimension==new Dimension(0,1,0));});
  Check("duplicate declarations rejected",()=>Reject("x","duplicate-variable","x: scalar\nx: meter"));
  Check("number word declaration rejected",()=>Reject("one","variable-declaration","one: scalar"));
  Check("equivalent expanded polynomial",()=>Eq("open paren x plus one close paren squared","x squared plus two times x plus one","equivalent"));
  Check("exact decimal equivalence",()=>Eq("0.1 plus 0.2","0.3","equivalent"));
  Check("zero canonicalization",()=>Eq("x minus x","zero","equivalent"));
  Check("non-equivalent polynomial",()=>Eq("x squared plus one","x squared plus two","different"));
  Check("symbolic cancellation not proved",()=>Eq("x over x","one","unknown"));
  Check("trig identity not guessed",()=>Eq("sine open paren x close paren squared plus cosine open paren x close paren squared","one","unknown"));
  Check("dimensions distinguish equivalence",()=>Eq("one meter","one second","different"));
  Check("integer numeric code emits double division",()=>Assert(Language.Parse("one over two").Code=="(1.0 / 2.0)"));
  Check("power code uses Math.Pow",()=>Assert(Language.Parse("x squared").Code=="Math.Pow(@x, 2.0)"));
  Check("LaTeX fraction preserves grouping",()=>Assert(Language.Parse("one over open paren x plus one close paren").Latex==@"\frac{1}{\left(\mathrm{x}+1\right)}"));
  Check("input budget enforced",()=>Reject(new string('x',1201),"input-budget"));
  Check("token budget enforced",()=>Reject(string.Join(" plus ",Enumerable.Repeat("x",60)),"token-budget"));
  Check("nesting budget enforced",()=>Reject(new string('(',30)+"x"+new string(')',30),"nesting-budget"));
  Check("overflow rejected",()=>Reject("ten power 9999","numeric-overflow"));
  Check("symbolic domains remain visible",()=>Assert(Language.Parse("one over x").Notes.Any(n=>n.Contains("nonzero"))));
  return new(outcomes.Count(x=>x.Passed),outcomes.Count(x=>!x.Passed),outcomes.ToArray());
 }
}
