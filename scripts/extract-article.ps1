# Extract CIL View manual HTML from docfx output
$inputPath = '..\docs\articles\cilview-manual.html'
$srcPath = '..\CilView.Core\Documentation\DocsRewriter.cs'

echo "Compiling C#..."
Add-Type -Path $srcPath
echo "Extracting article..."
[CilView.Core.Documentation.DocsRewriter]::ExtractArticle($inputPath,'manual.html')

echo "Finished"
[void][System.Console]::ReadKey($true)
