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
NumericS { Sexp = { ptr = 4628391712n } }
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
<line x1='77.07' y1='214.56' x2='500.43' y2='214.56' style='stroke-width: 0.75;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='221.76' style='stroke-width: 0.75;' />
<line x1='161.74' y1='214.56' x2='161.74' y2='221.76' style='stroke-width: 0.75;' />
<line x1='246.41' y1='214.56' x2='246.41' y2='221.76' style='stroke-width: 0.75;' />
<line x1='331.08' y1='214.56' x2='331.08' y2='221.76' style='stroke-width: 0.75;' />
<line x1='415.75' y1='214.56' x2='415.75' y2='221.76' style='stroke-width: 0.75;' />
<line x1='500.43' y1='214.56' x2='500.43' y2='221.76' style='stroke-width: 0.75;' />
<text x='77.07' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.0</text>
<text x='161.74' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.1</text>
<text x='246.41' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.2</text>
<text x='331.08' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.3</text>
<text x='415.75' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.4</text>
<text x='500.43' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='16.67px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<line x1='59.04' y1='194.75' x2='59.04' y2='78.04' style='stroke-width: 0.75;' />
<line x1='59.04' y1='194.75' x2='51.84' y2='194.75' style='stroke-width: 0.75;' />
<line x1='59.04' y1='155.84' x2='51.84' y2='155.84' style='stroke-width: 0.75;' />
<line x1='59.04' y1='116.94' x2='51.84' y2='116.94' style='stroke-width: 0.75;' />
<line x1='59.04' y1='78.04' x2='51.84' y2='78.04' style='stroke-width: 0.75;' />
<text transform='translate(41.76,194.75) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='10.67px' lengthAdjust='spacingAndGlyphs'>-2</text>
<text transform='translate(41.76,155.84) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='10.67px' lengthAdjust='spacingAndGlyphs'>-1</text>
<text transform='translate(41.76,116.94) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>0</text>
<text transform='translate(41.76,78.04) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>1</text>
<polygon points='59.04,214.56 545.76,214.56 545.76,59.04 59.04,59.04 ' style='stroke-width: 0.75;' />
<text x='302.40' y='269.28' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='50.03px' lengthAdjust='spacingAndGlyphs'>Leverage</text>
<text transform='translate(12.96,136.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='122.06px' lengthAdjust='spacingAndGlyphs'>Standardized residuals</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<circle cx='286.21' cy='128.49' r='2.70' style='stroke-width: 0.75;' />
<circle cx='326.93' cy='74.73' r='2.70' style='stroke-width: 0.75;' />
<circle cx='332.60' cy='198.87' r='2.70' style='stroke-width: 0.75;' />
<circle cx='232.18' cy='103.72' r='2.70' style='stroke-width: 0.75;' />
<circle cx='217.59' cy='79.27' r='2.70' style='stroke-width: 0.75;' />
<circle cx='527.73' cy='145.29' r='2.70' style='stroke-width: 0.75;' />
<circle cx='359.94' cy='167.51' r='2.70' style='stroke-width: 0.75;' />
<circle cx='391.66' cy='89.17' r='2.70' style='stroke-width: 0.75;' />
<circle cx='249.40' cy='101.93' r='2.70' style='stroke-width: 0.75;' />
<circle cx='386.59' cy='89.41' r='2.70' style='stroke-width: 0.75;' />
<polyline points='217.59,84.32 232.18,93.86 249.40,105.15 286.21,127.98 326.93,149.40 332.60,152.84 359.94,166.65 386.59,96.60 391.66,83.33 527.73,144.12 ' style='stroke-width: 0.75; stroke: #DF536B;' />
<line x1='59.04' y1='116.94' x2='545.76' y2='116.94' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
<line x1='77.07' y1='214.56' x2='77.07' y2='59.04' style='stroke-width: 0.75; stroke: #BEBEBE; stroke-dasharray: 1.00,3.00;' />
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<text x='302.40' y='283.68' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='403.58px' lengthAdjust='spacingAndGlyphs'>(function (formula, data, subset, weights, na.action, method = "qr", model  ...</text>
</g>
<g clip-path='url(#cpNTkuMDR8NTQ1Ljc2fDU5LjA0fDIxNC41Ng==)'>
<polyline points='81.57,-534.41 86.22,-338.95 90.86,-253.35 95.50,-202.46 100.14,-167.73 104.78,-142.06 109.42,-122.09 114.07,-105.95 118.71,-92.56 123.35,-81.20 127.99,-71.41 132.63,-62.84 137.28,-55.27 141.92,-48.50 146.56,-42.40 151.20,-36.87 155.84,-31.82 160.49,-27.19 165.13,-22.91 169.77,-18.95 174.41,-15.26 179.05,-11.81 183.69,-8.59 188.34,-5.55 192.98,-2.70 197.62,0.0025 202.26,2.56 206.90,4.98 211.55,7.29 216.19,9.49 220.83,11.58 225.47,13.59 230.11,15.50 234.75,17.34 239.40,19.11 244.04,20.81 248.68,22.44 253.32,24.01 257.96,25.53 262.61,27.00 267.25,28.41 271.89,29.78 276.53,31.11 281.17,32.40 285.82,33.65 290.46,34.86 295.10,36.03 299.74,37.18 304.38,38.29 309.02,39.37 313.67,40.43 318.31,41.46 322.95,42.46 327.59,43.44 332.23,44.40 336.88,45.33 341.52,46.24 346.16,47.13 350.80,48.01 355.44,48.86 360.09,49.70 364.73,50.52 369.37,51.32 374.01,52.11 378.65,52.88 383.29,53.64 387.94,54.39 392.58,55.12 397.22,55.84 401.86,56.54 406.50,57.24 411.15,57.92 415.79,58.59 420.43,59.25 425.07,59.90 429.71,60.54 434.36,61.18 439.00,61.80 443.64,62.41 448.28,63.02 452.92,63.61 457.56,64.20 462.21,64.78 466.85,65.35 471.49,65.92 476.13,66.48 480.77,67.03 485.42,67.57 490.06,68.11 494.70,68.65 499.34,69.17 503.98,69.69 508.63,70.21 513.27,70.72 517.91,71.22 522.55,71.72 527.19,72.22 531.83,72.71 536.48,73.19 541.12,73.67 545.76,74.15 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,768.29 86.22,572.84 90.86,487.23 95.50,436.34 100.14,401.61 104.78,375.94 109.42,355.97 114.07,339.83 118.71,326.44 123.35,315.08 127.99,305.29 132.63,296.72 137.28,289.15 141.92,282.38 146.56,276.28 151.20,270.75 155.84,265.70 160.49,261.07 165.13,256.79 169.77,252.83 174.41,249.14 179.05,245.69 183.69,242.47 188.34,239.44 192.98,236.58 197.62,233.88 202.26,231.32 206.90,228.90 211.55,226.59 216.19,224.40 220.83,222.30 225.47,220.30 230.11,218.38 234.75,216.54 239.40,214.77 244.04,213.08 248.68,211.44 253.32,209.87 257.96,208.35 262.61,206.88 267.25,205.47 271.89,204.10 276.53,202.77 281.17,201.48 285.82,200.24 290.46,199.02 295.10,197.85 299.74,196.70 304.38,195.59 309.02,194.51 313.67,193.45 318.31,192.42 322.95,191.42 327.59,190.44 332.23,189.49 336.88,188.55 341.52,187.64 346.16,186.75 350.80,185.88 355.44,185.02 360.09,184.18 364.73,183.36 369.37,182.56 374.01,181.77 378.65,181.00 383.29,180.24 387.94,179.50 392.58,178.76 397.22,178.05 401.86,177.34 406.50,176.65 411.15,175.96 415.79,175.29 420.43,174.63 425.07,173.98 429.71,173.34 434.36,172.71 439.00,172.08 443.64,171.47 448.28,170.87 452.92,170.27 457.56,169.68 462.21,169.10 466.85,168.53 471.49,167.96 476.13,167.40 480.77,166.85 485.42,166.31 490.06,165.77 494.70,165.24 499.34,164.71 503.98,164.19 508.63,163.67 513.27,163.16 517.91,162.66 522.55,162.16 527.19,161.66 531.83,161.17 536.48,160.69 541.12,160.21 545.76,159.73 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,-804.20 86.22,-527.79 90.86,-406.73 95.50,-334.75 100.14,-285.64 104.78,-249.35 109.42,-221.09 114.07,-198.28 118.71,-179.34 123.35,-163.28 127.99,-149.42 132.63,-137.31 137.28,-126.60 141.92,-117.02 146.56,-108.40 151.20,-100.58 155.84,-93.44 160.49,-86.89 165.13,-80.84 169.77,-75.23 174.41,-70.01 179.05,-65.14 183.69,-60.58 188.34,-56.29 192.98,-52.25 197.62,-48.43 202.26,-44.82 206.90,-41.39 211.55,-38.13 216.19,-35.02 220.83,-32.06 225.47,-29.22 230.11,-26.51 234.75,-23.91 239.40,-21.41 244.04,-19.01 248.68,-16.70 253.32,-14.48 257.96,-12.33 262.61,-10.26 267.25,-8.25 271.89,-6.32 276.53,-4.44 281.17,-2.62 285.82,-0.86 290.46,0.86 295.10,2.52 299.74,4.14 304.38,5.71 309.02,7.24 313.67,8.74 318.31,10.19 322.95,11.61 327.59,12.99 332.23,14.35 336.88,15.67 341.52,16.96 346.16,18.22 350.80,19.45 355.44,20.66 360.09,21.85 364.73,23.00 369.37,24.14 374.01,25.26 378.65,26.35 383.29,27.42 387.94,28.48 392.58,29.51 397.22,30.53 401.86,31.52 406.50,32.51 411.15,33.47 415.79,34.42 420.43,35.36 425.07,36.28 429.71,37.18 434.36,38.08 439.00,38.96 443.64,39.82 448.28,40.68 452.92,41.52 457.56,42.35 462.21,43.17 466.85,43.98 471.49,44.78 476.13,45.57 480.77,46.35 485.42,47.13 490.06,47.89 494.70,48.64 499.34,49.39 503.98,50.12 508.63,50.85 513.27,51.57 517.91,52.29 522.55,52.99 527.19,53.69 531.83,54.39 536.48,55.07 541.12,55.75 545.76,56.43 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<polyline points='81.57,1038.08 86.22,761.67 90.86,640.61 95.50,568.64 100.14,519.52 104.78,483.23 109.42,454.98 114.07,432.16 118.71,413.22 123.35,397.16 127.99,383.31 132.63,371.19 137.28,360.48 141.92,350.91 146.56,342.29 151.20,334.46 155.84,327.32 160.49,320.77 165.13,314.72 169.77,309.11 174.41,303.90 179.05,299.03 183.69,294.46 188.34,290.17 192.98,286.13 197.62,282.32 202.26,278.70 206.90,275.27 211.55,272.01 216.19,268.91 220.83,265.94 225.47,263.11 230.11,260.39 234.75,257.79 239.40,255.30 244.04,252.90 248.68,250.59 253.32,248.36 257.96,246.21 262.61,244.14 267.25,242.14 271.89,240.20 276.53,238.32 281.17,236.50 285.82,234.74 290.46,233.02 295.10,231.36 299.74,229.74 304.38,228.17 309.02,226.64 313.67,225.15 318.31,223.69 322.95,222.27 327.59,220.89 332.23,219.54 336.88,218.22 341.52,216.93 346.16,215.66 350.80,214.43 355.44,213.22 360.09,212.04 364.73,210.88 369.37,209.74 374.01,208.63 378.65,207.53 383.29,206.46 387.94,205.41 392.58,204.37 397.22,203.36 401.86,202.36 406.50,201.38 411.15,200.41 415.79,199.46 420.43,198.53 425.07,197.60 429.71,196.70 434.36,195.81 439.00,194.93 443.64,194.06 448.28,193.20 452.92,192.36 457.56,191.53 462.21,190.71 466.85,189.90 471.49,189.10 476.13,188.31 480.77,187.53 485.42,186.76 490.06,185.99 494.70,185.24 499.34,184.50 503.98,183.76 508.63,183.03 513.27,182.31 517.91,181.60 522.55,180.89 527.19,180.19 531.83,179.50 536.48,178.81 541.12,178.13 545.76,177.46 ' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<line x1='69.84' y1='206.46' x2='91.44' y2='206.46' style='stroke-width: 0.75; stroke: #9E9E9E; stroke-dasharray: 4.00,4.00;' />
<text x='94.14' y='210.76' style='font-size: 12.00px;fill: #9E9E9E; font-family: "Arial";' textLength='84.33px' lengthAdjust='spacingAndGlyphs'>Cook's distance</text>
</g>
<g clip-path='url(#cpMC4wMHw1NzYuMDB8MC4wMHwyODguMDA=)'>
<line x1='545.76' y1='177.46' x2='545.76' y2='59.04' style='stroke-width: 0.75;' />
<line x1='545.76' y1='177.46' x2='545.76' y2='177.46' style='stroke-width: 0.75;' />
<line x1='545.76' y1='159.73' x2='545.76' y2='159.73' style='stroke-width: 0.75;' />
<line x1='545.76' y1='74.15' x2='545.76' y2='74.15' style='stroke-width: 0.75;' />
<text x='549.36' y='180.68' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='549.36' y='162.95' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='549.36' y='77.37' style='font-size: 9.00px;fill: #9E9E9E; font-family: "Arial";' textLength='12.50px' lengthAdjust='spacingAndGlyphs'>0.5</text>
<text x='302.40' y='52.56' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='121.39px' lengthAdjust='spacingAndGlyphs'>Residuals vs Leverage</text>
<text x='329.00' y='203.88' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>3</text>
<text x='356.34' y='172.53' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>7</text>
<text x='524.13' y='150.31' text-anchor='end' style='font-size: 9.00px; font-family: "Arial";' textLength='5.00px' lengthAdjust='spacingAndGlyphs'>6</text>
</g>
</g>
</svg>

That's it - while simple, we hope this example illustrate
how you would go about to use any existing R statistical package.
While the details would differ, the general approach would
remain the same. Happy modelling!
