(**

*)
#r "nuget: RProvider,{{package-version}}"
(**
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

*)
open RProvider
open RProvider.Operators

open RProvider.graphics
open RProvider.stats
(**
Once the libraries and packages have been loaded,
Imagine that our true model is

Y = 5.0 + 3.0 * X1 - 2.0 * X2 + noise

Let's generate a fake dataset using F# that follows this model:

*)
// Random number generator
let rng = System.Random()
let rand () = rng.NextDouble()

// Generate fake X1 and X2 
let X1s = [ for i in 0 .. 9 -> 10. * rand () ]
let X2s = [ for i in 0 .. 9 -> 5. * rand () ]

// Build Ys, following the "true" model
let Ys = [ for i in 0 .. 9 -> 5. + 3. * X1s.[i] - 2. * X2s.[i] + rand () ]
(**
Using linear regression on this dataset, we should be able to
estimate the coefficients 5.0, 3.0 and -2.0, with some imprecision
due to the "noise" part.

Let's first put our dataset into a R dataframe; this allows us
to name our vectors, and use these names in R formulas afterwards:

*)
let dataset = [ 
    "Y" => Ys
    "X1" => X1s
    "X2" => X2s ] |> R.data_frame
(**
We can now use R to perform a linear regression.
We call the [R.lm function](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/lm.html),
passing it the formula we want to estimate.
(See the [R manual on formulas](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/formula.html)
for more on their somewhat esoteric construction)

*)
let result = R.lm(formula = "Y~X1+X2", data = dataset)
(**
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

*)
let coefficients = result?coefficients.AsVector().AsReal()
let residuals = result?residuals.AsVector().AsReal()
(**
We can also produce summary statistics about our model,
like R^2, which measures goodness-of-fit - close to 0
indicates a very poor fit, and close to 1 a good fit.
See [R docs for the details on Summary](http://stat.ethz.ch/R-manual/R-patched/library/stats/html/summary.lm.html).

*)
let summary = R.summary result

summary?``r.squared``.AsScalar()(* output: 
NumericS { Sexp = { ptr = 4649218560n } }*)
(**
Finally, we can directly pass results, which is a R expression,
to R.plot, to produce some fancy charts describing our model:

*)
Graphics.svg 8 4 (fun _ -> R.plot result)(* output: 
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
<line x1='77.07' y1='214.56' x2='528.12' y2='214.56' style='stroke-width: 0.75;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='221.76' style='stroke-width: 0.75;' />
<line x1='167.28' y1='214.56' x2='167.28' y2='221.76' style='stroke-width: 0.75;' />
<line x1='257.49' y1='214.56' x2='257.49' y2='221.76' style='stroke-width: 0.75;' />
<line x1='347.70' y1='214.56' x2='347.70' y2='221.76' style='stroke-width: 0.75;' />
<line x1='437.91' y1='214.56' x2='437.91' y2='221.76' style='stroke-width: 0.75;' />
<line x1='528.12' y1='214.56' x2='528.12' y2='221.76' style='stroke-width: 0.75;' />
<text x='77.07' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.0</text>
<text x='167.28' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.1</text>
<text x='257.49' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.2</text>
<text x='347.70' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.3</text>
<text x='437.91' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.4</text>
<text x='528.12' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<line x1='59.04' y1='206.23' x2='59.04' y2='62.81' style='stroke-width: 0.75;' />
<line x1='59.04' y1='206.23' x2='51.84' y2='206.23' style='stroke-width: 0.75;' />
<line x1='59.04' y1='185.74' x2='51.84' y2='185.74' style='stroke-width: 0.75;' />
<line x1='59.04' y1='165.26' x2='51.84' y2='165.26' style='stroke-width: 0.75;' />
<line x1='59.04' y1='144.77' x2='51.84' y2='144.77' style='stroke-width: 0.75;' />
<line x1='59.04' y1='124.28' x2='51.84' y2='124.28' style='stroke-width: 0.75;' />
<line x1='59.04' y1='103.79' x2='51.84' y2='103.79' style='stroke-width: 0.75;' />
<line x1='59.04' y1='83.30' x2='51.84' y2='83.30' style='stroke-width: 0.75;' />
<line x1='59.04' y1='62.81' x2='51.84' y2='62.81' style='stroke-width: 0.75;' />
<text transform='translate(41.76,206.23) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='20.67px' lengthAdjust='spacingAndGlyphs'>-1.5</text>
<text transform='translate(41.76,165.26) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='20.67px' lengthAdjust='spacingAndGlyphs'>-0.5</text>
<text transform='translate(41.76,124.28) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text transform='translate(41.76,83.30) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>1.5</text>
<polygon points='59.04,214.56 545.76,214.56 545.76,59.04 59.04,59.04 ' style='stroke-width: 0.75;' />
<text x='302.40' y='269.28' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='50.03px' lengthAdjust='spacingAndGlyphs'>Leverage</text>
<text transform='translate(12.96,136.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='122.06px' lengthAdjust='spacingAndGlyphs'>Standardized residuals</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<circle cx='392.28' cy='198.87' r='2.70' style='stroke-width: 0.75;' />
<circle cx='357.67' cy='169.54' r='2.70' style='stroke-width: 0.75;' />
<circle cx='315.53' cy='198.41' r='2.70' style='stroke-width: 0.75;' />
<circle cx='431.00' cy='169.26' r='2.70' style='stroke-width: 0.75;' />
<circle cx='234.54' cy='101.66' r='2.70' style='stroke-width: 0.75;' />
<circle cx='436.69' cy='133.90' r='2.70' style='stroke-width: 0.75;' />
<circle cx='178.81' cy='74.73' r='2.70' style='stroke-width: 0.75;' />
<circle cx='373.97' cy='146.28' r='2.70' style='stroke-width: 0.75;' />
<circle cx='228.76' cy='157.97' r='2.70' style='stroke-width: 0.75;' />
<circle cx='527.73' cy='103.75' r='2.70' style='stroke-width: 0.75;' />
<polyline points='178.81,78.77 228.76,122.07 234.54,127.03 315.53,196.27 357.67,175.26 373.97,170.32 392.28,165.57 431.00,155.40 436.69,151.96 527.73,102.65 ' style='stroke-width: 0.75; stroke: #DF536B;' />
<line x1='59.04' y1='144.77' x2='545.76' y2='144.77' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='59.04' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<text x='302.40' y='283.68' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='403.58px' lengthAdjust='spacingAndGlyphs'>(function (formula, data, subset, weights, na.action, method = "qr", model  ...</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<polyline points='81.57,-563.50 86.22,-351.05 90.86,-258.02 95.50,-202.72 100.14,-164.99 104.78,-137.12 109.42,-115.42 114.07,-97.90 118.71,-83.36 123.35,-71.04 127.99,-60.41 132.63,-51.12 137.28,-42.90 141.92,-35.56 146.56,-28.95 151.20,-22.95 155.84,-17.48 160.49,-12.45 165.13,-7.82 169.77,-3.53 174.41,0.47 179.05,4.20 183.69,7.69 188.34,10.97 192.98,14.07 197.62,16.99 202.26,19.75 206.90,22.37 211.55,24.86 216.19,27.24 220.83,29.50 225.47,31.67 230.11,33.74 234.75,35.73 239.40,37.63 244.04,39.46 248.68,41.23 253.32,42.92 257.96,44.56 262.61,46.14 267.25,47.67 271.89,49.15 276.53,50.58 281.17,51.96 285.82,53.30 290.46,54.61 295.10,55.87 299.74,57.10 304.38,58.30 309.02,59.47 313.67,60.60 318.31,61.70 322.95,62.78 327.59,63.83 332.23,64.86 336.88,65.86 341.52,66.84 346.16,67.80 350.80,68.73 355.44,69.65 360.09,70.54 364.73,71.42 369.37,72.28 374.01,73.12 378.65,73.95 383.29,74.76 387.94,75.56 392.58,76.34 397.22,77.11 401.86,77.86 406.50,78.60 411.15,79.33 415.79,80.04 420.43,80.75 425.07,81.44 429.71,82.12 434.36,82.80 439.00,83.46 443.64,84.11 448.28,84.75 452.92,85.38 457.56,86.01 462.21,86.62 466.85,87.23 471.49,87.83 476.13,88.42 480.77,89.01 485.42,89.58 490.06,90.15 494.70,90.72 499.34,91.27 503.98,91.82 508.63,92.36 513.27,92.90 517.91,93.43 522.55,93.96 527.19,94.48 531.83,94.99 536.48,95.50 541.12,96.01 545.76,96.51 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,853.03 86.22,640.59 90.86,547.56 95.50,492.26 100.14,454.53 104.78,426.65 109.42,404.96 114.07,387.44 118.71,372.90 123.35,360.57 127.99,349.94 132.63,340.65 137.28,332.43 141.92,325.09 146.56,318.48 151.20,312.49 155.84,307.01 160.49,301.99 165.13,297.35 169.77,293.06 174.41,289.07 179.05,285.34 183.69,281.84 188.34,278.56 192.98,275.47 197.62,272.55 202.26,269.79 206.90,267.16 211.55,264.67 216.19,262.30 220.83,260.03 225.47,257.87 230.11,255.80 234.75,253.81 239.40,251.90 244.04,250.07 248.68,248.31 253.32,246.61 257.96,244.97 262.61,243.39 267.25,241.87 271.89,240.39 276.53,238.96 281.17,237.58 285.82,236.23 290.46,234.93 295.10,233.66 299.74,232.43 304.38,231.24 309.02,230.07 313.67,228.94 318.31,227.83 322.95,226.75 327.59,225.70 332.23,224.68 336.88,223.68 341.52,222.70 346.16,221.74 350.80,220.81 355.44,219.89 360.09,218.99 364.73,218.12 369.37,217.26 374.01,216.41 378.65,215.59 383.29,214.77 387.94,213.98 392.58,213.20 397.22,212.43 401.86,211.68 406.50,210.94 411.15,210.21 415.79,209.49 420.43,208.79 425.07,208.09 429.71,207.41 434.36,206.74 439.00,206.08 443.64,205.43 448.28,204.78 452.92,204.15 457.56,203.53 462.21,202.91 466.85,202.30 471.49,201.71 476.13,201.11 480.77,200.53 485.42,199.95 490.06,199.38 494.70,198.82 499.34,198.26 503.98,197.72 508.63,197.17 513.27,196.63 517.91,196.10 522.55,195.58 527.19,195.06 531.83,194.54 536.48,194.03 541.12,193.53 545.76,193.03 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,-856.87 86.22,-556.43 90.86,-424.86 95.50,-346.65 100.14,-293.30 104.78,-253.88 109.42,-223.20 114.07,-198.42 118.71,-177.86 123.35,-160.43 127.99,-145.39 132.63,-132.25 137.28,-120.63 141.92,-110.25 146.56,-100.90 151.20,-92.42 155.84,-84.68 160.49,-77.58 165.13,-71.02 169.77,-64.95 174.41,-59.30 179.05,-54.03 183.69,-49.09 188.34,-44.45 192.98,-40.07 197.62,-35.94 202.26,-32.04 206.90,-28.33 211.55,-24.80 216.19,-21.44 220.83,-18.24 225.47,-15.18 230.11,-12.25 234.75,-9.44 239.40,-6.74 244.04,-4.15 248.68,-1.66 253.32,0.74 257.96,3.05 262.61,5.29 267.25,7.45 271.89,9.54 276.53,11.56 281.17,13.52 285.82,15.42 290.46,17.26 295.10,19.05 299.74,20.79 304.38,22.48 309.02,24.13 313.67,25.74 318.31,27.30 322.95,28.82 327.59,30.31 332.23,31.76 336.88,33.17 341.52,34.56 346.16,35.91 350.80,37.24 355.44,38.53 360.09,39.80 364.73,41.04 369.37,42.26 374.01,43.45 378.65,44.62 383.29,45.76 387.94,46.89 392.58,47.99 397.22,49.08 401.86,50.14 406.50,51.19 411.15,52.22 415.79,53.23 420.43,54.23 425.07,55.21 429.71,56.18 434.36,57.13 439.00,58.06 443.64,58.98 448.28,59.89 452.92,60.79 457.56,61.67 462.21,62.54 466.85,63.40 471.49,64.25 476.13,65.08 480.77,65.91 485.42,66.72 490.06,67.53 494.70,68.33 499.34,69.11 503.98,69.89 508.63,70.66 513.27,71.42 517.91,72.17 522.55,72.91 527.19,73.65 531.83,74.38 536.48,75.10 541.12,75.81 545.76,76.52 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,1146.41 86.22,845.96 90.86,714.40 95.50,636.19 100.14,582.83 104.78,543.41 109.42,512.73 114.07,487.96 118.71,467.39 123.35,449.96 127.99,434.93 132.63,421.79 137.28,410.17 141.92,399.78 146.56,390.44 151.20,381.96 155.84,374.22 160.49,367.11 165.13,360.56 169.77,354.49 174.41,348.84 179.05,343.56 183.69,338.62 188.34,333.98 192.98,329.61 197.62,325.48 202.26,321.57 206.90,317.86 211.55,314.34 216.19,310.98 220.83,307.78 225.47,304.71 230.11,301.78 234.75,298.98 239.40,296.28 244.04,293.69 248.68,291.20 253.32,288.80 257.96,286.48 262.61,284.25 267.25,282.09 271.89,280.00 276.53,277.98 281.17,276.02 285.82,274.12 290.46,272.27 295.10,270.48 299.74,268.74 304.38,267.05 309.02,265.40 313.67,263.80 318.31,262.24 322.95,260.71 327.59,259.23 332.23,257.78 336.88,256.36 341.52,254.98 346.16,253.62 350.80,252.30 355.44,251.01 360.09,249.74 364.73,248.50 369.37,247.28 374.01,246.09 378.65,244.92 383.29,243.77 387.94,242.65 392.58,241.54 397.22,240.46 401.86,239.39 406.50,238.34 411.15,237.31 415.79,236.30 420.43,235.31 425.07,234.32 429.71,233.36 434.36,232.41 439.00,231.47 443.64,230.55 448.28,229.64 452.92,228.75 457.56,227.87 462.21,227.00 466.85,226.14 471.49,225.29 476.13,224.45 480.77,223.63 485.42,222.81 490.06,222.01 494.70,221.21 499.34,220.42 503.98,219.65 508.63,218.88 513.27,218.12 517.91,217.37 522.55,216.62 527.19,215.89 531.83,215.16 536.48,214.44 541.12,213.72 545.76,213.02 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<line x1='69.84' y1='206.46' x2='91.44' y2='206.46' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<text x='94.14' y='210.76' style='font-size: 12.00px;fill: #9E9E9E; font-family: "Arial";' textLength='84.33px' lengthAdjust='spacingAndGlyphs'>Cook's distance</text>
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<line x1='545.76' y1='213.02' x2='545.76' y2='76.52' style='stroke-width: 0.75;' />
<line x1='545.76' y1='213.02' x2='545.76' y2='213.02' style='stroke-width: 0.75;' />
<line x1='545.76' y1='193.03' x2='545.76' y2='193.03' style='stroke-width: 0.75;' />
<line x1='545.76' y1='96.51' x2='545.76' y2='96.51' style='stroke-width: 0.75;' />
<line x1='545.76' y1='76.52' x2='545.76' y2='76.52' style='stroke-width: 0.75;' />
<text x='549.36' y='216.24' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='549.36' y='196.25' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='99.73' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='79.74' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='302.40' y='52.56' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='121.39px' lengthAdjust='spacingAndGlyphs'>Residuals vs Leverage</text>
<text x='524.13' y='105.90' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='10.00px' lengthAdjust='spacingAndGlyphs'>10</text>
<text x='388.68' y='203.88' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='311.93' y='203.42' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>3</text>
</g>
</g>
</svg>
*)
(**
That's it - while simple, we hope this example illustrate
how you would go about to use any existing R statistical package.
While the details would differ, the general approach would
remain the same. Happy modelling!

*)