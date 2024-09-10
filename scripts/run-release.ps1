act --directory '..' `
--workflows '.github/workflows/release-v2.yml' `
--input imageTag='release-manager' --input createRelease=true `
--env-file '.act.env' `
--env GITHUB_REF_NAME='refs/heads/release-manager' `
--secret GITHUB_TOKEN="$(gh auth token)" `
$args
