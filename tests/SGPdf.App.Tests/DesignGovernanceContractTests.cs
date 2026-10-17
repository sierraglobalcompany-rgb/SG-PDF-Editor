using Xunit;

namespace SGPdf.App.Tests;

public sealed class DesignGovernanceContractTests
{
    private const string DesignSequence =
        "INTENT → HIERARCHY → DISCLOSURE → CONSISTENCY → FEEDBACK → ACCESSIBILITY → VERIFY";

    [Fact]
    public void DesignLawAndProjectSkill_AreDurableProjectAuthorities()
    {
        var lawPath = FindRepoFile("docs", "design", "SGPDF_DESIGN_LAW.md");
        var skillPath = FindRepoFile(".agents", "skills", "sgpdf-design", "SKILL.md");

        var law = File.ReadAllText(lawPath);
        var skill = File.ReadAllText(skillPath);

        Assert.Contains(DesignSequence, law, StringComparison.Ordinal);
        Assert.Contains(DesignSequence, skill, StringComparison.Ordinal);
        Assert.Contains("Excel infinito", law, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("progressive disclosure", skill, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AgentAndReadmeAuthority_RequireTheSgPdfDesignGateForUiWork()
    {
        var agents = File.ReadAllText(FindRepoFile("AGENTS.md"));
        var readme = File.ReadAllText(FindRepoFile("README.md"));

        Assert.Contains(".agents/skills/sgpdf-design/SKILL.md", agents, StringComparison.Ordinal);
        Assert.Contains("Ley SG de Diseño", agents, StringComparison.Ordinal);
        Assert.Contains("Guardrail anti-parches", agents, StringComparison.Ordinal);
        Assert.Contains(".agents/skills/sgpdf-design/SKILL.md", readme, StringComparison.Ordinal);
        Assert.Contains("Ley SG de Diseño", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentAuthority_DoesNotRouteUiAgentsBackToTheStaleF12Snapshot()
    {
        var state = File.ReadAllText(FindRepoFile(".planning", "STATE.md"));
        var master = File.ReadAllText(FindRepoFile("docs", "MASTER_CONTEXT.md"));

        Assert.Contains("F13", state, StringComparison.Ordinal);
        Assert.Contains("Task9", state, StringComparison.Ordinal);
        Assert.Contains("DG-0", state, StringComparison.Ordinal);
        Assert.Contains("R1B", state, StringComparison.Ordinal);
        Assert.Contains("R2", state, StringComparison.Ordinal);
        Assert.Contains("feat/f13-task9-t9-1-document-shell-realignment-20261017", state, StringComparison.Ordinal);
        Assert.DoesNotContain("feat/f12-7-pdf-to-png-mvp-green3", state, StringComparison.Ordinal);

        Assert.Contains("AUTORIDAD VIGENTE — 2026-10-17", master, StringComparison.Ordinal);
        Assert.Contains("docs/design/SGPDF_DESIGN_LAW.md", master, StringComparison.Ordinal);
    }

    private static string FindRepoFile(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = segments.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Repository file not found: {Path.Combine(segments)}");
    }
}
