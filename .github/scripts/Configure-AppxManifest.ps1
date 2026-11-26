# Copyright (c) KyraFiles contributors
# SPDX-License-Identifier: MPL-2.0

param(
    [string]$Branch = "", # This has to correspond with one of the AppEnvironment enum values
    [string]$PackageManifestPath = "",
    [string]$Publisher = "",
    [string]$WorkingDir = "",
    [string]$SecretBingMapsKey = "",
    [string]$SecretSentry = "",
    [string]$SecretGitHubOAuthClientId = ""
)

# Load Package.appxmanifest
[xml]$xmlDoc = Get-Content $PackageManifestPath

# Add namespaces
$nsmgr = New-Object System.Xml.XmlNamespaceManager($xmlDoc.NameTable)
$nsmgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
$nsmgr.AddNamespace("rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities")
$nsmgr.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10")
$nsmgr.AddNamespace("uap5", "http://schemas.microsoft.com/appx/manifest/uap/windows10/5")
$ap = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Applications/pkg:Application/pkg:Extensions/uap:Extension[@Category='windows.protocol']/uap:Protocol", $nsmgr)
$aea = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Applications/pkg:Application/pkg:Extensions/uap5:Extension[@Category='windows.appExecutionAlias']/uap5:AppExecutionAlias", $nsmgr)
$ea = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Applications/pkg:Application/pkg:Extensions/uap5:Extension[@Category='windows.appExecutionAlias']/uap5:AppExecutionAlias/uap5:ExecutionAlias", $nsmgr)

# Update the publisher
$xmlDoc.Package.Identity.Publisher = $Publisher

if ($Branch -eq "SideloadPreview")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="KyraFilesPreview"
    $xmlDoc.Package.Properties.DisplayName="KyraFiles - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="KyraFiles - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="KyraFiles - Preview"

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "kyrafiles-preview");
    $ea.SetAttribute("Alias", "kyrafiles-preview.exe");

    # Save modified Package.appxmanifest
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Preview" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -creplace '"kyrafiles-dev"', '"kyrafiles-preview"' -creplace '"kyrafiles-dev:', '"kyrafiles-preview:' -creplace '"kyrafiles"', '"kyrafiles-preview"' -creplace '"kyrafiles:', '"kyrafiles-preview:' }) | `
        Set-Content $_ -NoNewline `
    }

}
elseif ($Branch -eq "StorePreview")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="KyraFilesPreview"
    $xmlDoc.Package.Properties.DisplayName="KyraFiles - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="KyraFiles - Preview"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="KyraFiles - Preview"

    # Remove capability that is only used for the sideload package
    $nsmgr = New-Object System.Xml.XmlNamespaceManager($xmlDoc.NameTable)
    $nsmgr.AddNamespace("pkg", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
    $nsmgr.AddNamespace("rescap", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities")
    $pm = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Capabilities/rescap:Capability[@Name='packageManagement']", $nsmgr)
    $xmlDoc.Package.Capabilities.RemoveChild($pm)

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "kyrafiles-preview");
    $ea.SetAttribute("Alias", "kyrafiles-preview.exe");
    
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Preview" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -creplace '"kyrafiles-dev"', '"kyrafiles-preview"' -creplace '"kyrafiles-dev:', '"kyrafiles-preview:' -creplace '"kyrafiles"', '"kyrafiles-preview"' -creplace '"kyrafiles:', '"kyrafiles-preview:' }) | `
        Set-Content $_ -NoNewline `
    }

}
elseif ($Branch -eq "SideloadStable")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="KyraFiles"
    $xmlDoc.Package.Properties.DisplayName="KyraFiles"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="KyraFiles"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="KyraFiles"

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "kyrafiles-stable");
    $ea.SetAttribute("Alias", "kyrafiles-stable.exe");
    
    # Save modified Package.appxmanifest
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -creplace '"kyrafiles-dev"', '"kyrafiles-stable"' -creplace '"kyrafiles-dev:', '"kyrafiles-stable:' -creplace '"kyrafiles"', '"kyrafiles-stable"' -creplace '"kyrafiles:', '"kyrafiles-stable:' }) | `
        Set-Content $_ -NoNewline `
    }

}
elseif ($Branch -eq "StoreStable")
{
    # Set identities
    $xmlDoc.Package.Identity.Name="KyraFiles"
    $xmlDoc.Package.Properties.DisplayName="KyraFiles"
    $xmlDoc.Package.Applications.Application.VisualElements.DisplayName="KyraFiles"
    $xmlDoc.Package.Applications.Application.VisualElements.DefaultTile.ShortName="KyraFiles"

    # Remove capability that is only used for the sideload package
    $pm = $xmlDoc.SelectSingleNode("/pkg:Package/pkg:Capabilities/rescap:Capability[@Name='packageManagement']", $nsmgr)
    $xmlDoc.Package.Capabilities.RemoveChild($pm)

    # Update app protocol and execution alias
    $ap.SetAttribute("Name", "kyrafiles-stable");
    $ea.SetAttribute("Alias", "kyrafiles-stable.exe");

    # Save modified Package.appxmanifest
    $xmlDoc.Save($PackageManifestPath)

    Get-ChildItem $WorkingDir -Include *.csproj, *.appxmanifest, *.xaml -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "Assets\\AppTiles\\Dev", "Assets\AppTiles\Release" }) | `
        Set-Content $_ -NoNewline `
    }

    Get-ChildItem $WorkingDir -Include *.cs, *.cpp -recurse | ForEach-Object -Process `
    { `
        (Get-Content $_ -Raw | ForEach-Object -Process { $_ -creplace '"kyrafiles-dev"', '"kyrafiles-stable"' -creplace '"kyrafiles-dev:', '"kyrafiles-stable:' -creplace '"kyrafiles"', '"kyrafiles-stable"' -creplace '"kyrafiles:', '"kyrafiles-stable:' }) | `
        Set-Content $_ -NoNewline `
    }

}

# Remove unused tile assets so they don't end up in the package
$keepTiles = if ($Branch -match 'Preview') { 'Preview' } elseif ($Branch -match 'Stable') { 'Release' } else { $null }
foreach ($folder in @('Dev', 'Preview', 'Release')) {
    if ($folder -eq $keepTiles) { continue }
    $tilePath = Join-Path $WorkingDir "src\KyraFiles.App\Assets\AppTiles\$folder"
    if (Test-Path $tilePath) { Remove-Item $tilePath -Recurse -Force }
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "cd_app_env_placeholder", $Branch }) | `
    Set-Content $_ -NoNewline `
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "bingmapskey.secret", "$SecretBingMapsKey" }) | `
    Set-Content $_ -NoNewline `
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "sentry.secret", "$SecretSentry" }) | `
    Set-Content $_ -NoNewline `
}

Get-ChildItem $WorkingDir -Include *.cs -recurse | ForEach-Object -Process `
{ `
    (Get-Content $_ -Raw | ForEach-Object -Process { $_ -replace "githubclientid.secret", "$SecretGitHubOAuthClientId" }) | `
    Set-Content $_ -NoNewline `
}
