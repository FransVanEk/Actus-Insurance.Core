#!/usr/bin/env bash
set -euo pipefail

BRANCH="${1:-main}"
TMP_BRANCH="history-reset-$(date +%s)"

echo "Resetting git history on branch: $BRANCH"
echo "Temporary branch: $TMP_BRANCH"

# Make sure we're in a git repo
git rev-parse --is-inside-work-tree >/dev/null

# Switch to target branch
git checkout "$BRANCH"

# Create orphan branch with current files
git checkout --orphan "$TMP_BRANCH"

# Stage everything and create single root commit
git add -A
git commit -m "Initial commit"

# Delete old branch and rename new one
git branch -D "$BRANCH"
git branch -m "$BRANCH"

echo
echo "Local history reset complete."
echo "To overwrite remote history, run:"
echo "  git push -f origin $BRANCH"