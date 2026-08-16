# Quickstart: Using Statistical Packages

A strong R community has contributed over 20,000 packages to CRAN,
R's central package registry. The F# R Type Provider enables you to
use every single one of them from within the F# environment.

Using RRrovider, you can orchestrate R workflows and manipulate R data,
pass in F# values, and extract R values back to F#.

For this example, we simply demonstrate some basic RProvider concepts
using the built-in `stats` package.

## Example: Linear Regression

Let's perform a simple linear regression from the F# interactive,
using the R.lm function.

Once you have referenced RProvider's nuget package in your script,
library, or app, you can reference the required libraries and packages this way:

```fsharp
open RProvider
open RProvider.Operators

open RProvider.graphics
open RProvider.stats
```

Once the libraries and packages have been loaded,
Imagine that our true model is

Y = 5.0 + 3.0 * X1 - 2.0 * X2 + noise

Let's generate a fake dataset using F# that follows this model:

```fsharp
// Random number generator
let rng = System.Random()
let rand () = rng.NextDouble()

// Generate fake X1 and X2 
let X1s = [ for i in 0 .. 9 -> 10. * rand () ]
let X2s = [ for i in 0 .. 9 -> 5. * rand () ]

// Build Ys, following the "true" model
let Ys = [ for i in 0 .. 9 -> 5. + 3. * X1s.[i] - 2. * X2s.[i] + rand () ]
```

Using linear regression on this dataset, we should be able to
estimate the coefficients 5.0, 3.0 and -2.0, with some imprecision
due to the "noise" part.

Let's first put our dataset into a R dataframe; this allows us
to name our vectors, and use these names in R formulas afterwards:

```fsharp
let dataset = [ 
    "Y" => Ys
    "X1" => X1s
    "X2" => X2s ] |> R.data_frame
```

We can now use R to perform a linear regression.
We call the [R.lm function](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/lm.html),
passing it the formula we want to estimate.
(See the [R manual on formulas](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/formula.html)
for more on their somewhat esoteric construction)

```fsharp
let result = R.lm(formula = "Y~X1+X2", data = dataset)
```

## Extracting Results from R to F#

The result we get back from R is a R Expression.
The R Type Provider tries as much as possible to keep data
as R Expressions, rather than converting back-and-forth
between F# and R types. It limits translations
between the 2 languages, which has performance benefits,
and simplifies composing R operations. On the other hand,
we need to extract the results from the R expression
into F# types.

The [R docs for lm](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/lm.html)
describes what R.lm returns: a R List. We can now retrieve each element,
accessing it by name (as defined in the documentation).
For instance, let's retrieve the coefficients and residuals,
which are both R vectors containg floats:

```fsharp
let coefficients = result?coefficients.AsVector().AsReal()
let residuals = result?residuals.AsVector().AsReal()
```

We can also produce summary statistics about our model,
like R^2, which measures goodness-of-fit - close to 0
indicates a very poor fit, and close to 1 a good fit.
See [R docs for the details on Summary](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/summary.lm.html).

```fsharp
let summary = R.summary result

summary?``r.squared``.AsScalar()
```

```
NumericS { Sexp = { ptr = 47315853544n } }
```

Finally, we can directly pass results, which is a R expression,
to R.plot, to produce some fancy charts describing our model:

```fsharp
Graphics.svg 8 4 (fun _ -> R.plot result)
```

<?xml version='1.0' encoding='UTF-8' ?>
<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='576.00pt' height='288.00pt' viewBox='0 0 576.00 288.00'>
<g class='svglite'>
<defs>
  <style type='text/css'><![CDATA[
    .svglite line, .svglite polyline, .svglite polygon, .svglite path, .svglite rect, .svglite circle {
      fill: none;
      stroke: #000000;
      stroke-linecap: round;
      stroke-linejoin: round;
      stroke-miterlimit: 10.00;
    }
    .svglite text {
      white-space: pre;
    }
    .svglite g.glyphgroup path {
      fill: inherit;
      stroke: none;
    }
  ]]></style>
</defs>
<rect width='100%' height='100%' style='stroke: none; fill: #FFFFFF;'/>
<defs>
  <clipPath id='cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA='>
    <rect x='0.00' y='0.00' width='576.00' height='288.00' />
  </clipPath>
</defs>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
</g>
<defs>
  <clipPath id='cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng=='>
    <rect x='59.04' y='59.04' width='486.72' height='155.52' />
  </clipPath>
