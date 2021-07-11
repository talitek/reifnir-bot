$apiKey = Read-Host "OrdbokApiKey";

$baseRequestUrl = "https://beta.ordbok.uib.no/api/dict";

$bob = [PSCustomObject]@{
    name = 'bob'
    fileSuffix = 'no_nb'
};

$nob = [PSCustomObject]@{
    name = 'nob'
    fileSuffix = 'no_nn'
};

$dictionaries = @($bob, $nob);

foreach ($dictionary in $dictionaries) {
    $dictName = $dictionary.name;
    $dictFileSuffix = $dictionary.fileSuffix;

    $requestUrl = "${baseRequestUrl}/${dictName}";
    $requestHeaders = @{'x-api-key' = $apiKey };

    $result = Invoke-WebRequest $requestUrl -Headers $requestHeaders | ConvertFrom-Json;
    
    $conceptMap = $result.concepts;
    
    $sb = [System.Text.StringBuilder]::new();
    
    [void]$sb.AppendLine("{");
    
    foreach ($property in $conceptMap.psobject.properties ) {
        $conceptName = $property.name;
        $conceptValue = $property.value.expansion;    
    
        [void]$sb.AppendLine("  ""${conceptName}"": ""${conceptValue}"",");
    }
    
    [void]$sb.AppendLine("}");
    
    $result = $sb.ToString();
    
    #Write-Output $result;
    $result | Out-File "OrdbokConcepts.${dictFileSuffix}.json";
}