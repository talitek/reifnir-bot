act --directory '..' `
--workflows '.github/workflows/release-v2.yml' `
--input imageTag='latest' `
--env-file '.act.env' `
--eventpath 'act-event.json' `
--env GITHUB_REF_NAME='refs/heads/main' `
--secret-file '.secrets' `
--secret GITHUB_TOKEN="$(gh auth token)" `
$args
