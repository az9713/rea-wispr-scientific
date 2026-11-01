# Controlled transcript language

```
expression := product (('plus' | 'minus' | '+' | '-') product)*
product    := unary (('*' | 'times' | '/' | 'over' | 'per') unary | UNIT)*
unary      := ('minus' | 'negative' | '-') unary | power
power      := atom ('squared' | 'cubed')* ('^' unary)?
atom       := NUMBER | DECLARED_VARIABLE | UNIT | 'pi' | 'e'
            | '(' expression ')'
            | FUNCTION '(' expression ')'
```

Lexical aliases: open/close paren, open/close parenthesis, left/right parenthesis; divided by; multiplied by; to the power [of]; sine/cosine/logarithm; square root [of]. No fuzzy matching. Alphabetic variable names must be declared and cannot collide with numbers/units/operators/functions. Identifier case is lowered in the transcript; declaration names are lowercase.

Single-word English integers zero–twenty are accepted. Larger values use digits. Numeric decimals require digits on both sides of the dot. Scientific notation and spoken decimal phrases are outside scope. Punctuation other than explicit math syntax is rejected.

Additional conservative rules:

- An ungrouped division followed by + or - at its product level is rejected as ambiguous. `(one over x) plus one` and `one over (x plus one)` are distinct accepted forms.
- A function needs explicit parentheses. `sine x plus one` is rejected, not guessed.
- Units immediately following an ungrouped exponent are rejected. Group the power before attaching units, or group the complete exponent.
- A numeric coefficient followed by a unit multiplies that coefficient by the following unit atom/power. `two meters squared` = `2 * meter^2`. `(two meters) squared` = `(2 * meter)^2`.
- Standard precedence is declared, including unary minus and right-associative powers; the AST and notes expose it. This is a language contract, not a guarantee to resolve every ambiguity in arbitrary English.

Dimensions use only integer M/L/T exponents. SI units: meter, second, kilogram, newton. Centimeter carries rational scale1/100. Plurals accepted. Variable declarations describe dimensions through magnitude-one SI expressions; no variable values or scaled-unit variable convention is inferred. Zero is dimensionless until explicitly multiplied by units.

Real-domain checks evaluate constants only. Symbolic logarithms, square roots, denominators and powers retain unresolved obligations. Dimension checking alone does not prove an expression defined, physically meaningful or numerically stable.

Budgets:1200 transcript characters;100 tokens;24 characters/token;24 nested atom/function groups;2000 declaration characters;30 variables;dimension exponents within±24. Dimensioned powers require constant integer±12. Equivalence powers require integers1–8;degree≤12;≤400 intermediate term products per multiplication;exact numerator/denominator≤8192bits. These caps bound supported work, not a hostile-input sandbox certification.
