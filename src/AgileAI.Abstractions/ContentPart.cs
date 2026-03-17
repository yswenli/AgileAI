namespace AgileAI.Abstractions;

public abstract record ContentPart;

public record TextPart(string Text) : ContentPart;

public record ImageUrlPart(string Url) : ContentPart;

public record BinaryPart(byte[] Data, string MediaType) : ContentPart;
