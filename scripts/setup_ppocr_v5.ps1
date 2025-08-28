param(
    [string]$BasePath = (Resolve-Path "..\").Path
)

$ErrorActionPreference = 'Stop'

function Ensure-Dir($path){ if(!(Test-Path $path)){ New-Item -ItemType Directory -Path $path -Force | Out-Null } }
function Join-Parts([string[]]$parts){ return [System.IO.Path]::Combine($parts) }

# Resolve base paths
$workspace = Resolve-Path $BasePath
$modelsDir = Join-Parts @($workspace, 'models')
$inferenceDir = Join-Parts @($workspace, 'src','MonLingo.Core','bin','x64','Debug','net481','inference')
Ensure-Dir $modelsDir
Ensure-Dir $inferenceDir

Write-Host "Workspace: $workspace"
Write-Host "Models dir: $modelsDir"
Write-Host "Inference dir: $inferenceDir"

# Files to fetch (official BOS links)
$files = @(
    @{ Name = 'ch_PP-OCRv5_det_infer.tar'; Url = 'https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_det_infer.tar' },
    @{ Name = 'ch_PP-OCRv5_rec_infer.tar'; Url = 'https://paddleocr.bj.bcebos.com/PP-OCRv5/chinese/ch_PP-OCRv5_rec_infer.tar' },
    @{ Name = 'ch_ppocr_mobile_v2.0_cls_infer.tar'; Url = 'https://paddleocr.bj.bcebos.com/PP-OCRv2/chinese/ch_ppocr_mobile_v2.0_cls_infer.tar' },
    @{ Name = 'ppocr_keys_v1.txt'; Url = 'https://paddleocr.bj.bcebos.com/PP-OCRv2/ppocr_keys_v1.txt' }
)

# Optional secondary mirrors (HuggingFace) if BOS is blocked
$hf = @{
    'ch_PP-OCRv5_det_infer.tar' = 'https://huggingface.co/PaddlePaddle/PaddleOCR/resolve/main/ppocrv5/ch_PP-OCRv5_det_infer.tar?download=true'
    'ch_PP-OCRv5_rec_infer.tar' = 'https://huggingface.co/PaddlePaddle/PaddleOCR/resolve/main/ppocrv5/ch_PP-OCRv5_rec_infer.tar?download=true'
    'ch_ppocr_mobile_v2.0_cls_infer.tar' = 'https://huggingface.co/PaddlePaddle/PaddleOCR/resolve/main/ch_ppocr_mobile_v2.0_cls_infer.tar?download=true'
    'ppocr_keys_v1.txt' = 'https://huggingface.co/PaddlePaddle/PaddleOCR/resolve/main/ppocr_keys_v1.txt?download=true'
}

function Download-IfMissing($target, $url){
    if(Test-Path $target){ return }
    try{
        Write-Host "Downloading $([System.IO.Path]::GetFileName($target)) ..."
        Invoke-WebRequest -Uri $url -OutFile $target -UseBasicParsing
    } catch {
        Write-Warning "Primary download failed for $target. Trying mirror..."
        $name = [System.IO.Path]::GetFileName($target)
        if($hf.ContainsKey($name)){
            Invoke-WebRequest -Uri $hf[$name] -OutFile $target -UseBasicParsing
        } else { throw }
    }
}

# Download artifacts
foreach($f in $files){
    $out = Join-Parts @($modelsDir, $f.Name)
    if(!(Test-Path (Join-Parts @($modelsDir, 'ppocr_keys.txt'))) -and $f.Name -eq 'ppocr_keys_v1.txt'){
        Download-IfMissing -target $out -url $f.Url
    } elseif($f.Name -ne 'ppocr_keys_v1.txt'){
        Download-IfMissing -target $out -url $f.Url
    }
}

# Extract .tar files into folders
Get-ChildItem $modelsDir -Filter *.tar | ForEach-Object {
    $tarFile = $_.FullName
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($tarFile)
    $targetDir = Join-Parts @($modelsDir, $baseName)
    if(Test-Path $targetDir -and (Get-ChildItem $targetDir -Recurse | Measure-Object).Count -gt 0){
        Write-Host "Skip extract: $baseName already exists"
        return
    }
    Ensure-Dir $targetDir
    Write-Host "Extracting $baseName ..."
    try{
        tar -xf $tarFile -C $targetDir
    } catch {
        throw "Extraction failed for $tarFile. Ensure 'tar' is available (Windows 10+ has tar)."
    }
}

# Place keys file to canonical name if present
$keysV1 = Join-Parts @($modelsDir, 'ppocr_keys_v1.txt')
$keys = Join-Parts @($modelsDir, 'ppocr_keys.txt')
if((Test-Path $keysV1) -and !(Test-Path $keys)){
    Copy-Item $keysV1 $keys -Force
}

# Deploy to runtime inference directory
$deploySet = @('ch_PP-OCRv5_det_infer','ch_PP-OCRv5_rec_infer','ch_ppocr_mobile_v2.0_cls_infer')
foreach($name in $deploySet){
    $src = Join-Parts @($modelsDir, $name)
    if(Test-Path $src){
        $dst = Join-Parts @($inferenceDir, $name)
        if(Test-Path $dst){ Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue }
        Write-Host "Copying $name -> inference"
        Copy-Item $src $dst -Recurse -Force
    }
}
if(Test-Path $keys){ Copy-Item $keys (Join-Parts @($inferenceDir, 'ppocr_keys.txt')) -Force }

Write-Host "Done. If det/rec v5 exist, runtime now uses PP-OCRv5. Otherwise v3 remains as fallback."
