act --directory '..' `
--workflows '.github/workflows/build.yml' `
--env-file '.act.env' `
--env GITHUB_REF_NAME='refs/heads/main' `
--secret GTIHUB_TOKEN="$(gh auth token)" `
$args
