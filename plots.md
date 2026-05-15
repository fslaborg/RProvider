# Creating Plots and Graphics

One of the compelling features of R is its ability to create beautiful plots.
With the R Type Provider, you can use all of R capabilities from F#,
and create simple plots quickly to explore and visualize your data on-the-fly,
as well as generate publication quality graphics that can be exported to virtually any format.

## Basic R plots

Basic plots can be found in the graphics package.
Assuming you are using an F# script,
you can reference the required libraries and packages this way:

```fsharp
#r "nuget: RProvider,2.0.2"

```

```fsharp
open RProvider
open RProvider.graphics
```

RProvider includes a `Graphics` module for capturing R plots.
You may also use many (but not all) R graphics devices directly.
See [graphics](graphics.html) for more details.

The primary helper function is for outputting vector-based non-interactive
plots as svg graphics. Wrap your plot-producing code in a function within `Graphics.svg` as below:

```fsharp
let widgets = [ 3; 8; 12; 15; 19; 18; 18; 20; ]

Graphics.svg 7 4 (fun _ -> R.plot widgets)
```

<?xml version='1.0' encoding='UTF-8' ?>
<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='504.00pt' height='288.00pt' viewBox='0 0 504.00 288.00'>
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
  <clipPath id='cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA='>
    <rect x='0.00' y='0.00' width='504.00' height='288.00' />
  </clipPath>
</defs>
<g clip-path='url(#cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA=)'>
</g>
<defs>
  <clipPath id='cpNTkuMDR8NDczLjc2fDU5LjA0fDIxNC41Ng=='>
    <rect x='59.04' y='59.04' width='414.72' height='155.52' />
  </clipPath>
</defs>
<g clip-path='url(#cpNTkuMDR8NDczLjc2fDU5LjA0fDIxNC41Ng==)'>
<circle cx='74.40' cy='208.80' r='2.70' style='stroke-width: 0.75;' />
<circle cx='129.26' cy='166.45' r='2.70' style='stroke-width: 0.75;' />
<circle cx='184.11' cy='132.56' r='2.70' style='stroke-width: 0.75;' />
<circle cx='238.97' cy='107.15' r='2.70' style='stroke-width: 0.75;' />
<circle cx='293.83' cy='73.27' r='2.70' style='stroke-width: 0.75;' />
<circle cx='348.69' cy='81.74' r='2.70' style='stroke-width: 0.75;' />
<circle cx='403.54' cy='81.74' r='2.70' style='stroke-width: 0.75;' />
<circle cx='458.40' cy='64.80' r='2.70' style='stroke-width: 0.75;' />
</g>
<g clip-path='url(#cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA=)'>
<line x1='74.40' y1='214.56' x2='458.40' y2='214.56' style='stroke-width: 0.75;' />
<line x1='74.40' y1='214.56' x2='74.40' y2='221.76' style='stroke-width: 0.75;' />
<line x1='129.26' y1='214.56' x2='129.26' y2='221.76' style='stroke-width: 0.75;' />
<line x1='184.11' y1='214.56' x2='184.11' y2='221.76' style='stroke-width: 0.75;' />
<line x1='238.97' y1='214.56' x2='238.97' y2='221.76' style='stroke-width: 0.75;' />
<line x1='293.83' y1='214.56' x2='293.83' y2='221.76' style='stroke-width: 0.75;' />
<line x1='348.69' y1='214.56' x2='348.69' y2='221.76' style='stroke-width: 0.75;' />
<line x1='403.54' y1='214.56' x2='403.54' y2='221.76' style='stroke-width: 0.75;' />
<line x1='458.40' y1='214.56' x2='458.40' y2='221.76' style='stroke-width: 0.75;' />
<text x='74.40' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='129.26' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>2</text>
<text x='184.11' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>3</text>
<text x='238.97' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>4</text>
<text x='293.83' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>5</text>
<text x='348.69' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>6</text>
<text x='403.54' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>7</text>
<text x='458.40' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>8</text>
<line x1='59.04' y1='191.86' x2='59.04' y2='64.80' style='stroke-width: 0.75;' />
<line x1='59.04' y1='191.86' x2='51.84' y2='191.86' style='stroke-width: 0.75;' />
<line x1='59.04' y1='149.51' x2='51.84' y2='149.51' style='stroke-width: 0.75;' />
<line x1='59.04' y1='107.15' x2='51.84' y2='107.15' style='stroke-width: 0.75;' />
<line x1='59.04' y1='64.80' x2='51.84' y2='64.80' style='stroke-width: 0.75;' />
<text transform='translate(41.76,191.86) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>5</text>
<text transform='translate(41.76,149.51) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>10</text>
<text transform='translate(41.76,107.15) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>15</text>
<text transform='translate(41.76,64.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>20</text>
<polygon points='59.04,214.56 473.76,214.56 473.76,59.04 59.04,59.04 ' style='stroke-width: 0.75;' />
<text x='266.40' y='269.28' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='29.34px' lengthAdjust='spacingAndGlyphs'>Index</text>
<text transform='translate(12.96,136.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='207.38px' lengthAdjust='spacingAndGlyphs'>c(3L, 8L, 12L, 15L, 19L, 18L, 18L, 20L)</text>
</g>
</g>
</svg>