</defs>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<line x1='77.07' y1='214.56' x2='544.94' y2='214.56' style='stroke-width: 0.75;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='221.76' style='stroke-width: 0.75;' />
<line x1='170.64' y1='214.56' x2='170.64' y2='221.76' style='stroke-width: 0.75;' />
<line x1='264.22' y1='214.56' x2='264.22' y2='221.76' style='stroke-width: 0.75;' />
<line x1='357.79' y1='214.56' x2='357.79' y2='221.76' style='stroke-width: 0.75;' />
<line x1='451.37' y1='214.56' x2='451.37' y2='221.76' style='stroke-width: 0.75;' />
<line x1='544.94' y1='214.56' x2='544.94' y2='221.76' style='stroke-width: 0.75;' />
<text x='77.07' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.0</text>
<text x='170.64' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.1</text>
<text x='264.22' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.2</text>
<text x='357.79' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.3</text>
<text x='451.37' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.4</text>
<text x='544.94' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<line x1='59.04' y1='198.66' x2='59.04' y2='89.21' style='stroke-width: 0.75;' />
<line x1='59.04' y1='198.66' x2='51.84' y2='198.66' style='stroke-width: 0.75;' />
<line x1='59.04' y1='162.18' x2='51.84' y2='162.18' style='stroke-width: 0.75;' />
<line x1='59.04' y1='125.69' x2='51.84' y2='125.69' style='stroke-width: 0.75;' />
<line x1='59.04' y1='89.21' x2='51.84' y2='89.21' style='stroke-width: 0.75;' />
<text transform='translate(41.76,198.66) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='10.67px' lengthAdjust='spacingAndGlyphs'>-2</text>
<text transform='translate(41.76,162.18) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='10.67px' lengthAdjust='spacingAndGlyphs'>-1</text>
<text transform='translate(41.76,125.69) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>0</text>
<text transform='translate(41.76,89.21) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>1</text>
<polygon points='59.04,214.56 545.76,214.56 545.76,59.04 59.04,59.04 ' style='stroke-width: 0.75;' />
<text x='302.40' y='269.28' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='50.03px' lengthAdjust='spacingAndGlyphs'>Leverage</text>
<text transform='translate(12.96,136.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='122.06px' lengthAdjust='spacingAndGlyphs'>Standardized residuals</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<circle cx='202.07' cy='136.26' r='2.70' style='stroke-width: 0.75;' />
<circle cx='234.67' cy='74.73' r='2.70' style='stroke-width: 0.75;' />
<circle cx='476.98' cy='86.70' r='2.70' style='stroke-width: 0.75;' />
<circle cx='302.18' cy='153.21' r='2.70' style='stroke-width: 0.75;' />
<circle cx='320.43' cy='97.67' r='2.70' style='stroke-width: 0.75;' />
<circle cx='526.83' cy='129.27' r='2.70' style='stroke-width: 0.75;' />
<circle cx='268.98' cy='198.87' r='2.70' style='stroke-width: 0.75;' />
<circle cx='479.32' cy='120.83' r='2.70' style='stroke-width: 0.75;' />
<circle cx='238.71' cy='148.22' r='2.70' style='stroke-width: 0.75;' />
<circle cx='527.73' cy='100.18' r='2.70' style='stroke-width: 0.75;' />
<polyline points='202.07,127.64 234.67,146.43 238.71,148.67 268.98,154.68 302.18,134.93 320.43,121.66 476.98,104.40 479.32,104.83 526.83,114.41 527.73,114.61 ' style='stroke-width: 0.75; stroke: #DF536B;' />
<line x1='59.04' y1='125.69' x2='545.76' y2='125.69' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='59.04' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<text x='302.40' y='283.68' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='403.58px' lengthAdjust='spacingAndGlyphs'>(function (formula, data, subset, weights, na.action, method = "qr", model  ...</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<polyline points='81.57,-516.63 86.22,-324.00 90.86,-239.66 95.50,-189.53 100.14,-155.33 104.78,-130.07 109.42,-110.41 114.07,-94.53 118.71,-81.36 123.35,-70.19 127.99,-60.56 132.63,-52.15 137.28,-44.70 141.92,-38.06 146.56,-32.07 151.20,-26.64 155.84,-21.69 160.49,-17.14 165.13,-12.94 169.77,-9.06 174.41,-5.44 179.05,-2.07 183.69,1.09 188.34,4.06 192.98,6.86 197.62,9.50 202.26,12.00 206.90,14.37 211.55,16.62 216.19,18.77 220.83,20.81 225.47,22.77 230.11,24.64 234.75,26.44 239.40,28.16 244.04,29.81 248.68,31.40 253.32,32.94 257.96,34.41 262.61,35.84 267.25,37.22 271.89,38.55 276.53,39.84 281.17,41.09 285.82,42.30 290.46,43.48 295.10,44.62 299.74,45.73 304.38,46.81 309.02,47.86 313.67,48.88 318.31,49.88 322.95,50.85 327.59,51.79 332.23,52.72 336.88,53.62 341.52,54.50 346.16,55.36 350.80,56.20 355.44,57.03 360.09,57.83 364.73,58.62 369.37,59.40 374.01,60.15 378.65,60.90 383.29,61.62 387.94,62.34 392.58,63.04 397.22,63.73 401.86,64.41 406.50,65.07 411.15,65.73 415.79,66.37 420.43,67.00 425.07,67.62 429.71,68.23 434.36,68.84 439.00,69.43 443.64,70.01 448.28,70.59 452.92,71.15 457.56,71.71 462.21,72.26 466.85,72.81 471.49,73.34 476.13,73.87 480.77,74.39 485.42,74.91 490.06,75.42 494.70,75.92 499.34,76.42 503.98,76.91 508.63,77.39 513.27,77.87 517.91,78.35 522.55,78.82 527.19,79.28 531.83,79.74 536.48,80.19 541.12,80.64 545.76,81.09 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,768.01 86.22,575.39 90.86,491.05 95.50,440.91 100.14,406.71 104.78,381.45 109.42,361.79 114.07,345.92 118.71,332.74 123.35,321.58 127.99,311.95 132.63,303.53 137.28,296.09 141.92,289.44 146.56,283.45 151.20,278.02 155.84,273.07 160.49,268.52 165.13,264.33 169.77,260.44 174.41,256.83 179.05,253.45 183.69,250.29 188.34,247.32 192.98,244.53 197.62,241.89 202.26,239.39 206.90,237.02 211.55,234.76 216.19,232.62 220.83,230.57 225.47,228.61 230.11,226.74 234.75,224.95 239.40,223.22 244.04,221.57 248.68,219.98 253.32,218.45 257.96,216.97 262.61,215.54 267.25,214.16 271.89,212.83 276.53,211.54 281.17,210.29 285.82,209.08 290.46,207.90 295.10,206.76 299.74,205.65 304.38,204.57 309.02,203.52 313.67,202.50 318.31,201.51 322.95,200.54 327.59,199.59 332.23,198.67 336.88,197.76 341.52,196.88 346.16,196.02 350.80,195.18 355.44,194.36 360.09,193.55 364.73,192.76 369.37,191.99 374.01,191.23 378.65,190.49 383.29,189.76 387.94,189.04 392.58,188.34 397.22,187.65 401.86,186.98 406.50,186.31 411.15,185.66 415.79,185.01 420.43,184.38 425.07,183.76 429.71,183.15 434.36,182.55 439.00,181.95 443.64,181.37 448.28,180.79 452.92,180.23 457.56,179.67 462.21,179.12 466.85,178.57 471.49,178.04 476.13,177.51 480.77,176.99 485.42,176.47 490.06,175.96 494.70,175.46 499.34,174.96 503.98,174.47 508.63,173.99 513.27,173.51 517.91,173.04 522.55,172.57 527.19,172.10 531.83,171.64 536.48,171.19 541.12,170.74 545.76,170.30 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,-782.69 86.22,-510.27 90.86,-391.00 95.50,-320.10 100.14,-271.74 104.78,-236.01 109.42,-208.20 114.07,-185.75 118.71,-167.12 123.35,-151.33 127.99,-137.71 132.63,-125.81 137.28,-115.28 141.92,-105.88 146.56,-97.42 151.20,-89.74 155.84,-82.73 160.49,-76.30 165.13,-70.37 169.77,-64.87 174.41,-59.76 179.05,-54.99 183.69,-50.52 188.34,-46.32 192.98,-42.37 197.62,-38.63 202.26,-35.10 206.90,-31.75 211.55,-28.56 216.19,-25.52 220.83,-22.63 225.47,-19.86 230.11,-17.21 234.75,-14.68 239.40,-12.24 244.04,-9.90 248.68,-7.65 253.32,-5.48 257.96,-3.39 262.61,-1.38 267.25,0.57 271.89,2.46 276.53,4.28 281.17,6.05 285.82,7.76 290.46,9.43 295.10,11.04 299.74,12.61 304.38,14.13 309.02,15.62 313.67,17.06 318.31,18.47 322.95,19.84 327.59,21.18 332.23,22.49 336.88,23.76 341.52,25.01 346.16,26.23 350.80,27.42 355.44,28.58 360.09,29.72 364.73,30.84 369.37,31.93 374.01,33.01 378.65,34.06 383.29,35.09 387.94,36.10 392.58,37.09 397.22,38.07 401.86,39.02 406.50,39.96 411.15,40.89 415.79,41.80 420.43,42.69 425.07,43.57 429.71,44.43 434.36,45.28 439.00,46.12 443.64,46.95 448.28,47.76 452.92,48.56 457.56,49.35 462.21,50.13 466.85,50.90 471.49,51.66 476.13,52.41 480.77,53.15 485.42,53.88 490.06,54.60 494.70,55.31 499.34,56.01 503.98,56.70 508.63,57.39 513.27,58.07 517.91,58.74 522.55,59.40 527.19,60.05 531.83,60.70 536.48,61.35 541.12,61.98 545.76,62.61 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,1034.07 86.22,761.66 90.86,642.38 95.50,571.48 100.14,523.12 104.78,487.39 109.42,459.59 114.07,437.14 118.71,418.51 123.35,402.71 127.99,389.10 132.63,377.19 137.28,366.66 141.92,357.26 146.56,348.80 151.20,341.12 155.84,334.12 160.49,327.68 165.13,321.75 169.77,316.26 174.41,311.14 179.05,306.37 183.69,301.90 188.34,297.71 192.98,293.75 197.62,290.02 202.26,286.48 206.90,283.13 211.55,279.94 216.19,276.91 220.83,274.01 225.47,271.24 230.11,268.60 234.75,266.06 239.40,263.62 244.04,261.29 248.68,259.03 253.32,256.87 257.96,254.78 262.61,252.76 267.25,250.81 271.89,248.92 276.53,247.10 281.17,245.33 285.82,243.62 290.46,241.96 295.10,240.34 299.74,238.77 304.38,237.25 309.02,235.76 313.67,234.32 318.31,232.91 322.95,231.54 327.59,230.20 332.23,228.89 336.88,227.62 341.52,226.37 346.16,225.15 350.80,223.96 355.44,222.80 360.09,221.66 364.73,220.54 369.37,219.45 374.01,218.38 378.65,217.33 383.29,216.29 387.94,215.28 392.58,214.29 397.22,213.32 401.86,212.36 406.50,211.42 411.15,210.50 415.79,209.59 420.43,208.69 425.07,207.81 429.71,206.95 434.36,206.10 439.00,205.26 443.64,204.43 448.28,203.62 452.92,202.82 457.56,202.03 462.21,201.25 466.85,200.48 471.49,199.72 476.13,198.97 480.77,198.24 485.42,197.51 490.06,196.79 494.70,196.08 499.34,195.37 503.98,194.68 508.63,193.99 513.27,193.32 517.91,192.65 522.55,191.98 527.19,191.33 531.83,190.68 536.48,190.04 541.12,189.40 545.76,188.77 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<line x1='69.84' y1='206.46' x2='91.44' y2='206.46' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<text x='94.14' y='210.76' style='font-size: 12.00px;fill: #9E9E9E; font-family: "Arial";' textLength='84.33px' lengthAdjust='spacingAndGlyphs'>Cook's distance</text>
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<line x1='545.76' y1='188.77' x2='545.76' y2='62.61' style='stroke-width: 0.75;' />
<line x1='545.76' y1='188.77' x2='545.76' y2='188.77' style='stroke-width: 0.75;' />
<line x1='545.76' y1='170.30' x2='545.76' y2='170.30' style='stroke-width: 0.75;' />
<line x1='545.76' y1='81.09' x2='545.76' y2='81.09' style='stroke-width: 0.75;' />
<line x1='545.76' y1='62.61' x2='545.76' y2='62.61' style='stroke-width: 0.75;' />
<text x='549.36' y='191.99' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='549.36' y='173.52' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='84.31' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='65.83' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='302.40' y='52.56' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='121.39px' lengthAdjust='spacingAndGlyphs'>Residuals vs Leverage</text>
<text x='272.58' y='203.88' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>7</text>
<text x='473.38' y='88.84' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>3</text>
<text x='524.13' y='102.32' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='10.00px' lengthAdjust='spacingAndGlyphs'>10</text>
</g>
</g>
</svg>

That's it - while simple, we hope this example illustrate
how you would go about to use any existing R statistical package.
While the details would differ, the general approach would
remain the same. Happy modelling!
