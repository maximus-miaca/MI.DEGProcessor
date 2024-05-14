Param (
    [String]$path,
    [String]$version,
    [String]$copyPath
)

$folder = Split-Path $path -Leaf
$parent = Split-Path $path -Parent

$versionParts = $version.Split(".")
$version3 = $versionParts[0] + "_" + $versionParts[1] + "_" + $versionParts[2]

$sourcepath = $path + "*"
$outpath = $parent + "\" + $folder + "_" + $version3 + ".zip"

Compress-Archive -Path "$sourcepath" -DestinationPath "$outpath" -Force

if (![string]::isNullOrEmpty($copyPath)) {
    $copyOutpath = $copyPath + "\" + $folder
    copy $outpath $copyOutpath
}