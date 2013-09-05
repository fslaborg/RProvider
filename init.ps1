param($installPath, $toolsPath, $package)

$destPath = $installPath + "\lib\"
Foreach($p in $package.DependencySets.Dependencies)
{
    $path = $installPath + "\..\" + ($p.Id) + "." + ($p.VersionSpec) + "\lib\" 
    $files = Get-ChildItem -Recurse $path | where {$_.PSIsContainer -eq $False}
    Foreach ($file in $files)   
    {  
        Copy-Item $file.FullName ($destPath + $file.Name) -Force
    }  
}