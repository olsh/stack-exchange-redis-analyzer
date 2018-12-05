#tool nuget:?package=Codecov
#addin nuget:?package=Cake.Codecov

#tool nuget:?package=MSBuild.SonarQube.Runner.Tool
#addin nuget:?package=Cake.Sonar

var target = Argument("target", "Default");

var buildConfiguration = "Release";

var projectName = "StackExchange.Redis.Analyzer";
var testProjectName = "StackExchange.Redis.Analyzer.Test";

var solutionFile = string.Format("./src/{0}.sln", projectName);
var projectFolder = string.Format("./src/{0}/{0}/", projectName);
var vsixProjectFolder = string.Format("./src/{0}/StackExchange.Redis.Analyzer.Vsix/", projectName);
var testProjectFolder = string.Format("./src/{0}/{1}/", projectName, testProjectName);
var testProjectFile = string.Format("{0}{1}.csproj", testProjectFolder, testProjectName);

var nugetPackageFile = string.Format("{0}bin/{1}/{2}.nuget", projectFolder, buildConfiguration, projectName);
var vsixFile = string.Format("{0}bin/{1}/{2}.vsix", vsixProjectFolder, buildConfiguration, projectName);

var projectFile = string.Format("{0}{1}.csproj", projectFolder, projectName);
var extensionsVersion = XmlPeek(projectFile, "Project/PropertyGroup/Version/text()");

var nugetPackage = string.Format("{0}/bin/{1}/{2}.{3}.nupkg", projectFolder, buildConfiguration, projectName, extensionsVersion);

Task("UpdateBuildVersion")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
    var buildNumber = BuildSystem.AppVeyor.Environment.Build.Number;

    BuildSystem.AppVeyor.UpdateBuildVersion(string.Format("{0}.{1}", extensionsVersion, buildNumber));
});

Task("Build")
  .Does(() =>
{
    var settings = new MSBuildSettings
    {
        Configuration = buildConfiguration,
        MSBuildPlatform = MSBuildPlatform.x86,
        Restore = true
    };

    MSBuild(solutionFile, settings);
});

Task("Test")
  .IsDependentOn("Build")
  .Does(() =>
{
     var settings = new DotNetCoreTestSettings
     {
         Configuration = buildConfiguration
     };

     DotNetCoreTest(testProjectFile, settings);
});

Task("CodeCoverage")
  .IsDependentOn("Build")
  .Does(() =>
{
    var settings = new DotNetCoreTestSettings
    {
        Configuration = buildConfiguration,
        ArgumentCustomization = args => args
                                            .Append("/p:CollectCoverage=true")
                                            .Append("/p:CoverletOutputFormat=opencover")
    };

    DotNetCoreTest(testProjectFile, settings);

    Codecov(string.Format("{0}coverage.opencover.xml", testProjectFolder), EnvironmentVariable("codecov:token"));
});

Task("CreateArtifact")
  .IsDependentOn("Build")
  .WithCriteria(BuildSystem.AppVeyor.IsRunningOnAppVeyor)
  .Does(() =>
{
    BuildSystem.AppVeyor.UploadArtifact(nugetPackageFile);
    BuildSystem.AppVeyor.UploadArtifact(vsixFile);
});

Task("SonarBegin")
  .Does(() => {
     SonarBegin(new SonarBeginSettings {
        Url = "https://sonarcloud.io",
        Login = EnvironmentVariable("sonar:apikey"),
        Key = "stack-exchange-redis-analyzer",
        Name = "StackExchange.Redis.Analyzer",
        ArgumentCustomization = args => args
            .Append($"/o:olsh-github"),
        Version = "1.0.0.0"
     });
  });

Task("SonarEnd")
  .Does(() => {
     SonarEnd(new SonarEndSettings {
        Login = EnvironmentVariable("sonar:apikey")
     });
  });

Task("Sonar")
  .IsDependentOn("SonarBegin")
  .IsDependentOn("Build")
  .IsDependentOn("SonarEnd");

Task("Default")
    .IsDependentOn("Test");

Task("CI")
    .IsDependentOn("UpdateBuildVersion")
    .IsDependentOn("Sonar")
    .IsDependentOn("CodeCoverage")
    .IsDependentOn("CreateArtifact");

RunTarget(target);
