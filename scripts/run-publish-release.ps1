act --directory '..' `
--workflows '.github/workflows/publish-release.yml' `
--env-file '.act.env' `
--env GITHUB_REF_NAME='refs/heads/main' `
--secret-file '.secrets' `
--secret GITHUB_TOKEN="$(gh auth token)" `
$args
