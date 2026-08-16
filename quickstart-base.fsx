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
NumericS { Sexp = { ptr = 47319240704n } }*)
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
<line x1='77.07' y1='214.56' x2='454.94' y2='214.56' style='stroke-width: 0.75;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='221.76' style='stroke-width: 0.75;' />
<line x1='171.53' y1='214.56' x2='171.53' y2='221.76' style='stroke-width: 0.75;' />
<line x1='266.00' y1='214.56' x2='266.00' y2='221.76' style='stroke-width: 0.75;' />
<line x1='360.47' y1='214.56' x2='360.47' y2='221.76' style='stroke-width: 0.75;' />
<line x1='454.94' y1='214.56' x2='454.94' y2='221.76' style='stroke-width: 0.75;' />
<text x='77.07' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.0</text>
<text x='171.53' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.1</text>
<text x='266.00' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.2</text>
<text x='360.47' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.3</text>
<text x='454.94' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.4</text>
<line x1='59.04' y1='210.52' x2='59.04' y2='71.46' style='stroke-width: 0.75;' />
<line x1='59.04' y1='210.52' x2='51.84' y2='210.52' style='stroke-width: 0.75;' />
<line x1='59.04' y1='175.76' x2='51.84' y2='175.76' style='stroke-width: 0.75;' />
<line x1='59.04' y1='140.99' x2='51.84' y2='140.99' style='stroke-width: 0.75;' />
<line x1='59.04' y1='106.23' x2='51.84' y2='106.23' style='stroke-width: 0.75;' />
<line x1='59.04' y1='71.46' x2='51.84' y2='71.46' style='stroke-width: 0.75;' />
<text transform='translate(41.76,210.52) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='10.67px' lengthAdjust='spacingAndGlyphs'>-2</text>
<text transform='translate(41.76,175.76) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='10.67px' lengthAdjust='spacingAndGlyphs'>-1</text>
<text transform='translate(41.76,140.99) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>0</text>
<text transform='translate(41.76,106.23) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>1</text>
<text transform='translate(41.76,71.46) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>2</text>
<polygon points='59.04,214.56 545.76,214.56 545.76,59.04 59.04,59.04 ' style='stroke-width: 0.75;' />
<text x='302.40' y='269.28' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='50.03px' lengthAdjust='spacingAndGlyphs'>Leverage</text>
<text transform='translate(12.96,136.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='122.06px' lengthAdjust='spacingAndGlyphs'>Standardized residuals</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<circle cx='527.73' cy='142.92' r='2.70' style='stroke-width: 0.75;' />
<circle cx='453.77' cy='136.83' r='2.70' style='stroke-width: 0.75;' />
<circle cx='303.93' cy='101.90' r='2.70' style='stroke-width: 0.75;' />
<circle cx='308.45' cy='100.06' r='2.70' style='stroke-width: 0.75;' />
<circle cx='210.87' cy='160.02' r='2.70' style='stroke-width: 0.75;' />
<circle cx='441.98' cy='159.85' r='2.70' style='stroke-width: 0.75;' />
<circle cx='259.42' cy='153.59' r='2.70' style='stroke-width: 0.75;' />
<circle cx='177.36' cy='171.55' r='2.70' style='stroke-width: 0.75;' />
<circle cx='522.90' cy='74.73' r='2.70' style='stroke-width: 0.75;' />
<circle cx='398.26' cy='198.87' r='2.70' style='stroke-width: 0.75;' />
<polyline points='177.36,177.09 210.87,160.14 259.42,134.50 303.93,121.34 308.45,124.96 398.26,151.78 441.98,157.96 453.77,150.18 522.90,107.44 527.73,104.57 ' style='stroke-width: 0.75; stroke: #DF536B;' />
<line x1='59.04' y1='140.99' x2='545.76' y2='140.99' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='59.04' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<text x='302.40' y='283.68' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='403.58px' lengthAdjust='spacingAndGlyphs'>(function (formula, data, subset, weights, na.action, method = "qr", model  ...</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<polyline points='81.57,-473.97 86.22,-289.56 90.86,-208.82 95.50,-160.83 100.14,-128.09 104.78,-103.90 109.42,-85.09 114.07,-69.89 118.71,-57.28 123.35,-46.59 127.99,-37.38 132.63,-29.32 137.28,-22.19 141.92,-15.83 146.56,-10.10 151.20,-4.91 155.84,-0.17 160.49,4.18 165.13,8.20 169.77,11.92 174.41,15.38 179.05,18.60 183.69,21.63 188.34,24.47 192.98,27.14 197.62,29.67 202.26,32.06 206.90,34.33 211.55,36.49 216.19,38.54 220.83,40.50 225.47,42.37 230.11,44.16 234.75,45.87 239.40,47.52 244.04,49.10 248.68,50.63 253.32,52.09 257.96,53.51 262.61,54.87 267.25,56.19 271.89,57.46 276.53,58.70 281.17,59.89 285.82,61.05 290.46,62.17 295.10,63.26 299.74,64.32 304.38,65.36 309.02,66.36 313.67,67.34 318.31,68.29 322.95,69.22 327.59,70.12 332.23,71.00 336.88,71.86 341.52,72.71 346.16,73.53 350.80,74.33 355.44,75.12 360.09,75.89 364.73,76.65 369.37,77.38 374.01,78.11 378.65,78.82 383.29,79.51 387.94,80.20 392.58,80.87 397.22,81.53 401.86,82.17 406.50,82.81 411.15,83.43 415.79,84.04 420.43,84.65 425.07,85.24 429.71,85.83 434.36,86.40 439.00,86.97 443.64,87.52 448.28,88.07 452.92,88.61 457.56,89.15 462.21,89.67 466.85,90.19 471.49,90.70 476.13,91.21 480.77,91.71 485.42,92.20 490.06,92.68 494.70,93.16 499.34,93.64 503.98,94.10 508.63,94.57 513.27,95.02 517.91,95.47 522.55,95.92 527.19,96.36 531.83,96.80 536.48,97.23 541.12,97.66 545.76,98.09 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,755.95 86.22,571.54 90.86,490.80 95.50,442.81 100.14,410.07 104.78,385.89 109.42,367.07 114.07,351.87 118.71,339.26 123.35,328.57 127.99,319.36 132.63,311.30 137.28,304.18 141.92,297.82 146.56,292.09 151.20,286.89 155.84,282.15 160.49,277.80 165.13,273.78 169.77,270.07 174.41,266.61 179.05,263.38 183.69,260.36 188.34,257.51 192.98,254.84 197.62,252.31 202.26,249.92 206.90,247.65 211.55,245.50 216.19,243.44 220.83,241.49 225.47,239.61 230.11,237.82 234.75,236.11 239.40,234.46 244.04,232.88 248.68,231.36 253.32,229.89 257.96,228.48 262.61,227.11 267.25,225.80 271.89,224.52 276.53,223.29 281.17,222.09 285.82,220.93 290.46,219.81 295.10,218.72 299.74,217.66 304.38,216.63 309.02,215.62 313.67,214.65 318.31,213.70 322.95,212.77 327.59,211.86 332.23,210.98 336.88,210.12 341.52,209.28 346.16,208.45 350.80,207.65 355.44,206.86 360.09,206.09 364.73,205.34 369.37,204.60 374.01,203.87 378.65,203.16 383.29,202.47 387.94,201.79 392.58,201.12 397.22,200.46 401.86,199.81 406.50,199.18 411.15,198.55 415.79,197.94 420.43,197.34 425.07,196.74 429.71,196.16 434.36,195.58 439.00,195.02 443.64,194.46 448.28,193.91 452.92,193.37 457.56,192.84 462.21,192.31 466.85,191.79 471.49,191.28 476.13,190.78 480.77,190.28 485.42,189.79 490.06,189.30 494.70,188.82 499.34,188.35 503.98,187.88 508.63,187.42 513.27,186.96 517.91,186.51 522.55,186.06 527.19,185.62 531.83,185.18 536.48,184.75 541.12,184.32 545.76,183.90 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,-728.70 86.22,-467.90 90.86,-353.71 95.50,-285.84 100.14,-239.55 104.78,-205.34 109.42,-178.73 114.07,-157.24 118.71,-139.41 123.35,-124.29 127.99,-111.26 132.63,-99.86 137.28,-89.79 141.92,-80.79 146.56,-72.69 151.20,-65.34 155.84,-58.64 160.49,-52.48 165.13,-46.81 169.77,-41.55 174.41,-36.66 179.05,-32.09 183.69,-27.81 188.34,-23.80 192.98,-20.01 197.62,-16.44 202.26,-13.06 206.90,-9.85 211.55,-6.80 216.19,-3.90 220.83,-1.13 225.47,1.52 230.11,4.05 234.75,6.48 239.40,8.80 244.04,11.04 248.68,13.20 253.32,15.27 257.96,17.27 262.61,19.20 267.25,21.06 271.89,22.86 276.53,24.61 281.17,26.30 285.82,27.94 290.46,29.53 295.10,31.07 299.74,32.57 304.38,34.03 309.02,35.45 313.67,36.83 318.31,38.17 322.95,39.48 327.59,40.76 332.23,42.01 336.88,43.23 341.52,44.42 346.16,45.59 350.80,46.72 355.44,47.84 360.09,48.93 364.73,49.99 369.37,51.04 374.01,52.06 378.65,53.07 383.29,54.05 387.94,55.02 392.58,55.96 397.22,56.89 401.86,57.81 406.50,58.71 411.15,59.59 415.79,60.46 420.43,61.31 425.07,62.15 429.71,62.97 434.36,63.79 439.00,64.59 443.64,65.38 448.28,66.15 452.92,66.92 457.56,67.67 462.21,68.42 466.85,69.15 471.49,69.87 476.13,70.59 480.77,71.29 485.42,71.99 490.06,72.67 494.70,73.35 499.34,74.02 503.98,74.68 508.63,75.34 513.27,75.98 517.91,76.62 522.55,77.25 527.19,77.88 531.83,78.50 536.48,79.11 541.12,79.71 545.76,80.31 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,1010.68 86.22,749.88 90.86,635.70 95.50,567.83 100.14,521.53 104.78,487.33 109.42,460.71 114.07,439.22 118.71,421.39 123.35,406.27 127.99,393.24 132.63,381.85 137.28,371.77 141.92,362.77 146.56,354.67 151.20,347.33 155.84,340.62 160.49,334.47 165.13,328.79 169.77,323.53 174.41,318.64 179.05,314.07 183.69,309.80 188.34,305.78 192.98,302.00 197.62,298.42 202.26,295.04 206.90,291.83 211.55,288.79 216.19,285.88 220.83,283.11 225.47,280.47 230.11,277.93 234.75,275.51 239.40,273.18 244.04,270.94 248.68,268.79 253.32,266.71 257.96,264.72 262.61,262.79 267.25,260.92 271.89,259.12 276.53,257.38 281.17,255.69 285.82,254.05 290.46,252.46 295.10,250.91 299.74,249.41 304.38,247.96 309.02,246.54 313.67,245.16 318.31,243.81 322.95,242.50 327.59,241.22 332.23,239.97 336.88,238.75 341.52,237.56 346.16,236.40 350.80,235.26 355.44,234.15 360.09,233.06 364.73,231.99 369.37,230.95 374.01,229.92 378.65,228.92 383.29,227.93 387.94,226.97 392.58,226.02 397.22,225.09 401.86,224.17 406.50,223.28 411.15,222.39 415.79,221.53 420.43,220.67 425.07,219.83 429.71,219.01 434.36,218.20 439.00,217.40 443.64,216.61 448.28,215.83 452.92,215.07 457.56,214.31 462.21,213.57 466.85,212.83 471.49,212.11 476.13,211.40 480.77,210.69 485.42,210.00 490.06,209.31 494.70,208.63 499.34,207.96 503.98,207.30 508.63,206.65 513.27,206.00 517.91,205.36 522.55,204.73 527.19,204.11 531.83,203.49 536.48,202.88 541.12,202.27 545.76,201.67 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<line x1='69.84' y1='206.46' x2='91.44' y2='206.46' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<text x='94.14' y='210.76' style='font-size: 12.00px;fill: #9E9E9E; font-family: "Arial";' textLength='84.33px' lengthAdjust='spacingAndGlyphs'>Cook's distance</text>
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<line x1='545.76' y1='201.67' x2='545.76' y2='80.31' style='stroke-width: 0.75;' />
<line x1='545.76' y1='201.67' x2='545.76' y2='201.67' style='stroke-width: 0.75;' />
<line x1='545.76' y1='183.90' x2='545.76' y2='183.90' style='stroke-width: 0.75;' />
<line x1='545.76' y1='98.09' x2='545.76' y2='98.09' style='stroke-width: 0.75;' />
<line x1='545.76' y1='80.31' x2='545.76' y2='80.31' style='stroke-width: 0.75;' />
<text x='549.36' y='204.89' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='549.36' y='187.12' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='101.31' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='83.53' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='302.40' y='52.56' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='121.39px' lengthAdjust='spacingAndGlyphs'>Residuals vs Leverage</text>
<text x='519.30' y='76.88' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>9</text>
<text x='394.66' y='203.88' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='10.00px' lengthAdjust='spacingAndGlyphs'>10</text>
<text x='304.85' y='102.21' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>4</text>
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