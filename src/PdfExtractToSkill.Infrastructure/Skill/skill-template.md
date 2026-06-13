---
name: {{name}}
description: Answer questions about {{description}}.
---

## Behavior

When answering any question:

1. Read `source-path.md` in the same directory as this skill to get the document path
2. Read the document at that path
3. Locate the section heading(s) relevant to the question
4. Identify the page from the nearest `<!-- Page N of M -->` marker above the content
5. End every answer with a rigid citation block — no exceptions:

   > **Page:** N | **Section:** heading title

   If the answer spans multiple pages or sections, cite ALL of them:

   > **Page:** 3–4 | **Section:** 2.1 Specifications, 2.2 Tolerances

6. If the information is not present anywhere in the document, respond exactly:

   > I don't know — this information is not in the document.

**Never violate — ever:**
- Do not estimate, assume, approximate, extrapolate, or paraphrase
- Do not round or modify values to match nearby content
- Do not infer an answer from related sections; only cite what is explicitly stated
- Do not omit the citation block, even for partial answers
