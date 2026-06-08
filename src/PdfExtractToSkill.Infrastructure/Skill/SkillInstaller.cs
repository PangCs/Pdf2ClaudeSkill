using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Skill;

public sealed class SkillInstaller : ISkillInstaller
{
    private readonly string _skillsRoot;

    public SkillInstaller()
        : this(DefaultSkillsRoot()) { }

    internal SkillInstaller(string skillsRoot)
    {
        _skillsRoot = skillsRoot;
    }

    public void Install(SkillDefinition definition)
    {
        var skillDir = SkillDir(definition.Name);
        Directory.CreateDirectory(skillDir);

        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            BuildSkillMd(definition));

        File.WriteAllText(
            ConfigPath(definition.Name),
            Path.GetDirectoryName(definition.OutputFilePath) ?? string.Empty);
    }

    public bool Exists(string skillName) =>
        File.Exists(Path.Combine(SkillDir(skillName), "SKILL.md"));

    private string SkillDir(string name) => Path.Combine(_skillsRoot, name);
    private string ConfigPath(string name) => Path.Combine(_skillsRoot, $"{name}-config");

    private static string DefaultSkillsRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "skills");

    private static string BuildSkillMd(SkillDefinition def) => $"""
        ---
        name: {def.Name}
        description: Answer questions about {def.Description} from the extracted document. Cite the section and page number for every answer. Say "I don't know" if the information is absent.
        ---

        # {def.Name}

        **Source document:** {def.SourceFileName}
        **Reference file:** `{def.OutputFilePath}`

        ## Lookup Workflow

        When answering a question:

        1. Read the reference file at `{def.OutputFilePath}`
        2. Find the section heading(s) relevant to the question
        3. Note the page from the nearest `<!-- Page N of M -->` marker above the content
        4. Return the exact answer from the document with the citation: **Section:** `<heading>` | **Page:** N
        5. If the information is not present anywhere in the document, respond exactly:
           > I don't know — this information is not in the document.

        **Strict rules — never violate:**
        - Do not estimate, assume, approximate, extrapolate, or paraphrase
        - Do not round or modify values to match nearby content
        - Do not infer an answer from related sections; only cite what is explicitly stated

        ## Reference

        | Source document | Skill name | Reference file |
        |-----------------|------------|----------------|
        | {def.SourceFileName} | {def.Name} | `{def.OutputFilePath}` |
        """;
}
