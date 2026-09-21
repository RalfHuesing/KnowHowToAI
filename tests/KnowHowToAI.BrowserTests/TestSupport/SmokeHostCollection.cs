namespace KnowHowToAI.BrowserTests.TestSupport;

[CollectionDefinition("Smoke-Host")]
public sealed class SmokeHostCollection : ICollectionFixture<SmokeHostFixture>;

[CollectionDefinition("Visual-Shell-Host")]
public sealed class VisualShellHostCollection : ICollectionFixture<VisualShellHostFixture>;

[CollectionDefinition("RootNode-Host")]
public sealed class RootNodeHostCollection : ICollectionFixture<RootNodeHostFixture>;
