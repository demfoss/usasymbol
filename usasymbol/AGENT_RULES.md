Agent rule for this workspace:

- Never use scripts without the user's explicit permission.
- If bulk changes are needed, use manual edits only unless the user clearly allows scripting.
- Do not run mass search/replace or script-driven edits across `Content/**`, SEO fields, headings, YAML, or Markdown files without explicit user permission for that exact operation.
- Treat all YAML and Markdown content files as UTF-8. Do not infer file corruption from how PowerShell displays Unicode.
- Do not use PowerShell `Get-Content` output as the source of truth for Unicode punctuation in content files. Verify suspicious characters with a UTF-8-safe read method before editing.
- After any encoding-related issue, do not expand the scope to "fix similar files" without the user's explicit approval. Limit work to the single requested file unless told otherwise.
- In content files, never use scripts to normalize punctuation, quotes, dashes, or SEO text at scale. Use targeted manual edits only.