```fsharp
Graphics.svg 7 4 (fun _ -> R.barplot widgets)
```

<?xml version='1.0' encoding='UTF-8' ?>
<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='504.00pt' height='288.00pt' viewBox='0 0 504.00 288.00'>
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
  <clipPath id='cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA='>
    <rect x='0.00' y='0.00' width='504.00' height='288.00' />
  </clipPath>
</defs>
<g clip-path='url(#cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA=)'>
<rect x='74.40' y='189.92' width='40.85' height='23.10' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='123.42' y='151.43' width='40.85' height='61.59' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='172.44' y='120.63' width='40.85' height='92.39' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='221.46' y='97.54' width='40.85' height='115.49' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='270.49' y='66.74' width='40.85' height='146.28' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='319.51' y='74.44' width='40.85' height='138.58' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='368.53' y='74.44' width='40.85' height='138.58' style='stroke-width: 0.75; fill: #BEBEBE;' />
<rect x='417.55' y='59.04' width='40.85' height='153.98' style='stroke-width: 0.75; fill: #BEBEBE;' />
<line x1='59.04' y1='213.02' x2='59.04' y2='59.04' style='stroke-width: 0.75;' />
<line x1='59.04' y1='213.02' x2='51.84' y2='213.02' style='stroke-width: 0.75;' />
<line x1='59.04' y1='174.53' x2='51.84' y2='174.53' style='stroke-width: 0.75;' />
<line x1='59.04' y1='136.03' x2='51.84' y2='136.03' style='stroke-width: 0.75;' />
<line x1='59.04' y1='97.54' x2='51.84' y2='97.54' style='stroke-width: 0.75;' />
<line x1='59.04' y1='59.04' x2='51.84' y2='59.04' style='stroke-width: 0.75;' />
<text transform='translate(41.76,213.02) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>0</text>
<text transform='translate(41.76,174.53) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>5</text>
<text transform='translate(41.76,136.03) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>10</text>
<text transform='translate(41.76,97.54) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>15</text>
<text transform='translate(41.76,59.04) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>20</text>
</g>
</g>
</svg>

## Using ggplot2

ggplot2 is an R package that expresses the grammar of graphics.
Using RProvider, we can plot F# data in publication-quality plots
using ggplot2.

```fsharp
open RProvider.ggplot2
open RProvider.datasets

let (++) a b = R.ggplot__add (a,b)

Graphics.svg 7 4 (fun _ ->
    R.ggplot(R.mtcars, R.aes(x = "mpg", y = "disp")) ++
    R.geom__point()
)
```

```
No value returned by any evaluator
```

## Exporting and Saving Charts

The RProvider Graphics.svg callback (above) may be used to generate
svg graphics.

Alternatively, you may use R graphics devices directly and manipulate
them through R functions.

**Note. Some graphics devices are not happy running when R is embedded in another process like RProvider.**
For example, on macOS calling Quartz will crash the process, as it will not run outside of the main thread of a process.
X11 is more stable on macOS.

An example is shown below of using the PNG device.

```fsharp
open RProvider.grDevices

// Open the device and create the file as a png.
// R.bmp, R.jpeg, R.pdf, ... will generate other formats.
R.png(filename = "test.png", height = 200, width = 300, bg = "white")
// Create the chart into the file
R.barplot widgets
// Close the device once the chart is complete
R.dev_off ()
```

## R plot arguments

Named parameters allow you to specify every argument supported by R,
as an list of label and value tuples.

An example of using named arguments is below.

```fsharp
open RProvider.Operators

let sprokets = [ 5.3; 6.5; 1.2; 5.3; 4.; 18.; 15.2; 12.1 ]

Graphics.svg 7 4 (fun _ ->
    R.plot [
        "x" => widgets
        "type" => "o"
        "col" => "blue"
        "ylim" => [0; 25] ] |> ignore
    R.lines [
        "x" => sprokets
        "type" => "o"
        "pch" => 22
        "lty" => 2
        "col" => "red" ]
)
```

<?xml version='1.0' encoding='UTF-8' ?>
<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='504.00pt' height='288.00pt' viewBox='0 0 504.00 288.00'>
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
  <clipPath id='cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA='>
    <rect x='0.00' y='0.00' width='504.00' height='288.00' />
  </clipPath>
</defs>
<g clip-path='url(#cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA=)'>
</g>
<defs>
  <clipPath id='cpNTkuMDR8NDczLjc2fDU5LjA0fDIxNC41Ng=='>
    <rect x='59.04' y='59.04' width='414.72' height='155.52' />
  </clipPath>
