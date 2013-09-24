# Nuget gives us some information in the following variables
param($installPath, $toolsPath, $package)

# Destination "RProvider-X.Y\lib" (where RProivder.dll lives)
$destPath = $installPath + "\lib\"

# Get dependencies - one would expect that we can use $package.DependencySets.Dependencies
# but that does not seem to work. So just get RDotNet and R.NET folders in the packages root..
$deps = Get-ChildItem -Path ($installPath + "\..") | Where-Object { $_.Name.Contains("RDotNet") -or  $_.Name.Contains("R.NET") }
Foreach($d in $deps)
{ 
    # Find files in "<package-name>\lib\net40"
    $files = Get-ChildItem ($d.FullName + "\lib\net40") | where {$_.PSIsContainer -eq $False}
    Foreach ($file in $files)
    {  
        # This gets executed each time project is loaded, so skip files if they exist already
        Copy-Item $file.FullName ($destPath + $file.Name) -Force -ErrorAction SilentlyContinue
    }  
}