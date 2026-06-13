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
            Path.Combine(skillDir, "source-path.md"),
            definition.OutputFilePath);
    }

    public bool Exists(string skillName)
    {
        var dir = SkillDir(skillName);
        return File.Exists(Path.Combine(dir, "SKILL.md")) &&
               File.Exists(Path.Combine(dir, "source-path.md"));
    }

    private string SkillDir(string name) => Path.Combine(_skillsRoot, name);

    private static string DefaultSkillsRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "skills");

    private static string BuildSkillMd(SkillDefinition def) => $"""
        ---
        name: {def.Name}
        description: Answer questions about {def.Description}.
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
        """;
}