</defs>
<g clip-path='url(#cpNTkuMDR8NDczLjc2fDU5LjA0fDIxNC41Ng==)'>
<polyline points='74.40,191.52 129.26,162.72 184.11,139.68 238.97,122.40 293.83,99.36 348.69,105.12 403.54,105.12 458.40,93.60 ' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='74.40' cy='191.52' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='129.26' cy='162.72' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='184.11' cy='139.68' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='238.97' cy='122.40' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='293.83' cy='99.36' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='348.69' cy='105.12' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='403.54' cy='105.12' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
<circle cx='458.40' cy='93.60' r='2.70' style='stroke-width: 0.75; stroke: #0000FF;' />
</g>
<g clip-path='url(#cpMC4wMHw1MDQuMDB8MC4wMHwyODguMDA=)'>
<line x1='74.40' y1='214.56' x2='458.40' y2='214.56' style='stroke-width: 0.75;' />
<line x1='74.40' y1='214.56' x2='74.40' y2='221.76' style='stroke-width: 0.75;' />
<line x1='129.26' y1='214.56' x2='129.26' y2='221.76' style='stroke-width: 0.75;' />
<line x1='184.11' y1='214.56' x2='184.11' y2='221.76' style='stroke-width: 0.75;' />
<line x1='238.97' y1='214.56' x2='238.97' y2='221.76' style='stroke-width: 0.75;' />
<line x1='293.83' y1='214.56' x2='293.83' y2='221.76' style='stroke-width: 0.75;' />
<line x1='348.69' y1='214.56' x2='348.69' y2='221.76' style='stroke-width: 0.75;' />
<line x1='403.54' y1='214.56' x2='403.54' y2='221.76' style='stroke-width: 0.75;' />
<line x1='458.40' y1='214.56' x2='458.40' y2='221.76' style='stroke-width: 0.75;' />
<text x='74.40' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>1</text>
<text x='129.26' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>2</text>
<text x='184.11' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>3</text>
<text x='238.97' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>4</text>
<text x='293.83' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>5</text>
<text x='348.69' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>6</text>
<text x='403.54' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>7</text>
<text x='458.40' y='240.48' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>8</text>
<line x1='59.04' y1='208.80' x2='59.04' y2='64.80' style='stroke-width: 0.75;' />
<line x1='59.04' y1='208.80' x2='51.84' y2='208.80' style='stroke-width: 0.75;' />
<line x1='59.04' y1='180.00' x2='51.84' y2='180.00' style='stroke-width: 0.75;' />
<line x1='59.04' y1='151.20' x2='51.84' y2='151.20' style='stroke-width: 0.75;' />
<line x1='59.04' y1='122.40' x2='51.84' y2='122.40' style='stroke-width: 0.75;' />
<line x1='59.04' y1='93.60' x2='51.84' y2='93.60' style='stroke-width: 0.75;' />
<line x1='59.04' y1='64.80' x2='51.84' y2='64.80' style='stroke-width: 0.75;' />
<text transform='translate(41.76,208.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>0</text>
<text transform='translate(41.76,180.00) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='6.67px' lengthAdjust='spacingAndGlyphs'>5</text>
<text transform='translate(41.76,151.20) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>10</text>
<text transform='translate(41.76,122.40) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>15</text>
<text transform='translate(41.76,93.60) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>20</text>
<text transform='translate(41.76,64.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='13.34px' lengthAdjust='spacingAndGlyphs'>25</text>
<polygon points='59.04,214.56 473.76,214.56 473.76,59.04 59.04,59.04 ' style='stroke-width: 0.75;' />
<text x='266.40' y='269.28' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='29.34px' lengthAdjust='spacingAndGlyphs'>Index</text>
<text transform='translate(12.96,136.80) rotate(-90)' text-anchor='middle' style='font-size: 12.00px; font-family: "Arial";' textLength='207.38px' lengthAdjust='spacingAndGlyphs'>c(3L, 8L, 12L, 15L, 19L, 18L, 18L, 20L)</text>
</g>
<g clip-path='url(#cpNTkuMDR8NDczLjc2fDU5LjA0fDIxNC41Ng==)'>
<polyline points='74.40,178.27 129.26,171.36 184.11,201.89 238.97,178.27 293.83,185.76 348.69,105.12 403.54,121.25 458.40,139.10 ' style='stroke-width: 0.75; stroke: #FF0000; stroke-dasharray: 4.00,4.00;' />
<rect x='72.01' y='175.88' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='126.86' y='168.97' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='181.72' y='199.50' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='236.58' y='175.88' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='291.44' y='183.37' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='346.29' y='102.73' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='401.15' y='118.86' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
<rect x='456.01' y='136.71' width='4.79' height='4.79' style='stroke-width: 0.75; stroke: #FF0000;' />
</g>
</g>
</svg>
