pipeline {
  agent { label "windows" }
  environment {
    GITHUB_TOKEN = credentials("github-publish-token")
  }
  stages {
    // Configure git
    stage("Configure git") {
      steps {
        bat "git config user.email burt-macklin@jenkins"
        bat 'git config user.name "Agent Burt Macklin"'
      }
    }
    // Determine build & publish flags for branch
    stage("Setup bleeding edge environment") {
      when { 
        anyOf {
          branch "main"
          tag "*-bleeding-edge-*"
        }
      }
      steps {
        script {
          env.ARTIFACT_CACHES = "bleeding-edge"
          env.AUTO_PUBLISH = "true"
          env.BUILD_CONFIG = "debug"
          env.IS_PRERELEASE = "true"
          env.JOB_CACHE = "bleeding-edge"
          env.TAG_PREFIX = "Unstable Release"
        }
      }
    }
    stage("Setup experimental environment") {
      when { branch "experimental" }
      steps {
        script {
          env.ARTIFACT_CACHES = "experimental"
          env.AUTO_PUBLISH = "false"
          env.BUILD_CONFIG = "debug"
          env.IS_PRERELEASE = "true"
          env.JOB_CACHE = "experimental"
          env.TAG_PREFIX = "Experimental Release"
        }
      }
    }
    stage("Setup pre-release environment") {
      when { branch "prerelease" }
      steps {
        script {
          env.ARTIFACT_CACHES = "bleeding-edge,prerelease"
          env.AUTO_PUBLISH = "true"
          env.BUILD_CONFIG = "release"
          env.IS_PRERELEASE = "true"
          env.JOB_CACHE = "prerelease"
          env.TAG_PREFIX = "Pre-Release"
        }
      }
    }
    stage("Setup release environment") {
      when { branch "release" }
      steps {
        script {
          env.ARTIFACT_CACHES = "bleeding-edge,prerelease,release"
          env.AUTO_PUBLISH = "true"
          env.BUILD_CONFIG = "release"
          env.IS_PRERELEASE = "false"
          env.JOB_CACHE = "release"
          env.TAG_PREFIX = "Stable Release"
        }
      }
    }
    // Determine the version number
    stage("Calculate semver") {
      steps {
        bat "gitversion /output buildserver"
        script {
          def props = readProperties file: "gitversion.properties"
          env.GITVERSION_SEMVER = props.GitVersion_SemVer
          env.PUBLISH_TAG = "v${props.GitVersion_SemVer}"
        }
      }
    }
    // Build
    stage("Build") {
      steps {
        powershell '''
          $DriveRoot = Split-Path -Path $env:WORKSPACE -Qualifier
          $ReleasePath = Join-Path -Path $DriveRoot -ChildPath "usi-releases"
          $JobReleasePath = Join-Path -Path $ReleasePath -ChildPath $env:JOB_CACHE
          $ReferencePath = Join-Path -Path $JobReleasePath -ChildPath "000_USITools"

          dotnet build --output FOR_RELEASE/GameData/UmbraSpaceIndustries/MKS --configuration $env:BUILD_CONFIG `
            --verbosity detailed /p:ReferencePath="$ReferencePath" ./Source/KolonyTools/KolonyTools.csproj
          dotnet build --output FOR_RELEASE/GameData/UmbraSpaceIndustries/WOLF --configuration $env:BUILD_CONFIG `
            --verbosity detailed /p:ReferencePath="$ReferencePath" ./Source/WOLF/WOLFUI/WOLFUI.csproj
          dotnet build --output FOR_RELEASE/GameData/UmbraSpaceIndustries/WOLF --configuration $env:BUILD_CONFIG `
            --verbosity detailed /p:ReferencePath="$ReferencePath" ./Source/WOLF/WOLF/WOLF.csproj
        '''
      }
    }
    // Update artifact cache
    stage("Cache artifacts") {
      steps {
        powershell '''
          try {
            Write-Output "Determining releases folder location..."
            $DriveRoot = Split-Path -Path $env:WORKSPACE -Qualifier
            $ReleaseFolder = "usi-releases"
            $ReleasePath = Join-Path -Path $DriveRoot -ChildPath $ReleaseFolder
            Write-Output "Releases folder location is: $ReleasePath"
            Write-Output "Determining artifact folder locations..."
            $CacheNames = $env:ARTIFACT_CACHES.Split(",")
            $CacheNames.ForEach({
              Write-Output "Working on cache for: $_..."
              $CachePath = Join-Path -Path $ReleasePath -ChildPath $_
              Write-Output "Branch folder location is: $CachePath"
              New-Item -Path $ReleasePath -Name $_ -ItemType Directory -Force
              Write-Output "Removing old artifacts..."
              $NewArtifacts = Get-ChildItem -Path ./FOR_RELEASE/GameData/UmbraSpaceIndustries -Attributes Directory -Name
              $NewArtifacts.ForEach({
                $ArtifactPath = Join-Path -Path (Join-Path -Path $CachePath -ChildPath "UmbraSpaceIndustries") -ChildPath $_
                if (Test-Path -Path $ArtifactPath) {
                  Remove-Item -Path $ArtifactPath -Recurse -Force
                }
              })
              Write-Output "Caching new artifacts..."
              Copy-Item -Path ./FOR_RELEASE/GameData/* -Destination $CachePath -Recurse
            })
          }
          catch {
            throw $_
          }
        '''
      }
    }
    // Packaging
    stage("Package artifacts") {
      steps {
        powershell '''
          Write-Output "Gathering artifacts to package..."
          if (Test-Path -Path "./artifacts") {
            Remove-Item -Path "./artifacts/*" -Recurse -Force
          }
          New-Item -Path . -Name "artifacts" -ItemType Directory -Force
          Copy-Item -Path ./FOR_RELEASE/* -Destination ./artifacts -Recurse
          Copy-Item -Path ./*.txt -Destination ./artifacts

          Write-Output "Pulling in cached dependencies..."
          $DriveRoot = Split-Path -Path $env:WORKSPACE -Qualifier
          $ReleasePath = Join-Path -Path $DriveRoot -ChildPath "usi-releases"
          $ModuleManagerPath = Join-Path -Path $ReleasePath -ChildPath "ModuleManager"
          $CachePath = Join-Path -Path $ReleasePath -ChildPath $env:JOB_CACHE
          $UsiCachePath = Join-Path -Path $CachePath -ChildPath "UmbraSpaceIndustries"
          Copy-Item -Path $ModuleManagerPath/* -Destination ./artifacts/GameData
          Copy-Item -Path (Join-Path -Path $CachePath -ChildPath "000_USITools") -Destination ./artifacts/GameData -Recurse
          Copy-Item -Path (Join-Path -Path $CachePath -ChildPath "CommunityCategoryKit") -Destination ./artifacts/GameData -Recurse
          Copy-Item -Path (Join-Path -Path $CachePath -ChildPath "CommunityResourcePack") -Destination ./artifacts/GameData -Recurse
          Copy-Item -Path (Join-Path -Path $CachePath -ChildPath "Firespitter") -Destination ./artifacts/GameData -Recurse
          Copy-Item -Path (Join-Path -Path $UsiCachePath -ChildPath "Akita") -Destination ./artifacts/GameData/UmbraSpaceIndustries -Recurse
          Copy-Item -Path (Join-Path -Path $UsiCachePath -ChildPath "FX") -Destination ./artifacts/GameData/UmbraSpaceIndustries -Recurse
          Copy-Item -Path (Join-Path -Path $UsiCachePath -ChildPath "Kontainers") -Destination ./artifacts/GameData/UmbraSpaceIndustries -Recurse
          Copy-Item -Path (Join-Path -Path $UsiCachePath -ChildPath "Konstruction") -Destination ./artifacts/GameData/UmbraSpaceIndustries -Recurse
          Copy-Item -Path (Join-Path -Path $UsiCachePath -ChildPath "ReactorPack") -Destination ./artifacts/GameData/UmbraSpaceIndustries -Recurse
          Copy-Item -Path (Join-Path -Path $UsiCachePath -ChildPath "USICore") -Destination ./artifacts/GameData/UmbraSpaceIndustries -Recurse
        '''
        script {
          env.ARCHIVE_FILENAME = "MKS_${env.GITVERSION_SEMVER}.zip"
          zip dir: "artifacts", zipFile: "${env.ARCHIVE_FILENAME}", archive: true
        }
      }
    }
    // Tag commit, if necessary
    stage("Tag commit") {
      steps {
        powershell '''
          Write-Output "Looking for tag $env:PUBLISH_TAG..."
          $tagFound = git tag -l "$env:PUBLISH_TAG"
          if ( $tagFound -ne $env:PUBLISH_TAG )
          {
            Write-Output "Tag not found. Creating tag..."
            git tag -a $env:PUBLISH_TAG -m "$env:TAG_PREFIX $env:GITVERSION_SEMVER"
            Write-Output "Pushing tag to GitHub..."
            git push --tags
          }
        '''
      }
    }
    // Push artifacts to GitHub
    stage("Push release artifacts to GitHub") {
      when {
        environment name: "AUTO_PUBLISH", value: "true"
      }
      steps {
        powershell '''
          echo "Creating release on GitHub..."
          $RepoUrl = [uri]$env:GIT_URL
          $RepoPath = $RepoUrl.LocalPath.Replace(".git", "")
          $Url = "https://api.github.com/repos$RepoPath/releases"
          $Headers = @{
            "Accept" = "application/vnd.github.v3+json"
            "Authorization" = "token $env:GITHUB_TOKEN"
          }
          $Body = @{
            tag_name = "$env:PUBLISH_TAG"
            name = "$env:TAG_PREFIX $env:GITVERSION_SEMVER"
            prerelease = ($env:IS_PRERELEASE -eq "true")
          }
          $Json = ConvertTo-Json $Body
          $Response = Invoke-WebRequest -Method Post -Uri $Url -Headers $Headers `
              -ContentType "application/json" -Body $Json -UseBasicParsing
          if ( $Response.StatusCode -ge 300 ) {
            Write-Output "Could not create GitHub Release"
            Write-Output "Status Code: $Response.StatusCode"
            Write-Output $Response.Content
            throw $Response.StatusCode
          } else {
            Write-Output "GitHub API replied with status code: $($Response.StatusCode)"
            Write-Output ($Response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 100)
          }

          echo "Uploading artifacts to GitHub..."
          $ReleaseMetadata = ConvertFrom-Json $Response.Content
          $UploadUrl = $ReleaseMetadata | Select -ExpandProperty "upload_url"
          $UploadUrl = $UploadUrl.Replace("{?name,label}", "?name=$env:ARCHIVE_FILENAME")
          $Response = Invoke-WebRequest -Method Post -Uri $UploadUrl -Headers $Headers `
              -ContentType "application/zip" -InFile $env:ARCHIVE_FILENAME -UseBasicParsing
          if ( $Response.StatusCode -ge 300 ) {
            Write-Output "Could not upload artifacts to GitHub"
            Write-Output "Status Code: $Response.StatusCode"
            Write-Output $Response.Content
            throw $Response.StatusCode
          } else {
            Write-Output "GitHub API replied with status code: $($Response.StatusCode)"
            Write-Output ($Response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 100)
          }
        '''
      }
    }
  }
}
