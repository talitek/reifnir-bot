act --directory '..' `
--workflows '.github/workflows/release-v2.yml' `
--input imageTag='release-manager' `
--env-file '.act.env' `
--secret-file '.secrets' `
--eventpath 'act-event.json' `
--env GITHUB_REF_NAME='refs/heads/release-manager' `
--secret GITHUB_TOKEN="$(gh auth token)" `
$args
