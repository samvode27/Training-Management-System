# TMS API Versioning Policy

## Breaking Changes (Require New Version)
- Removing a field
- Renaming a field
- Changing a status code
- Tightening validation
- Changing default sort order

## Non-Breaking (Additive Changes)
- Adding a new optional field
- Adding a new endpoint
- Adding a new optional query parameter

## Sunset Window
- Minimum 6 months support for each version
- Deprecation headers sent from day one of new version

## Communication
- Deprecation/Sunset/Link headers in every response
- CHANGELOG entry
- Email to all API key holders
- Calendar invite for shutdown date