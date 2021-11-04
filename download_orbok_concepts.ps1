$baseRequestUrl = "https://ord.uib.no";

$bob = [PSCustomObject]@{
    name = 'bm'
    fileSuffix = 'no_nb'
};

$nob = [PSCustomObject]@{
    name = 'nn'
    fileSuffix = 'no_nn'
};

$dictionaries = @($bob, $nob);

$tempFile = "temp_file.txt";

foreach ($dictionary in $dictionaries) {
    $dictName = $dictionary.name;
    $dictFileSuffix = $dictionary.fileSuffix;

    $requestUrl = "${baseRequestUrl}/${dictName}/concepts.json";
    $requestHeaders = @{'Accept' = 'application/json; charset=utf-8'};

    $response = Invoke-WebRequest $requestUrl -Headers $requestHeaders -OutFile $tempFile;

    $responseJson = Get-Content $tempFile -Encoding UTF8 -Raw | ConvertFrom-Json

    Remove-Item $tempFile -Force
    
    $conceptMap = $responseJson.concepts;
    
    $sb = [System.Text.StringBuilder]::new();
    
    [void]$sb.AppendLine("{");
    
    foreach ($property in $conceptMap.psobject.properties ) {
        $conceptName = $property.name;
        $conceptValue = $property.value.expansion;    
    
        [void]$sb.AppendLine("  ""${conceptName}"": ""${conceptValue}"",");
    }
    
    [void]$sb.AppendLine("}");
    
    $conceptData = $sb.ToString();
    
    #Write-Output $result;
    $conceptData | Out-File "OrdbokConcepts_${dictFileSuffix}.json";
}