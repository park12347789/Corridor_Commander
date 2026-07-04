namespace CorridorCommander
{
    public interface ISkillProvider
    {
        SkillDefinitionSO SkillDefinition { get; }
        bool IsReady { get; }
        float CooldownRemaining { get; }
        bool TryUseSkill(SkillUseContext context);
    }
}
