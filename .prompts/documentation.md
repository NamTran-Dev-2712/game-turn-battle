# Prompt: Write / Update Documentation

```
Document <topic / change>.

RULES
- Respect SSOT: business truth stays in docs/mvp/, decisions in docs/adr/. Update the CANONICAL
  doc; make derived docs LINK it, never copy (index, don't repeat).
- Language: docs/ + folder READMEs are Vietnamese; the .claude/ AI execution layer is English.
- Markdown per docs/conventions/data-and-docs-conventions.md (relative links, README-per-folder).

SCOPE
- Which canonical doc is the home for this? <docs/...>
- Which derived docs must point to it? <root *.md, module README>

DONE
- Canonical doc updated, derived docs link it, no dead links.
- If this documents a change to code, it's part of the same change (.claude/workflows/documentation-sync.md).
```
