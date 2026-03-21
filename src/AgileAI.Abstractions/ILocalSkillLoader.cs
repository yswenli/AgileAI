namespace AgileAI.Abstractions;

public interface ILocalSkillLoader
{
    Task<IReadOnlyList<SkillManifest>> LoadFromDirectoryAsync(
        string rootDirectory,
        CancellationToken cancellationToken = default);
}
