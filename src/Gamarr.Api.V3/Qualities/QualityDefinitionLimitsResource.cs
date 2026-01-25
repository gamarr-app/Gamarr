namespace Gamarr.Api.V3.Qualities;

// SA1313 requires camelCase parameters, but record primary constructor parameters must be PascalCase to generate public properties
#pragma warning disable SA1313
public record QualityDefinitionLimitsResource(int Min, int Max);
#pragma warning restore SA1313
