#!/bin/bash
PROPS_FILE="Directory.Build.props"
CURRENT_VERSION=$(LC_ALL=en_US.utf8 grep -oP '<Version>\K[^<]+' "$PROPS_FILE")
if [ -z "$CURRENT_VERSION" ]; then
  echo "Error: Could not find Version tag in $PROPS_FILE"
  exit 1
fi
echo "Current version: $CURRENT_VERSION"

if [[ "$CURRENT_VERSION" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)(-.+)?$ ]]; then
  MAJOR="${BASH_REMATCH[1]}"
  MINOR="${BASH_REMATCH[2]}"
  PATCH="${BASH_REMATCH[3]}"
  SUFFIX="${BASH_REMATCH[4]}"
else
  echo "Error: Version format not recognized"
  exit 1
fi

NEW_PATCH=$((PATCH + 1))
NEW_VERSION="$MAJOR.$MINOR.$NEW_PATCH$SUFFIX"

echo "New version: $NEW_VERSION"
sed -i "s/<Version>$CURRENT_VERSION<\/Version>/<Version>$NEW_VERSION<\/Version>/" "$PROPS_FILE"
echo "Version bumped from $CURRENT_VERSION to $NEW_VERSION"
